using WayFinder.DevTools.Cli.Interactive;
using WayFinder.DevTools.Cli.Interactive.Commands;

namespace WayFinder.DevTools.Tests.Cli;

public sealed class ClearInteractiveCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WithoutTerminal_ReturnsContinue()
    {
        var command = new ClearInteractiveCommand(false);

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(Array.Empty<string>(), output);

        Assert.Equal(
            InteractiveCommandResult.Continue,
            result
        );
    }

    [Fact]
    public void Command_HasCorrectMetadata()
    {
        var command = new ClearInteractiveCommand(false);

        Assert.Equal("/clear", command.Name);
        Assert.Equal("Clear the screen", command.Description);
    }
}
