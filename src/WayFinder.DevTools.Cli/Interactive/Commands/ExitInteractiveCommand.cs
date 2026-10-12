using WayFinder.DevTools.Cli.Presentation;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class ExitInteractiveCommand : IInteractiveCommand
{
    public string Name { get; }

    public string Description => "Exit WayFinder";

    public ExitInteractiveCommand(string name = "/exit")
    {
        if (name is not "/exit" and not "/quit")
        {
            throw new ArgumentException(
                "Unsupported exit command.",
                nameof(name)
            );
        }

        Name = name;
    }

    public Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            InteractiveCommandResult.Exit
        );
    }
}
