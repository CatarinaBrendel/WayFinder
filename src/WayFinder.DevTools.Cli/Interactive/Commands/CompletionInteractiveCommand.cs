using WayFinder.DevTools.Cli.Presentation;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class CompletionInteractiveCommand
    : IInteractiveCommand
{
    public string Name => "completion";

    public string Description =>
        "Show shell completion instructions";

    public Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ConsolePresenter.Header(
            output,
            "Shell completion"
        );

        ConsolePresenter.Text(
            output,
            "Install zsh completion using:"
        );

        ConsolePresenter.Command(
            output,
            "wayfinder completion install",
            ""
        );

        return Task.FromResult(
            InteractiveCommandResult.Continue
        );
    }
}
