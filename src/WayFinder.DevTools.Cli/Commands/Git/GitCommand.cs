using System.CommandLine;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Git;

internal static class GitCommand
{
    public static Command Create(WayFinderServices services)
    {
        var command = new Command(
            "git",
            "Inspect Git repository state."
        );

        command.Subcommands.Add(
            GitStatusCommand.Create(services)
        );

        command.Subcommands.Add(
            GitDiffCommand.Create(services)
        );

        command.Subcommands.Add(
            GitLogCommand.Create(services)
        );

        return command;
    }
}
