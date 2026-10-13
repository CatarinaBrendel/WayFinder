namespace WayFinder.DevTools.Application.Conversations;

public enum ConversationStreamEventType
{
    Thought,
    Answer,
    ToolStarted,
    ToolUpdated
}

public sealed record ConversationStreamEvent(
    ConversationStreamEventType Type,
    string Content,
    ConversationToolEvent? Tool = null
);
