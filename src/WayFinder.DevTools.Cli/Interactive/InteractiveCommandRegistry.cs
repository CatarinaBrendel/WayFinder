namespace WayFinder.DevTools.Cli.Interactive;

public sealed class InteractiveCommandRegistry
{
    private readonly Dictionary<string, IInteractiveCommand>
        _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IInteractiveCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_commands.TryAdd(command.Name, command))
        {
            throw new InvalidOperationException(
                $"Command '{command.Name}' is already registered."
            );
        }
    }

    public IReadOnlyCollection<IInteractiveCommand> Commands =>
        _commands.Values;

    public bool TryGet(
        string name,
        out IInteractiveCommand? command)
    {
        return _commands.TryGetValue(name, out command);
    }
}
