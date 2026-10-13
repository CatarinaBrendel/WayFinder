using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using WayFinder.DevTools.Application.Conversations;

namespace WayFinder.DevTools.Infrastructure.Conversations.Acp;

public sealed class CopilotAcpProvider : IStreamingAiProvider, IAsyncDisposable
{
    private readonly AcpClient _client;
    private readonly AcpSession _session;
    private readonly SemaphoreSlim _promptLock = new(1, 1);

    private bool _initialized;
    private bool _disposed;
    private bool _sessionInvalidated;

    public string Id => "copilot";

    public CopilotAcpProvider()
    {
        _client = new AcpClient();
        _session = new AcpSession(_client);
    }

    public async Task<ConversationResponse> CompleteAsync(
        ConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CompleteStreamingAsync(
            request,
            _ => { },
            cancellationToken);
    }

    public async Task<ConversationResponse> CompleteStreamingAsync(
        ConversationRequest request,
        Action<ConversationStreamEvent> onEvent,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(onEvent);

        await _promptLock.WaitAsync(cancellationToken);

        try
        {
            if (_sessionInvalidated)
            {
                throw new InvalidOperationException(
                    "The ACP session was invalidated by a previous cancellation. " +
                    "Create a new CopilotAcpProvider to continue.");
            }

            if (!_initialized)
            {
                await _session.InitializeAsync(cancellationToken);
                _initialized = true;
            }

            var prompt = request.Messages
                .LastOrDefault(message =>
                    message.Role == ConversationRole.User);

            if (prompt is null)
            {
                throw new ArgumentException(
                    "Conversation request contains no user message.",
                    nameof(request));
            }

            var answer = new StringBuilder();

            var events =
                Channel.CreateUnbounded<ConversationStreamEvent>(
                    new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = false
                    });

            void HandleNotification(JsonElement message)
            {
                if (!message.TryGetProperty("method", out var method) ||
                    method.GetString() != "session/update")
                {
                    return;
                }

                if (!message.TryGetProperty("params", out var parameters) ||
                    !parameters.TryGetProperty("sessionId", out var sessionId) ||
                    sessionId.GetString() != _session.SessionId ||
                    !parameters.TryGetProperty("update", out var update) ||
                    !update.TryGetProperty("sessionUpdate", out var updateType))
                {
                    return;
                }

                switch (updateType.GetString())
                {
                    case "agent_thought_chunk":
                        EnqueueText(
                            ConversationStreamEventType.Thought,
                            update);
                        break;

                    case "agent_message_chunk":
                        {
                            if (!update.TryGetProperty("content", out var content) ||
                                !content.TryGetProperty("text", out var text))
                            {
                                break;
                            }

                            var value = text.GetString();

                            // Copilot emits this diagnostic as a separate ACP chunk.
                            if (value is null ||
                                value.StartsWith(
                                    "Info: Disabled tools:",
                                    StringComparison.Ordinal))
                            {
                                break;
                            }

                            EnqueueText(
                                ConversationStreamEventType.Answer,
                                update);

                            break;
                        }

                    case "tool_call":
                        EnqueueTool(
                            ConversationStreamEventType.ToolStarted,
                            update);
                        break;

                    case "tool_call_update":
                        EnqueueTool(
                            ConversationStreamEventType.ToolUpdated,
                            update);
                        break;
                }
            }

            void EnqueueText(
                ConversationStreamEventType type,
                JsonElement update)
            {
                if (!update.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("text", out var text))
                {
                    return;
                }

                var value = text.GetString();

                if (!string.IsNullOrEmpty(value))
                {
                    events.Writer.TryWrite(
                        new ConversationStreamEvent(type, value));
                }
            }

            void EnqueueTool(
                ConversationStreamEventType type,
                JsonElement update)
            {
                var id = GetString(update, "toolCallId");
                var name = GetString(update, "title");
                var status = GetString(update, "status");

                events.Writer.TryWrite(
                    new ConversationStreamEvent(
                        type,
                        string.Empty,
                        new ConversationToolEvent(
                            id ?? string.Empty,
                            name ?? string.Empty,
                            status ?? string.Empty
                        )
                    )
                );
            }

            _client.NotificationReceived += HandleNotification;

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var promptTask = _session.SendPromptAsync(
                    prompt.Content,
                    cancellationToken);

                var consumeTask = Task.Run(async () =>
                {
                    await foreach (var streamEvent in
                        events.Reader.ReadAllAsync(cancellationToken))
                    {
                        if (streamEvent.Type == ConversationStreamEventType.Answer)
                        {
                            answer.Append(streamEvent.Content);
                        }

                        onEvent(streamEvent);
                    }
                }, cancellationToken);

                JsonElement result;

                try
                {
                    result = await promptTask;
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    _sessionInvalidated = true;

                    try
                    {
                        using var cancelTimeout =
                            new CancellationTokenSource(TimeSpan.FromSeconds(5));

                        await _session.CancelAsync(cancelTimeout.Token);
                    }
                    catch (Exception)
                    {
                        // The session is already invalidated.
                        // Cancellation is best-effort.
                    }

                    throw;
                }
                finally
                {
                    events.Writer.TryComplete();
                }

                await consumeTask;

                stopwatch.Stop();

                int? inputTokens = null;
                int? outputTokens = null;
                int? cachedTokens = null;

                if (result.TryGetProperty("usage", out var usage))
                {
                    inputTokens = GetInt(usage, "inputTokens");
                    outputTokens = GetInt(usage, "outputTokens");
                    cachedTokens = GetInt(usage, "cachedReadTokens");
                }

                return new ConversationResponse(
                    Content: answer.ToString(),
                    InputTokens: inputTokens,
                    OutputTokens: outputTokens,
                    CachedInputTokens: cachedTokens,
                    Model: _session.CurrentModelId,
                    DurationMs: stopwatch.Elapsed.TotalMilliseconds);
            }
            finally
            {
                _client.NotificationReceived -= HandleNotification;
            }
        }
        finally
        {
            _promptLock.Release();
        }
    }

    private static int? GetInt(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) &&
               value.TryGetInt32(out var number)
            ? number
            : null;
    }

    private static string? GetString(
        JsonElement element,
        string property)
    {
        return element.TryGetProperty(property, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _client.DisposeAsync();

        _promptLock.Dispose();
    }
}
