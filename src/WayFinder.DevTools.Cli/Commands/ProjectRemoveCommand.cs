using System.CommandLine;
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
                Description = "ID of the registered project to remove",
                Arity = ArgumentArity.ExactlyOne,
            };

        var command = new Command(
            "remove",
            "Remove a project from the WayFinder registry"
        );

        command.Arguments.Add(idArgument);

        command.SetAction(
            parseResult => Execute(
                services,
                parseResult.GetValue(idArgument)
            )
        );

        return command;
    }

    private static void Execute(
        WayFinderServices services,
        Guid id
    )
    {
        if (id == Guid.Empty)
        {
            Console.Error.WriteLine(
                "A valid project ID is required."
            );

            return;
        }

        var project =
            services.ProjectRegistry.FindById(id);

        if (project is null)
        {
            Console.Error.WriteLine(
                $"No registered project found with ID '{id}'."
            );

            return;
        }

        if (!services.ProjectRegistry.Remove(id))
        {
            Console.Error.WriteLine(
                $"Could not remove registered project '{id}'."
            );

            return;
        }

        Console.WriteLine(
            $"Removed {project.Name}"
        );

        Console.WriteLine(
            $"  {project.RootPath}"
        );

        Console.WriteLine();
        Console.WriteLine(
            "AI-facing WayFinder adapters no longer have access to this project."
        );
    }
}
