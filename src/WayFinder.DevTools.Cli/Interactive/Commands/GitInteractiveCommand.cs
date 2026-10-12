using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class GitInteractiveCommand : IInteractiveCommand, IInteractiveCommandCompletion
{
    private readonly WayFinderServices _services;

    public string Name => "/git";

    public string Description => "Inspect Git repository state";

    public GitInteractiveCommand(WayFinderServices services)
    {
        _services = services;
    }

    public IReadOnlyList<string> Subcommands { get; } =
    [
        "status",
        "diff",
        "log"
    ];

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Options { get; } =
    new Dictionary<string, IReadOnlyList<string>>(
        StringComparer.OrdinalIgnoreCase)
    {
        ["diff"] =
        [
            "--staged",
            "--stat",
            "--path"
        ],

        ["log"] =
        [
            "--count",
            "--oneline",
            "--path"
        ]
    };

    public Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        if (arguments.Count == 0)
        {
            WriteHelp(output);
            return Task.FromResult(InteractiveCommandResult.Continue);
        }

        try
        {
            switch (arguments[0].ToLowerInvariant())
            {
                case "status":
                    if (arguments.Count != 1)
                    {
                        ConsolePresenter.Info(
                            output,
                            "Usage: /git status"
                        );
                        break;
                    }

                    ShowStatus(output);
                    break;

                case "diff":
                    ShowDiff(arguments, output);
                    break;

                case "log":
                    ShowLog(arguments, output);
                    break;

                default:
                    ConsolePresenter.Error(
                        output,
                        $"Unknown Git command: {arguments[0]}"
                    );
                    break;
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or TimeoutException
                or FormatException)
        {
            ConsolePresenter.Error(
                output,
                exception.Message
            );
        }

        return Task.FromResult(InteractiveCommandResult.Continue);
    }

    private void ShowStatus(TextWriter output)
    {
        var project = _services.ProjectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No Git repository found."
            );
            return;
        }

        var status = _services.GitStatusReader.Read(project);

        ConsolePresenter.Header(output, "Git Status");

        ConsolePresenter.Property(
            output,
            "Repository",
            ConsoleTheme.Accent(status.RepositoryName)
        );

        ConsolePresenter.Property(
            output,
            "Branch",
            ConsoleTheme.Secondary(
                status.Branch ?? "(detached HEAD)"
            )
        );

        WriteChanges(
            output,
            "Staged",
            status.Staged,
            ConsoleTheme.Success
        );

        WriteChanges(
            output,
            "Unstaged",
            status.Unstaged,
            ConsoleTheme.Warning
        );

        WriteChanges(
            output,
            "Conflicts",
            status.Conflicts,
            ConsoleTheme.Error
        );

        WriteUntracked(output, status.Untracked);

        if (status.Staged.Count == 0
            && status.Unstaged.Count == 0
            && status.Conflicts.Count == 0
            && status.Untracked.Count == 0)
        {
            ConsolePresenter.Success(
                output,
                "Working tree clean."
            );
        }
    }

    private void ShowDiff(
        IReadOnlyList<string> arguments,
        TextWriter output)
    {
        var staged = false;
        var stat = false;
        string? path = null;

        for (var i = 1; i < arguments.Count; i++)
        {
            switch (arguments[i])
            {
                case "--staged":
                    staged = true;
                    break;

                case "--stat":
                    stat = true;
                    break;

                case "--path":
                    if (++i >= arguments.Count ||
                        string.IsNullOrWhiteSpace(arguments[i]))
                    {
                        ConsolePresenter.Info(
                            output,
                            "Usage: /git diff [--staged] [--stat] [--path <file>]"
                        );
                        return;
                    }

                    path = arguments[i];
                    break;

                default:
                    ConsolePresenter.Error(
                        output,
                        $"Unknown option: {arguments[i]}"
                    );
                    return;
            }
        }

        var project = _services.ProjectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No Git repository found."
            );
            return;
        }

        var result = _services.GitDiffReader.Read(
            project,
            new GitDiffRequest(
                Staged: staged,
                StatOnly: stat,
                Path: path
            )
        );

        ConsolePresenter.Header(output, "Git Diff");

        ConsolePresenter.Property(
            output,
            "Repository",
            ConsoleTheme.Accent(result.RepositoryName)
        );

        ConsolePresenter.Property(
            output,
            "Branch",
            ConsoleTheme.Secondary(
                result.Branch ?? "(detached HEAD)"
            )
        );

        ConsolePresenter.Property(
            output,
            "Changes",
            result.Staged ? "staged" : "unstaged"
        );

        if (result.Path is not null)
        {
            ConsolePresenter.Property(
                output,
                "Path",
                result.Path
            );
        }

        if (string.IsNullOrEmpty(result.Content))
        {
            ConsolePresenter.Success(output, "No changes.");
            return;
        }

        output.WriteLine();

        if (result.StatOnly)
        {
            ConsolePresenter.Raw(output, result.Content);
        }
        else
        {
            WritePatch(output, result.Content);
        }

        if (result.Truncated)
        {
            ConsolePresenter.Warning(
                output,
                "Diff output truncated. Use --path to narrow the result."
            );
        }
    }

    private void ShowLog(
        IReadOnlyList<string> arguments,
        TextWriter output)
    {
        var count = 10;
        var oneline = false;
        string? path = null;

        for (var i = 1; i < arguments.Count; i++)
        {
            switch (arguments[i])
            {
                case "--count":
                    if (++i >= arguments.Count ||
                        !int.TryParse(arguments[i], out count))
                    {
                        ConsolePresenter.Info(
                            output,
                            "Usage: /git log [--count <1-100>] [--oneline] [--path <file>]"
                        );
                        return;
                    }

                    break;

                case "--oneline":
                    oneline = true;
                    break;

                case "--path":
                    if (++i >= arguments.Count ||
                        string.IsNullOrWhiteSpace(arguments[i]))
                    {
                        ConsolePresenter.Info(
                            output,
                            "Usage: /git log [--count <1-100>] [--oneline] [--path <file>]"
                        );
                        return;
                    }

                    path = arguments[i];
                    break;

                default:
                    ConsolePresenter.Error(
                        output,
                        $"Unknown option: {arguments[i]}"
                    );
                    return;
            }
        }

        var project = _services.ProjectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No Git repository found."
            );
            return;
        }

        var result = _services.GitLogReader.Read(
            project,
            new GitLogRequest(
                Count: count,
                Path: path
            )
        );

        ConsolePresenter.Header(output, "Git Log");

        ConsolePresenter.Property(
            output,
            "Repository",
            ConsoleTheme.Accent(result.RepositoryName)
        );

        ConsolePresenter.Property(
            output,
            "Branch",
            ConsoleTheme.Secondary(
                result.Branch ?? "(detached HEAD)"
            )
        );

        if (result.Commits.Count == 0)
        {
            ConsolePresenter.Info(
                output,
                "No commits found."
            );
            return;
        }

        ConsolePresenter.Subsection(output, "Commits");

        foreach (var commit in result.Commits)
        {
            var shortHash =
                commit.Hash[..Math.Min(7, commit.Hash.Length)];

            if (oneline)
            {
                ConsolePresenter.Item(
                    output,
                    $"{ConsoleTheme.Accent(shortHash)} {commit.Subject}"
                );

                continue;
            }

            ConsolePresenter.Item(
                output,
                $"{ConsoleTheme.Accent(commit.Hash)}"
            );

            ConsolePresenter.Property(
                output,
                "Author",
                $"{commit.AuthorName} <{commit.AuthorEmail}>"
            );

            ConsolePresenter.Property(
                output,
                "Date",
                $"{commit.AuthoredAt:yyyy-MM-dd HH:mm:ss zzz}"
            );

            ConsolePresenter.Text(
                output,
                commit.Subject
            );

            output.WriteLine();
        }
    }

    private static void WriteChanges(
        TextWriter output,
        string heading,
        IReadOnlyList<GitFileChange> changes,
        Func<string, string> colorize)
    {
        if (changes.Count == 0)
            return;

        ConsolePresenter.Subsection(
            output,
            $"{heading} ({changes.Count})"
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

            ConsolePresenter.Item(
                output,
                $"{colorize(code)}  {path}"
            );
        }
    }

    private static void WriteUntracked(
        TextWriter output,
        IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
            return;

        ConsolePresenter.Subsection(
            output,
            $"Untracked ({paths.Count})"
        );

        foreach (var path in paths)
        {
            ConsolePresenter.Item(
                output,
                $"{ConsoleTheme.Secondary("?")}  {path}"
            );
        }
    }

    private static void WritePatch(
        TextWriter output,
        string content)
    {
        using var reader = new StringReader(content);

        while (reader.ReadLine() is { } line)
        {
            var formatted = line switch
            {
                _ when line.StartsWith("+++", StringComparison.Ordinal)
                    => ConsoleTheme.Bold(line),

                _ when line.StartsWith("---", StringComparison.Ordinal)
                    => ConsoleTheme.Bold(line),

                _ when line.StartsWith('+')
                    => ConsoleTheme.Success(line),

                _ when line.StartsWith('-')
                    => ConsoleTheme.Error(line),

                _ when line.StartsWith("@@", StringComparison.Ordinal)
                    => ConsoleTheme.Secondary(line),

                _ when line.StartsWith("diff --git", StringComparison.Ordinal)
                    => ConsoleTheme.Accent(line),

                _ => line
            };

            output.WriteLine(formatted);
        }
    }

    private static void WriteHelp(TextWriter output)
    {
        ConsolePresenter.Header(output, "Git commands");

        ConsolePresenter.Command(
            output,
            "/git status",
            "Show working tree changes"
        );

        ConsolePresenter.Command(
            output,
            "/git diff",
            "Show changes (--staged, --stat, --path)"
        );

        ConsolePresenter.Command(
            output,
            "/git log",
            "Show commits (--count, --oneline, --path)"
        );
    }
}
