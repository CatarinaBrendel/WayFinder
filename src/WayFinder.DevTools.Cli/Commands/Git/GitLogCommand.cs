using System.CommandLine;
using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Git;

internal static class GitLogCommand
{
    public static Command Create(WayFinderServices services)
    {
        var countOption = new Option<int>("--count")
        {
            Description = "Maximum number of commits (1–100).",
            DefaultValueFactory = _ => 10
        };

        var onelineOption = new Option<bool>("--oneline")
        {
            Description = "Display one line per commit."
        };

        var pathOption = new Option<string?>("--path")
        {
            Description = "Filter history by repository-relative path."
        };

        var command = new Command(
            "log",
            "Show recent Git commits."
        );

        command.Options.Add(countOption);
        command.Options.Add(onelineOption);
        command.Options.Add(pathOption);

        command.SetAction(parseResult =>
        {
            var count = parseResult.GetValue(countOption);
            var oneline = parseResult.GetValue(onelineOption);
            var path = parseResult.GetValue(pathOption);

            try
            {
                var project = services.ProjectLocator.Locate(
                    Directory.GetCurrentDirectory()
                );

                if (project is null)
                {
                    throw new InvalidOperationException(
                        "No WayFinder project found in the current directory."
                    );
                }

                var result = services.GitLogReader.Read(
                    project,
                    new GitLogRequest(
                        Count: count,
                        Path: path
                    )
                );

                Render(result, oneline);

                return ExitCodes.Success;
            }
            catch (Exception exception) when (
                exception is ArgumentException
                    or InvalidOperationException
                    or TimeoutException
                    or FormatException)
            {
                Console.Error.WriteLine(
                    ConsoleTheme.Error(exception.Message)
                );

                return ExitCodes.Failure;
            }
        });

        return command;
    }

    private static void Render(
        GitLogResult result,
        bool oneline
    )
    {
        Console.WriteLine(
            $"{ConsoleTheme.Waypoint} " +
            $"{ConsoleTheme.Bold(result.RepositoryName)} " +
            ConsoleTheme.Secondary(
                $"({result.Branch ?? "detached HEAD"})"
            )
        );

        Console.WriteLine();

        if (result.Commits.Count == 0)
        {
            Console.WriteLine(
                ConsoleTheme.Warning("No commits found.")
            );

            return;
        }

        foreach (var commit in result.Commits)
        {
            if (oneline)
            {
                RenderOneline(commit);
            }
            else
            {
                RenderCommit(commit);
            }
        }
    }

    private static void RenderOneline(GitCommit commit)
    {
        var shortHash = commit.Hash[..Math.Min(7, commit.Hash.Length)];

        Console.WriteLine(
            $"{ConsoleTheme.Accent(shortHash)} {commit.Subject}"
        );
    }

    private static void RenderCommit(GitCommit commit)
    {
        Console.WriteLine(
            $"{ConsoleTheme.Accent("commit")} " +
            ConsoleTheme.Accent(commit.Hash)
        );

        Console.WriteLine(
            $"Author: {commit.AuthorName} <{commit.AuthorEmail}>"
        );

        Console.WriteLine(
            $"Date:   {commit.AuthoredAt:yyyy-MM-dd HH:mm:ss zzz}"
        );

        Console.WriteLine();
        Console.WriteLine($"    {commit.Subject}");
        Console.WriteLine();
    }
}
