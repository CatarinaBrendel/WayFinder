using System.Text.Json;
using WayFinder.DevTools.Application.Conversations;

namespace WayFinder.DevTools.Infrastructure.Conversations;

public static class CopilotUsageParser
{
    public static ConversationResponse Parse(
        string json,
        string content = "")
    {
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        var tokens = root.GetProperty("tokenDetails");

        var inputTokens =
            tokens.GetProperty("input").GetProperty("tokenCount").GetInt32();

        var cachedTokens =
            tokens.GetProperty("cache_read").GetProperty("tokenCount").GetInt32();

        var outputTokens =
            tokens.GetProperty("output").GetProperty("tokenCount").GetInt32();

        return new ConversationResponse(
            Content: content,
            InputTokens: inputTokens + cachedTokens,
            OutputTokens: outputTokens,
            CachedInputTokens: cachedTokens,
            Model: root.GetProperty("currentModel").GetString(),
            DurationMs: root.GetProperty("totalApiDurationMs").GetDouble(),
            PremiumRequestCost: root.GetProperty("totalPremiumRequestCost").GetDouble()
        );
    }
}
