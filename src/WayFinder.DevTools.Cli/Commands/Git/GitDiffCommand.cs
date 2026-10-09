using System.CommandLine;
using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Git;

internal static class GitDiffCommand
{
    public static Command Create(WayFinderServices services)
    {
        var stagedOption = new Option<bool>("--staged")
        {
            Description = "Show staged changes instead of unstaged changes."
        };

        var statOption = new Option<bool>("--stat")
        {
            Description = "Show change statistics instead of patch content."
        };

        var pathOption = new Option<string?>("--path")
        {
            Description = "Restrict the diff to a repository-relative file."
        };

        var command = new Command(
            "diff",
            "Show local Git changes."
        );

        command.Options.Add(stagedOption);
        command.Options.Add(statOption);
        command.Options.Add(pathOption);

        command.SetAction(parseResult =>
        {
            var staged = parseResult.GetValue(stagedOption);
            var stat = parseResult.GetValue(statOption);
            var path = parseResult.GetValue(pathOption);

            return Execute(
                services,
                new GitDiffRequest(
                    Staged: staged,
                    StatOnly: stat,
                    Path: path
                )
            );
        });

        return command;
    }

    private static int Execute(
        WayFinderServices services,
        GitDiffRequest request
    )
    {
        try
        {
            var project = services.ProjectLocator.Locate(
                Environment.CurrentDirectory
            );

            if (project is null)
            {
                Console.Error.WriteLine(
                    ConsoleTheme.Error("✗ No Git repository found.")
                );

                return ExitCodes.Failure;
            }

            var result = services.GitDiffReader.Read(
                project,
                request
            );

            WriteResult(result);

            return ExitCodes.Success;
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or TimeoutException
                or FormatException
        )
        {
            Console.Error.WriteLine(
                ConsoleTheme.Error($"✗ {exception.Message}")
            );

            return ExitCodes.Failure;
        }
    }

    private static void WriteResult(GitDiffResult result)
    {
        Console.WriteLine(
            $"{ConsoleTheme.Waypoint} {ConsoleTheme.Bold("Git Diff")}"
        );

        Console.WriteLine(ConsoleTheme.Rule);

        Console.WriteLine(
            $"  Repository  {ConsoleTheme.Accent(result.RepositoryName)}"
        );

        Console.WriteLine(
            $"  Branch      {ConsoleTheme.Secondary(result.Branch ?? "(detached HEAD)")}"
        );

        Console.WriteLine(
            $"  Changes     {ConsoleTheme.Secondary(result.Staged ? "staged" : "unstaged")}"
        );

        if (result.Path is not null)
        {
            Console.WriteLine(
                $"  Path        {result.Path}"
            );
        }

        Console.WriteLine();

        if (string.IsNullOrEmpty(result.Content))
        {
            Console.WriteLine(
                $"  {ConsoleTheme.SuccessMark} No changes."
            );

            return;
        }

        // Preserve Git's patch text without adding ANSI formatting.
        Console.Write(result.Content);

        if (result.StatOnly)
        {
            Console.Write(result.Content);

            if (!result.Content.EndsWith('\n'))
            {
                Console.WriteLine();
            }
        }
        else
        {
            WritePatch(result.Content);
        }

        if (result.Truncated)
        {
            Console.WriteLine();

            Console.WriteLine(
                $"  {ConsoleTheme.WarningMark} Diff output truncated. Use --path to narrow the result."
            );
        }
    }

    private static void WritePatch(string content)
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

            Console.WriteLine(formatted);
        }
    }
}
