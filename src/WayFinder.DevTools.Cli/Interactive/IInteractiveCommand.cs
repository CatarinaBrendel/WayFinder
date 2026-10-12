namespace WayFinder.DevTools.Cli.Interactive;

public enum InteractiveCommandResult
{
    Continue,
    Exit
}

public interface IInteractiveCommand
{
    string Name { get; }

    string Description { get; }

    Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default);
}
