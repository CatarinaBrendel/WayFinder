using System.CommandLine;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Repositories;

public static class RepositoryCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var command =
            new Command(
                "repo",
                "Inspect repository content"
            );

        command.Subcommands.Add(
            RepositoryReadCommand.Create(services)
        );

        command.Subcommands.Add(
            RepositorySearchCommand.Create(services)
        );

        return command;
    }
}
