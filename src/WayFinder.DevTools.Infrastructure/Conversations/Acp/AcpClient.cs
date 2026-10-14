using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace WayFinder.DevTools.Infrastructure.Conversations.Acp;

public sealed class AcpClient : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>>
        _pendingRequests = new();

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private readonly CancellationTokenSource _shutdown = new();

    private readonly Process _process;

    private readonly Task _readTask;
    private readonly Task _errorTask;

    private int _nextRequestId;
    private bool _disposed;

    public event Action<JsonElement>? NotificationReceived;
    public Func<AcpPermissionRequest, Task<AcpPermissionDecision>>? PermissionRequested { get; set; }

    public AcpClient()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "copilot",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("--acp");
        startInfo.ArgumentList.Add("--no-auto-update");

        // Only expose approved WayFinder MCP tools to Copilot.
        // This list must be kept in sync with the MCP server's tool definitions.
        string[] allowedTools =
        [
            "wayfinder-context",
            "wayfinder-projects",
            "wayfinder-repo_read",
            "wayfinder-repo_search",
            "wayfinder-git_status",
            "wayfinder-git_log",
            "wayfinder-git_diff"
        ];

        startInfo.ArgumentList.Add("--available-tools");
        startInfo.ArgumentList.Add(string.Join(",", allowedTools));

        // Grant execution permission only to the project-discovery MCP tool.
        startInfo.ArgumentList.Add("--allow-tool=wayfinder(projects)");
        startInfo.ArgumentList.Add("--allow-tool=wayfinder(repo_read)");

        // Disable built-in GitHub MCP servers.
        startInfo.ArgumentList.Add("--disable-builtin-mcps");

        // Prevent remote control and session export.
        startInfo.ArgumentList.Add("--no-remote");
        startInfo.ArgumentList.Add("--no-remote-export");

        // Disable automatic access to the temporary directory.
        startInfo.ArgumentList.Add("--disallow-temp-dir");

        _process = new Process
        {
            StartInfo = startInfo
        };

        if (!_process.Start())
        {
            _process.Dispose();

            throw new InvalidOperationException(
                "Could not start Copilot ACP server.");
        }

        _readTask = ReadMessagesAsync(_shutdown.Token);
        _errorTask = ReadErrorsAsync(_shutdown.Token);
    }

    public async Task<JsonElement> SendRequestAsync(
        string method,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        cancellationToken.ThrowIfCancellationRequested();

        var requestId = Interlocked.Increment(
            ref _nextRequestId);

        var completion = new TaskCompletionSource<JsonElement>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pendingRequests.TryAdd(requestId, completion))
        {
            throw new InvalidOperationException(
                "Could not register ACP request.");
        }

        try
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = requestId,
                method,
                @params = parameters
            };

            await WriteMessageAsync(
                request,
                cancellationToken);

            return await completion.Task.WaitAsync(
                cancellationToken);
        }
        finally
        {
            _pendingRequests.TryRemove(
                requestId,
                out _);
        }
    }

    private async Task WriteMessageAsync(
        object message,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);

        await _writeLock.WaitAsync(cancellationToken);

        try
        {
            await _process.StandardInput.WriteLineAsync(
                json.AsMemory(),
                cancellationToken);

            await _process.StandardInput.FlushAsync(
                cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ReadMessagesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _process.StandardOutput
                    .ReadLineAsync(cancellationToken);

                if (line is null)
                    break;

                using var document = JsonDocument.Parse(line);

                var message = document.RootElement.Clone();

                if (message.TryGetProperty(
                        "method", out var method))
                {
                    if (message.TryGetProperty("id", out var incomingId))
                    {
                        if (method.GetString() == "session/request_permission")
                        {
                            // Do not block the ACP reader while waiting for user input.
                            _ = HandlePermissionRequestAsync(
                                message,
                                cancellationToken);
                        }
                        else
                        {
                            // Reject unsupported server-initiated requests.
                            await WriteMessageAsync(
                                new
                                {
                                    jsonrpc = "2.0",
                                    id = incomingId,
                                    error = new
                                    {
                                        code = -32601,
                                        message = "Method not supported by WayFinder."
                                    }
                                },
                                cancellationToken);
                        }
                    }
                    else
                    {
                        NotificationReceived?.Invoke(message);
                    }

                    continue;
                }

                if (message.TryGetProperty(
                        "id", out var id) &&
                    id.ValueKind == JsonValueKind.Number)
                {
                    var requestId = id.GetInt32();

                    if (_pendingRequests.TryRemove(
                            requestId,
                            out var completion))
                    {
                        if (message.TryGetProperty(
                                "error", out var error))
                        {
                            completion.TrySetException(
                                new InvalidOperationException(
                                    $"ACP error: {error}"));
                        }
                        else
                        {
                            completion.TrySetResult(message);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception exception)
        {
            FailPendingRequests(exception);
        }
        finally
        {
            FailPendingRequests(
                new IOException("Copilot ACP connection closed."));
        }
    }

    private async Task ReadErrorsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (await _process.StandardError
                       .ReadLineAsync(cancellationToken) is { })
            {
                // Drain stderr to prevent the Copilot process from blocking.
                // Diagnostic logging can be added later.
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (IOException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Stream closed during shutdown.
        }
    }

    private void FailPendingRequests(Exception exception)
    {
        foreach (var entry in _pendingRequests)
        {
            if (_pendingRequests.TryRemove(
                    entry.Key,
                    out var completion))
            {
                completion.TrySetException(exception);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _shutdown.Cancel();

        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
        }

        await _process.WaitForExitAsync(
            CancellationToken.None);

        try
        {
            await Task.WhenAll(_readTask, _errorTask);
        }
        finally
        {
            _process.Dispose();
            _writeLock.Dispose();
            _shutdown.Dispose();
        }
    }

    public async Task SendNotificationAsync(
        string method,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        await WriteMessageAsync(
            new
            {
                jsonrpc = "2.0",
                method,
                @params = parameters
            },
            cancellationToken);
    }

    private async Task HandlePermissionRequestAsync(
    JsonElement message,
    CancellationToken cancellationToken)
    {
        var requestId = message.GetProperty("id").Clone();

        try
        {
            var parameters = message.GetProperty("params");
            var toolCall = parameters.GetProperty("toolCall");

            var request = new AcpPermissionRequest(
                ToolCallId: toolCall.GetProperty("toolCallId").GetString()!,
                ToolName: toolCall.GetProperty("title").GetString()!,
                RawInput: toolCall.GetProperty("rawInput").Clone());

            // Default deny when no interactive handler exists.
            var decision = PermissionRequested is { } handler
                ? await handler(request).WaitAsync(cancellationToken)
                : AcpPermissionDecision.Deny;

            // Only return an option explicitly offered by Copilot.
            var requestedOption = decision == AcpPermissionDecision.AllowOnce
                ? "allow_once"
                : "reject_once";

            var options = parameters.GetProperty("options");

            var optionAvailable = options.EnumerateArray().Any(
                option =>
                    option.GetProperty("optionId").GetString() ==
                    requestedOption);

            if (!optionAvailable)
            {
                throw new InvalidOperationException(
                    $"ACP permission option '{requestedOption}' was not offered.");
            }

            await WriteMessageAsync(
                new
                {
                    jsonrpc = "2.0",
                    id = requestId,
                    result = new
                    {
                        outcome = new
                        {
                            outcome = "selected",
                            optionId = requestedOption
                        }
                    }
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // The connection is shutting down.
        }
        catch (Exception)
        {
            // Never approve a permission request after a handler failure.
            try
            {
                await WriteMessageAsync(
                    new
                    {
                        jsonrpc = "2.0",
                        id = requestId,
                        error = new
                        {
                            code = -32603,
                            message = "Permission request could not be processed."
                        }
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // The connection is shutting down.
            }
        }
    }
}
