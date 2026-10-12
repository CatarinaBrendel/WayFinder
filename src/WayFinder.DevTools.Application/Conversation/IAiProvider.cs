namespace WayFinder.DevTools.Application.Conversations;

public interface IAiProvider
{
    string Id { get; }

    Task<ConversationResponse> CompleteAsync(
        ConversationRequest request,
        CancellationToken cancellationToken = default);
}
