using WayFinder.DevTools.Cli.Interactive;

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
}
