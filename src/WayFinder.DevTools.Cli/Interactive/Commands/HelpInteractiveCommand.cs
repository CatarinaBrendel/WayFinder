using WayFinder.DevTools.Cli.Presentation;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class HelpInteractiveCommand : IInteractiveCommand
{
    private readonly InteractiveCommandRegistry _registry;

    public HelpInteractiveCommand(InteractiveCommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _registry = registry;
    }

    public string Name => "/help";

    public string Description => "Show available commands";

    public Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        InteractivePresentation.WriteHelp(
            output,
            _registry.Commands
        );

        return Task.FromResult(
            InteractiveCommandResult.Continue
        );
    }
}
