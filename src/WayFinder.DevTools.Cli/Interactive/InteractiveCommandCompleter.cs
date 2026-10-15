namespace WayFinder.DevTools.Cli.Interactive;

internal sealed class InteractiveCommandCompleter
{
    private readonly InteractiveCommandRegistry _registry;

    public InteractiveCommandCompleter(
        InteractiveCommandRegistry registry)
    {
        _registry = registry
            ?? throw new ArgumentNullException(nameof(registry));
    }

    public IReadOnlyList<string> Complete(string input)
    {
        if (string.IsNullOrEmpty(input) ||
            !input.StartsWith('/'))
        {
            return [];
        }

        var separatorIndex = input.IndexOf(' ');

        if (separatorIndex < 0)
        {
            return CompleteCommands(input);
        }

        var commandName = input[..separatorIndex];

        if (!_registry.TryGet(commandName, out var command) ||
            command is not IInteractiveCommandCompletion completion)
        {
            return [];
        }

        var remainder = input[(separatorIndex + 1)..];

        // Commands with direct options, such as /tree and /context.
        if (completion.Subcommands.Count == 0 &&
            completion.Options.TryGetValue("", out var directOptions))
        {
            return CompleteOptions(
                input,
                separatorIndex,
                directOptions);
        }

        // Complete the first argument as a subcommand.
        if (!remainder.Any(char.IsWhiteSpace))
        {
            return CompleteSubcommands(
                commandName,
                remainder,
                completion.Subcommands);
        }

        // Complete argument values when supported by the command.
        if (command is IInteractiveArgumentCompletion argumentCompletion)
        {
            var lastSeparatorIndex = input.LastIndexOf(' ');
            var prefix = input[(lastSeparatorIndex + 1)..];

            var completedArguments = input[
                (separatorIndex + 1)..lastSeparatorIndex
            ].Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            var candidates = argumentCompletion.CompleteArguments(
                completedArguments,
                prefix);

            if (candidates.Count > 0)
            {
                return candidates
                    .Select(candidate =>
                        $"{input[..(lastSeparatorIndex + 1)]}{candidate}")
                    .ToArray();
            }
        }

        // Commands with subcommand-specific options, such as /git.
        var tokens = remainder.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0 ||
            !completion.Options.TryGetValue(
                tokens[0],
                out var options))
        {
            return [];
        }

        return CompleteOptions(
            input,
            separatorIndex,
            options);
    }

    private IReadOnlyList<string> CompleteCommands(string prefix)
    {
        return _registry.Commands
            .Select(command => command.Name)
            .Where(name => name.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(
                name => name,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> CompleteSubcommands(
        string commandName,
        string prefix,
        IReadOnlyList<string> subcommands)
    {
        return subcommands
            .Where(name => name.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            .Select(name => $"{commandName} {name}")
            .OrderBy(
                name => name,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> CompleteOptions(
        string input,
        int commandSeparatorIndex,
        IReadOnlyList<string> options)
    {
        var lastSeparatorIndex = input.LastIndexOf(' ');
        var prefix = input[(lastSeparatorIndex + 1)..];

        // Only complete options, not task text or argument values.
        if (prefix.Length > 0 &&
            !prefix.StartsWith("--", StringComparison.Ordinal))
        {
            return [];
        }

        var previousArguments = input[
            (commandSeparatorIndex + 1)..lastSeparatorIndex
        ].Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        // Options requiring a value must not trigger completion
        // while their value is being entered.
        if (previousArguments.Length > 0 &&
            previousArguments[^1] is "--budget" or "--depth" &&
            prefix.Length == 0)
        {
            return [];
        }

        var usedOptions = previousArguments
            .Where(argument =>
                argument.StartsWith(
                    "--",
                    StringComparison.Ordinal))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return options
            .Where(option =>
                option.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase)
                && !usedOptions.Contains(option))
            .Select(option =>
                $"{input[..(lastSeparatorIndex + 1)]}{option}")
            .OrderBy(
                option => option,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
