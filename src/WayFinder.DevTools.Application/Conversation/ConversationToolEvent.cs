namespace WayFinder.DevTools.Application.Conversations;

public sealed record ConversationToolEvent(
    string Id,
    string Name,
    string Status
);
