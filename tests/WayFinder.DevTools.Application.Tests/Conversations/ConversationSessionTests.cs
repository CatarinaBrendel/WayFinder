using WayFinder.DevTools.Application.Conversations;

namespace WayFinder.DevTools.Application.Tests.Conversations;

public sealed class ConversationSessionTests
{
    [Fact]
    public async Task SendAsync_AddsUserMessageAndAssistantResponse()
    {
        var provider = new FakeAiProvider();
        var session = new ConversationSession(provider);

        var response = await session.SendAsync("Hello WayFinder");

        Assert.Equal("Response 1", response.Content);

        Assert.Collection(
            session.Messages,
            message =>
            {
                Assert.Equal(ConversationRole.User, message.Role);
                Assert.Equal("Hello WayFinder", message.Content);
            },
            message =>
            {
                Assert.Equal(ConversationRole.Assistant, message.Role);
                Assert.Equal("Response 1", message.Content);
            });
    }

    [Fact]
    public async Task SendAsync_IncludesPreviousMessagesInNextRequest()
    {
        var provider = new FakeAiProvider();
        var session = new ConversationSession(provider);

        await session.SendAsync("First question");
        await session.SendAsync("Second question");

        Assert.Equal(2, provider.Requests.Count);

        Assert.Collection(
            provider.Requests[1].Messages,
            message => Assert.Equal("First question", message.Content),
            message => Assert.Equal("Response 1", message.Content),
            message => Assert.Equal("Second question", message.Content));

        Assert.Equal(4, session.Messages.Count);
    }

    [Fact]
    public async Task SendAsync_WhenProviderFails_DoesNotModifyHistory()
    {
        var provider = new FakeAiProvider
        {
            ShouldFail = true
        };

        var session = new ConversationSession(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.SendAsync("Hello"));

        Assert.Empty(session.Messages);
    }

    [Fact]
    public async Task SendAsync_WhenCancelled_DoesNotModifyHistory()
    {
        var provider = new FakeAiProvider
        {
            ShouldCancel = true
        };

        var session = new ConversationSession(provider);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => session.SendAsync("Hello"));

        Assert.Empty(session.Messages);
    }

    [Fact]
    public async Task SendAsync_WhenMessageIsEmpty_ThrowsArgumentException()
    {
        var provider = new FakeAiProvider();
        var session = new ConversationSession(provider);

        await Assert.ThrowsAsync<ArgumentException>(
            () => session.SendAsync("   "));

        Assert.Empty(provider.Requests);
        Assert.Empty(session.Messages);
    }

    private sealed class FakeAiProvider : IAiProvider
    {
        public string Id => "fake";

        public List<ConversationRequest> Requests { get; } = [];

        public bool ShouldFail { get; init; }

        public bool ShouldCancel { get; init; }

        public Task<ConversationResponse> CompleteAsync(
            ConversationRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            if (ShouldCancel)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (ShouldFail)
            {
                throw new InvalidOperationException("Provider failed.");
            }

            return Task.FromResult(
                new ConversationResponse($"Response {Requests.Count}"));
        }
    }
}
