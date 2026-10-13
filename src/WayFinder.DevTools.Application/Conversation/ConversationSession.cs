namespace WayFinder.DevTools.Application.Conversations;

public sealed class ConversationSession
{
    private readonly IAiProvider _provider;
    private readonly List<ConversationMessage> _messages = [];

    public string ProviderId => _provider.Id;

    public IReadOnlyList<ConversationMessage> Messages => _messages.AsReadOnly();

    public ConversationSession(IAiProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public Task<ConversationResponse> SendAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        return SendCoreAsync(message, null, cancellationToken);
    }

    public Task<ConversationResponse> SendStreamingAsync(
        string message,
        Action<ConversationStreamEvent> onEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onEvent);

        return SendCoreAsync(message, onEvent, cancellationToken);
    }

    private async Task<ConversationResponse> SendCoreAsync(
        string message,
        Action<ConversationStreamEvent>? onEvent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(message));
        }

        var userMessage = new ConversationMessage(
            ConversationRole.User,
            message);

        var messages = new List<ConversationMessage>(_messages)
        {
            userMessage
        };

        var request = new ConversationRequest(messages);

        ConversationResponse response;

        if (onEvent is not null &&
            _provider is IStreamingAiProvider streamingProvider)
        {
            response = await streamingProvider.CompleteStreamingAsync(
                request,
                onEvent,
                cancellationToken);
        }
        else
        {
            response = await _provider.CompleteAsync(
                request,
                cancellationToken);
        }

        // Only commit messages after successful completion.
        _messages.Add(userMessage);

        _messages.Add(new ConversationMessage(
            ConversationRole.Assistant,
            response.Content));

        return response;
    }
}
