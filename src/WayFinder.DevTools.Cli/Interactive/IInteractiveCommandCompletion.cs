namespace WayFinder.DevTools.Cli.Interactive;

public interface IInteractiveCommandCompletion
{
    IReadOnlyList<string> Subcommands { get; }

    IReadOnlyDictionary<string, IReadOnlyList<string>> Options =>
        new Dictionary<string, IReadOnlyList<string>>();
}
