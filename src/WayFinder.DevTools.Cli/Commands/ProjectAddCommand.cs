using System.CommandLine;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Projects;

internal static class ProjectAddCommand
{
    public static Command Create(
    WayFinderServices services
)
    {
        var pathArgument =
            new Argument<string>(
                "path"
            )
            {
                Description = "Path inside the project to register",
            };

        var command = new Command(
            "add",
            "Register a project with WayFinder"
        );

        command.Arguments.Add(pathArgument);

        command.SetAction(
            parseResult => Execute(
                services,
                parseResult.GetValue(pathArgument)
            )
        );

        return command;
    }

    private static void Execute(
        WayFinderServices services,
        string? path
    )
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine(
                "A project path is required."
            );

            return;
        }

        var fullPath = Path.GetFullPath(path);

        var project = services.ProjectLocator.Locate(
            fullPath
        );

        if (project is null)
        {
            Console.Error.WriteLine("No project found.");
            return;
        }

        var existing =
            services.ProjectRegistry.FindByRootPath(
                project.RootPath
            );

        if (existing is not null)
        {
            Console.WriteLine(
                $"Already registered: {existing.Name}"
            );

            Console.WriteLine(
                $"  {existing.RootPath}"
            );

            return;
        }

        var registered =
            services.ProjectRegistry.Add(project);

        Console.WriteLine(
            $"Registered {registered.Name}"
        );

        Console.WriteLine(
            $"  {registered.RootPath}"
        );

        Console.WriteLine();
        Console.WriteLine(
            "AI-facing WayFinder adapters may now read this project."
        );

        Console.WriteLine(
            "Write access has not been granted."
        );
    }
}
