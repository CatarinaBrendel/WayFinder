using System.CommandLine;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Repositories;

internal static class RepositorySearchCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var queryArgument =
            new Argument<string>("query")
            {
                Description =
                    "Text to search for in the current repository"
            };

        var command =
            new Command(
                "search",
                "Search text files in the current repository"
            );

        command.Arguments.Add(queryArgument);

        command.SetAction(
            parseResult =>
            {
                var query =
                    parseResult.GetValue(
                        queryArgument
                    );

                return Execute(
                    services,
                    query
                );
            }
        );

        return command;
    }

    private static int Execute(
        WayFinderServices services,
        string? query
    )
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.Error.WriteLine(
                "A search query is required."
            );

            return ExitCodes.Failure;
        }

        var project =
            services.ProjectLocator.Locate(
                Directory.GetCurrentDirectory()
            );

        if (project is null)
        {
            Console.Error.WriteLine(
                "No Git project found."
            );

            return ExitCodes.Failure;
        }

        var result =
            services.RepositorySearcher.Search(
                project,
                query
            );

        foreach (var match in result.Matches)
        {
            Console.WriteLine(
                $"{match.Path}:{match.LineNumber}"
            );

            Console.WriteLine(
                $"  {match.Line}"
            );
        }

        if (result.Matches.Count > 0)
        {
            Console.WriteLine();
        }

        Console.WriteLine(
            result.Matches.Count == 1
                ? "1 match"
                : $"{result.Matches.Count} matches"
        );

        if (result.Truncated)
        {
            Console.Error.WriteLine(
                "[results truncated]"
            );
        }

        return ExitCodes.Success;
    }
}
