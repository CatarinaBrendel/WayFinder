using WayFinder.DevTools.Cli.Interactive;
using WayFinder.DevTools.Application.Conversations;

namespace WayFinder.DevTools.Tests.Cli;

public sealed class InteractiveShellTests
{
    [Fact]
    public async Task RunAsync_ExitCommand_ReturnsSuccess()
    {
        using var input = new StringReader("/exit");
        using var output = new StringWriter();

        var shell = new InteractiveShell(input, output);

        var exitCode = await shell.RunAsync();

        Assert.Equal(0, exitCode);

        Assert.Contains(
            "Session ended.",
            output.ToString()
        );
    }

    [Fact]
    public async Task RunAsync_HelpCommand_DisplaysHelp()
    {
        using var input = new StringReader(
            "/help\n/exit"
        );

        using var output = new StringWriter();

        var shell = new InteractiveShell(
            input,
            output
        );

        await shell.RunAsync();

        Assert.Contains(
            "Available commands",
            output.ToString()
        );
    }

    [Fact]
    public async Task RunAsync_UnknownInput_ReportsMissingProvider()
    {
        using var input = new StringReader(
            "Hello WayFinder\n/exit"
        );

        using var output = new StringWriter();

        var shell = new InteractiveShell(
            input,
            output
        );

        await shell.RunAsync();

        Assert.Contains(
            "No AI provider configured.",
            output.ToString()
        );
    }

    [Fact]
    public async Task RunAsync_EndOfInput_ExitsSuccessfully()
    {
        using var input = new StringReader("");
        using var output = new StringWriter();

        var shell = new InteractiveShell(
            input,
            output
        );

        var exitCode = await shell.RunAsync();

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsync_StatusCommand_DisplaysSessionInformation()
    {
        using var input = new StringReader(
            "/status\n/exit"
        );

        using var output = new StringWriter();

        var shell = new InteractiveShell(
            input,
            output
        );

        await shell.RunAsync();

        Assert.Contains(
            "Session status",
            output.ToString()
        );

        Assert.Contains(
            "Not configured",
            output.ToString()
        );
    }

    [Fact]
    public async Task RunAsync_ClearCommand_DoesNotFail()
    {
        using var input = new StringReader(
            "/clear\n/exit"
        );

        using var output = new StringWriter();

        var shell = new InteractiveShell(
            input,
            output
        );

        var exitCode = await shell.RunAsync();

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsync_WithSession_ProcessesConversation()
    {
        using var input = new StringReader(
            "Hello\nHow are you?\n/exit\n"
        );

        using var output = new StringWriter();

        var provider = new FakeAiProvider();
        var session = new ConversationSession(provider);

        var shell = new InteractiveShell(
            input,
            output,
            session
        );

        var exitCode = await shell.RunAsync();

        Assert.Equal(0, exitCode);

        Assert.Contains("Response 1", output.ToString());
        Assert.Contains("Response 2", output.ToString());
        Assert.Contains("Copilot", output.ToString());

        Assert.Equal(4, session.Messages.Count);
    }

    [Fact]
    public async Task RunAsync_RecognizesCommandWithArguments()
    {
        using var input = new StringReader(
            "/help extra\n/exit\n"
        );

        using var output = new StringWriter();

        var shell = new InteractiveShell(
            input,
            output
        );

        var result = await shell.RunAsync();

        Assert.Equal(0, result);

        Assert.Contains(
            "/help",
            output.ToString()
        );

        Assert.DoesNotContain(
            "Unknown command",
            output.ToString()
        );
    }

    private sealed class FakeAiProvider : IAiProvider
    {
        private int _requestCount;

        public string Id => "fake";

        public Task<ConversationResponse> CompleteAsync(
            ConversationRequest request,
            CancellationToken cancellationToken = default)
        {
            _requestCount++;

            return Task.FromResult(
                new ConversationResponse(
                    $"Response {_requestCount}"
                )
            );
        }
    }
}
