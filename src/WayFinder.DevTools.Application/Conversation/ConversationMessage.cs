namespace WayFinder.DevTools.Application.Conversations;

public sealed record ConversationMessage(
    ConversationRole Role,
    string Content);
