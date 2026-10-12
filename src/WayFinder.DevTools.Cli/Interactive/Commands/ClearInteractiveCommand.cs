namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class ClearInteractiveCommand : IInteractiveCommand
{
    private readonly bool _isTerminal;

    public ClearInteractiveCommand(bool isTerminal)
    {
        _isTerminal = isTerminal;
    }

    public string Name => "/clear";

    public string Description => "Clear the screen";

    public Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_isTerminal)
        {
            Console.Clear();
        }

        return Task.FromResult(
            InteractiveCommandResult.Continue
        );
    }
}
