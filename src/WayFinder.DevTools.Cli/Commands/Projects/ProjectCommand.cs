using System.CommandLine;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Projects;

internal static class ProjectCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var command = new Command(
            "project",
            "Inspect and manage projects"
        );

        command.Subcommands.Add(
            ProjectInfoCommand.Create(services)
        );

        command.Subcommands.Add(
            ProjectInitCommand.Create(services)
        );

        command.Subcommands.Add(
            ProjectAddCommand.Create(services)
        );

        command.Subcommands.Add(
            ProjectListCommand.Create(services)
        );

        command.Subcommands.Add(
            ProjectRemoveCommand.Create(services)
        );

        return command;
    }
}
