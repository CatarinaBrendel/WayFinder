namespace WayFinder.DevTools.Application.Conversations;

public sealed record ConversationResponse(
    string Content,
    int? InputTokens = null,
    int? OutputTokens = null,
    int? CachedInputTokens = null,
    string? Model = null,
    double? DurationMs = null,
    double? PremiumRequestCost = null);
