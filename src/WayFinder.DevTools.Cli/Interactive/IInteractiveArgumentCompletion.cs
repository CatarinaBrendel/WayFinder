namespace WayFinder.DevTools.Cli.Interactive;

public interface IInteractiveArgumentCompletion
{
    IReadOnlyList<string> CompleteArguments(
        IReadOnlyList<string> arguments,
        string prefix);
}
