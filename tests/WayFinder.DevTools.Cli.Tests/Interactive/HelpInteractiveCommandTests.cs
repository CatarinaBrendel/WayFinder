using WayFinder.DevTools.Cli.Interactive;
using WayFinder.DevTools.Cli.Interactive.Commands;

namespace WayFinder.DevTools.Tests.Cli;

public sealed class HelpInteractiveCommandTests
{
    [Fact]
    public async Task ExecuteAsync_DisplaysRegisteredCommands()
    {
        var registry = new InteractiveCommandRegistry();

        registry.Register(
            new HelpInteractiveCommand(registry)
        );

        registry.Register(
            new ExitInteractiveCommand()
        );

        using var output = new StringWriter();

        var command = new HelpInteractiveCommand(registry);

        var result = await command.ExecuteAsync(Array.Empty<string>(), output);

        Assert.Equal(
            InteractiveCommandResult.Continue,
            result
        );

        Assert.Contains("/help", output.ToString());
        Assert.Contains("/exit", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_IncludesCommandsRegisteredLater()
    {
        var registry = new InteractiveCommandRegistry();

        var command = new HelpInteractiveCommand(registry);

        registry.Register(command);

        using var output = new StringWriter();

        await command.ExecuteAsync(Array.Empty<string>(), output);

        Assert.Contains("/help", output.ToString());

        registry.Register(
            new ExitInteractiveCommand()
        );

        output.GetStringBuilder().Clear();

        await command.ExecuteAsync(Array.Empty<string>(), output);

        Assert.Contains("/exit", output.ToString());
    }
}
