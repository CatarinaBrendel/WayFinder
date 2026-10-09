using System.CommandLine;
using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Git;

internal static class GitStatusCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var command = new Command(
            "status",
            "Show local Git repository changes."
        );

        command.SetAction(
            _ => Execute(services)
        );

        return command;
    }

    private static int Execute(
        WayFinderServices services
    )
    {
        var project = services.ProjectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            Console.Error.WriteLine(
                ConsoleTheme.Error(
                    "✗ No Git repository found."
                )
            );

            return ExitCodes.Failure;
        }

        var status = services.GitStatusReader.Read(project);

        WriteStatus(status);

        return ExitCodes.Success;
    }

    private static void WriteStatus(
        GitStatus status
    )
    {
        Console.WriteLine(
            $"{ConsoleTheme.Waypoint} {ConsoleTheme.Bold("Git Status")}"
        );

        Console.WriteLine(
            ConsoleTheme.Rule
        );

        Console.WriteLine(
            $"  Repository  {ConsoleTheme.Accent(status.RepositoryName)}"
        );

        Console.WriteLine(
            $"  Branch      {ConsoleTheme.Secondary(status.Branch ?? "(detached HEAD)")}"
        );

        Console.WriteLine();

        WriteChanges(
            "Staged",
            status.Staged,
            ConsoleTheme.Success
        );

        WriteChanges(
            "Unstaged",
            status.Unstaged,
            ConsoleTheme.Warning
        );

        WriteChanges(
            "Conflicts",
            status.Conflicts,
            ConsoleTheme.Error
        );

        WriteUntracked(
            status.Untracked
        );

        if (status.Staged.Count == 0
            && status.Unstaged.Count == 0
            && status.Conflicts.Count == 0
            && status.Untracked.Count == 0)
        {
            Console.WriteLine(
                $"  {ConsoleTheme.SuccessMark} Working tree clean."
            );
        }
    }

    private static void WriteChanges(
        string heading,
        IReadOnlyList<GitFileChange> changes,
        Func<string, string> colorize
    )
    {
        if (changes.Count == 0)
        {
            return;
        }

        Console.WriteLine(
            ConsoleTheme.Bold(
                $"{heading} ({changes.Count})"
            )
        );

        foreach (var change in changes)
        {
            var code = change.Kind switch
            {
                GitChangeKind.Added => "A",
                GitChangeKind.Modified => "M",
                GitChangeKind.Deleted => "D",
                GitChangeKind.Renamed => "R",
                GitChangeKind.Copied => "C",
                GitChangeKind.TypeChanged => "T",
                GitChangeKind.Unmerged => "U",

                _ => throw new InvalidOperationException(
                    $"Unsupported Git change kind: {change.Kind}."
                )
            };

            var path = change.OriginalPath is null
                ? change.Path
                : $"{change.OriginalPath} -> {change.Path}";

            Console.WriteLine(
                $"  {colorize(code)}  {path}"
            );
        }

        Console.WriteLine();
    }

    private static void WriteUntracked(
        IReadOnlyList<string> paths
    )
    {
        if (paths.Count == 0)
        {
            return;
        }

        Console.WriteLine(
            ConsoleTheme.Bold(
                $"Untracked ({paths.Count})"
            )
        );

        foreach (var path in paths)
        {
            Console.WriteLine(
                $"  {ConsoleTheme.Secondary("?")}  {path}"
            );
        }

        Console.WriteLine();
    }
}
