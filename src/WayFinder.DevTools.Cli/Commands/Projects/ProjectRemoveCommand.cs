using System.CommandLine;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Projects;

internal static class ProjectRemoveCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var idArgument =
            new Argument<Guid>(
                "id"
            )
            {
                Description =
                    "ID of the registered project to remove",
                Arity = ArgumentArity.ExactlyOne,
            };

        var command = new Command(
            "remove",
            "Remove a project from the WayFinder registry"
        );

        command.Arguments.Add(idArgument);

        command.SetAction(
            parseResult =>
                Execute(
                    services,
                    parseResult.GetValue(
                        idArgument
                    )
                )
        );

        return command;
    }

    private static int Execute(
        WayFinderServices services,
        Guid id
    )
    {
        if (id == Guid.Empty)
        {
            Console.Error.WriteLine(
                $"{ConsoleTheme.ErrorMark} A valid project ID is required."
            );

            return ExitCodes.Failure;
        }

        var project =
            services.ProjectRegistry.FindById(id);

        if (project is null)
        {
            Console.Error.WriteLine(
                $"{ConsoleTheme.ErrorMark} No registered project found with ID '{id}'."
            );

            return ExitCodes.Failure;
        }

        if (!services.ProjectRegistry.Remove(id))
        {
            Console.Error.WriteLine(
                $"{ConsoleTheme.ErrorMark} Could not remove registered project '{id}'."
            );

            return ExitCodes.Failure;
        }

        Console.WriteLine(
            $"{ConsoleTheme.SuccessMark} Removed {ConsoleTheme.Bold(project.Name)}"
        );

        Console.WriteLine(
            $"  {project.RootPath}"
        );

        Console.WriteLine();

        Console.WriteLine(
            ConsoleTheme.Bold(
                "AI visibility"
            )
        );

        Console.WriteLine(
            $"  Read     {ConsoleTheme.WarningMark} {ConsoleTheme.Warning("revoked")}"
        );

        Console.WriteLine();

        Console.WriteLine(
            "This waypoint is no longer visible to AI adapters."
        );

        return ExitCodes.Success;
    }
}
