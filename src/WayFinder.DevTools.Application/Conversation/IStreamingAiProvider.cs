namespace WayFinder.DevTools.Application.Conversations;

public interface IStreamingAiProvider : IAiProvider
{
    Task<ConversationResponse> CompleteStreamingAsync(
        ConversationRequest request,
        Action<ConversationStreamEvent> onEvent,
        CancellationToken cancellationToken = default);
}
