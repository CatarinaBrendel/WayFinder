using System.CommandLine;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Projects;

internal static class ProjectInitCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var command = new Command(
            "init",
            "Create WayFinder metadata for the current project"
        );

        command.SetAction(
            _ => Execute(services)
        );

        return command;
    }

    private static void Execute(
        WayFinderServices services
    )
    {
        var project = services.ProjectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            Console.Error.WriteLine("No project found.");
            return;
        }

        if (services.ProjectFileSystem.FileExists(
                project,
                "wayfinder.json"))
        {
            Console.Error.WriteLine(
                "This project already contains wayfinder.json."
            );

            return;
        }

        var initialization =
            services.ProjectInitializer.Prepare(project);

        Console.WriteLine("WayFinder will create:");

        Console.WriteLine(
            $"  {Path.Combine(project.RootPath, initialization.ManifestPath)}"
        );

        Console.WriteLine();
        Console.WriteLine("Detected technologies:");

        if (initialization.Technologies.Count == 0)
        {
            Console.WriteLine("  none");
        }
        else
        {
            foreach (var technology in initialization.Technologies)
            {
                Console.WriteLine($"  {technology}");
            }
        }

        Console.WriteLine();
        Console.Write("Create project manifest? [y/N] ");

        var answer = Console.ReadLine();

        if (!string.Equals(
                answer,
                "y",
                StringComparison.OrdinalIgnoreCase)
            && !string.Equals(
                answer,
                "yes",
                StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Cancelled.");
            return;
        }

        services.ProjectInitializer.Initialize(
            project,
            initialization
        );

        Console.WriteLine();
        Console.WriteLine("Created wayfinder.json.");
    }
}
