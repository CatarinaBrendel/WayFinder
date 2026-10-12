using WayFinder.DevTools.Application.Conversations;

namespace WayFinder.DevTools.Application.Tests.Conversations;

public sealed class ConversationContractsTests
{
    [Fact]
    public void ConversationMessage_PreservesRoleAndContent()
    {
        var message = new ConversationMessage(
            ConversationRole.User,
            "Hello WayFinder");

        Assert.Equal(ConversationRole.User, message.Role);
        Assert.Equal("Hello WayFinder", message.Content);
    }

    [Fact]
    public void ConversationRequest_PreservesMessageHistory()
    {
        var messages = new[]
        {
            new ConversationMessage(
                ConversationRole.User,
                "Hello"),

            new ConversationMessage(
                ConversationRole.Assistant,
                "Hello!"),

            new ConversationMessage(
                ConversationRole.User,
                "What did I just say?")
        };

        var request = new ConversationRequest(messages);

        Assert.Equal(3, request.Messages.Count);
        Assert.Equal("Hello", request.Messages[0].Content);
        Assert.Equal(
            ConversationRole.User,
            request.Messages[2].Role);
    }

    [Fact]
    public void ConversationResponse_PreservesContentAndUsage()
    {
        var response = new ConversationResponse(
            "Hello!",
            InputTokens: 12,
            OutputTokens: 4);

        Assert.Equal("Hello!", response.Content);
        Assert.Equal(12, response.InputTokens);
        Assert.Equal(4, response.OutputTokens);
    }
}
