using WayFinder.DevTools.Cli.Presentation;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class StatusInteractiveCommand : IInteractiveCommand
{
    private readonly string? _providerName;

    public StatusInteractiveCommand(string? providerName)
    {
        _providerName = providerName;
    }

    public string Name => "/status";

    public string Description => "Show session information";

    public Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        InteractivePresentation.WriteStatus(
            output,
            _providerName
        );

        return Task.FromResult(
            InteractiveCommandResult.Continue
        );
    }
}
