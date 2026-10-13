using System.Text.Json;

namespace WayFinder.DevTools.Infrastructure.Conversations.Acp;

public sealed class AcpSession
{
    private readonly AcpClient _client;

    public string? SessionId { get; private set; }

    public string? CurrentModelId { get; private set; }

    public bool IsInitialized => SessionId is not null;

    public AcpSession(AcpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException(
                "ACP session has already been initialized.");
        }

        var initializeResponse = await _client.SendRequestAsync(
            "initialize",
            new
            {
                protocolVersion = 1,
                clientCapabilities = new
                {
                    fs = new
                    {
                        readTextFile = false,
                        writeTextFile = false
                    },
                    terminal = false
                },
                clientInfo = new
                {
                    name = "wayfinder",
                    version = "0.1.0"
                }
            },
            cancellationToken);

        ValidateResponse(initializeResponse, "initialize");

        var sessionResponse = await _client.SendRequestAsync(
            "session/new",
            new
            {
                cwd = Directory.GetCurrentDirectory(),
                mcpServers = Array.Empty<object>()
            },
            cancellationToken);

        var result = GetResult(sessionResponse, "session/new");

        var sessionId = result.GetProperty("sessionId").GetString();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidOperationException(
                "Copilot returned an invalid ACP session ID.");
        }

        SessionId = sessionId;

        if (result.TryGetProperty("models", out var models) &&
            models.TryGetProperty("currentModelId", out var model))
        {
            CurrentModelId = model.GetString();
        }

        await SetModelAsync("gpt-5-mini", cancellationToken);
    }

    private static void ValidateResponse(
        JsonElement response,
        string method)
    {
        _ = GetResult(response, method);
    }

    private static JsonElement GetResult(
        JsonElement response,
        string method)
    {
        if (!response.TryGetProperty("result", out var result))
        {
            throw new InvalidOperationException(
                $"ACP '{method}' returned no result.");
        }

        return result;
    }

    public async Task SetModelAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            throw new InvalidOperationException(
                "ACP session has not been initialized.");
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException(
                "Model ID cannot be empty.",
                nameof(modelId));
        }

        var response = await _client.SendRequestAsync(
            "session/set_model",
            new
            {
                sessionId = SessionId,
                modelId
            },
            cancellationToken);

        ValidateResponse(response, "session/set_model");

        CurrentModelId = modelId;
    }

    public async Task<JsonElement> SendPromptAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                "ACP session has not been initialized.");
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException(
                "Prompt cannot be empty.",
                nameof(prompt));
        }

        var response = await _client.SendRequestAsync(
            "session/prompt",
            new
            {
                sessionId = SessionId,
                prompt = new[]
                {
                new
                {
                    type = "text",
                    text = prompt
                }
                }
            },
            cancellationToken);

        return GetResult(response, "session/prompt").Clone();
    }

    public async Task CancelAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsInitialized)
        {
            return;
        }

        await _client.SendNotificationAsync(
            "session/cancel",
            new
            {
                sessionId = SessionId
            },
            cancellationToken);
    }
}
