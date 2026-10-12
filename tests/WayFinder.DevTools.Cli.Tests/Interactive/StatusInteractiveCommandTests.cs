using WayFinder.DevTools.Cli.Interactive;
using WayFinder.DevTools.Cli.Interactive.Commands;

namespace WayFinder.DevTools.Tests.Cli;

public sealed class StatusInteractiveCommandTests
{
    [Fact]
    public async Task ExecuteAsync_DisplaysConfiguredProvider()
    {
        var command = new StatusInteractiveCommand(
            "GitHub Copilot"
        );

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(Array.Empty<string>(), output);

        Assert.Equal(
            InteractiveCommandResult.Continue,
            result
        );

        Assert.Contains(
            "GitHub Copilot",
            output.ToString()
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithoutProvider_DisplaysNotConfigured()
    {
        var command = new StatusInteractiveCommand(null);

        using var output = new StringWriter();

        await command.ExecuteAsync(Array.Empty<string>(), output);

        Assert.Contains(
            "Not configured",
            output.ToString()
        );
    }
}
