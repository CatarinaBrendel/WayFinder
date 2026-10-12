namespace WayFinder.DevTools.Application.Conversations;

public sealed class ConversationSession
{
    private readonly IAiProvider _provider;

    private readonly List<ConversationMessage> _messages = [];

    public string ProviderId => _provider.Id;

    public ConversationSession(IAiProvider provider)
    {
        _provider = provider
            ?? throw new ArgumentNullException(nameof(provider));
    }

    public IReadOnlyList<ConversationMessage> Messages =>
        _messages.AsReadOnly();

    public async Task<ConversationResponse> SendAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Message content cannot be empty.",
                nameof(content));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var userMessage = new ConversationMessage(
            ConversationRole.User,
            content);

        var requestMessages = new List<ConversationMessage>(
            _messages.Count + 1);

        requestMessages.AddRange(_messages);
        requestMessages.Add(userMessage);

        var request = new ConversationRequest(
            requestMessages.AsReadOnly());

        var response = await _provider.CompleteAsync(
            request,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        _messages.Add(userMessage);

        _messages.Add(new ConversationMessage(
            ConversationRole.Assistant,
            response.Content));

        return response;
    }
}
