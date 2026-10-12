namespace WayFinder.DevTools.Application.Conversations;

public sealed record ConversationRequest(
    IReadOnlyList<ConversationMessage> Messages);
