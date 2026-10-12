using System.CommandLine;
using WayFinder.DevTools.Application.Projects.Initialization;
using WayFinder.DevTools.Cli.Presentation;
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

    private static int Execute(
        WayFinderServices services
    )
    {
        var project =
            services.ProjectLocator.Locate(
                Environment.CurrentDirectory
            );

        if (project is null)
        {
            Console.Error.WriteLine(
                $"{ConsoleTheme.ErrorMark} No project found."
            );

            return ExitCodes.Failure;
        }

        if (services.ProjectFileSystem.FileExists(
                project,
                "wayfinder.json"))
        {
            Console.WriteLine(
                $"{ConsoleTheme.SuccessMark} {ConsoleTheme.Bold(project.Name)} is already configured"
            );

            Console.WriteLine(
                "  wayfinder.json already exists."
            );

            Console.WriteLine();
            Console.WriteLine(
                "Nothing to change. This route is already mapped."
            );

            return ExitCodes.Success;
        }

        var initialization =
            services.ProjectInitializer.Prepare(
                project
            );

        WritePreview(
            project.RootPath,
            initialization
        );

        Console.WriteLine();

        Console.Write(
            $"Create this configuration? {ConsoleTheme.Warning("[y/N]")} "
        );

        var answer =
            Console.ReadLine();

        if (!IsConfirmed(answer))
        {
            Console.WriteLine();
            Console.WriteLine(
                "No changes made."
            );

            return ExitCodes.Success;
        }

        services.ProjectInitializer.Initialize(
            project,
            initialization
        );

        Console.WriteLine();

        Console.WriteLine(
            $"{ConsoleTheme.SuccessMark} Created {ConsoleTheme.Bold("wayfinder.json")}"
        );

        Console.WriteLine(
            "  WayFinder knows this route now."
        );

        return ExitCodes.Success;
    }

    private static void WritePreview(
        string projectRoot,
        ProjectInitialization initialization
    )
    {
        Console.WriteLine(
            $"{ConsoleTheme.Waypoint} {ConsoleTheme.Bold("Project setup")}"
        );

        Console.WriteLine(
            ConsoleTheme.Rule
        );

        Console.WriteLine();

        Console.WriteLine(
            $"  Name          {initialization.ProjectName}"
        );

        Console.WriteLine(
            $"  Manifest      {Path.Combine(
                projectRoot,
                initialization.ManifestPath
            )}"
        );

        Console.WriteLine();

        Console.WriteLine(
            ConsoleTheme.Bold(
                "Detected technologies"
            )
        );

        if (initialization.Technologies.Count == 0)
        {
            Console.WriteLine(
                "  none"
            );

            return;
        }

        foreach (var technology in initialization.Technologies)
        {
            Console.WriteLine(
                $"  {ConsoleTheme.SuccessMark} {technology}"
            );
        }
    }

    private static bool IsConfirmed(
        string? answer
    )
    {
        return string.Equals(
                   answer,
                   "y",
                   StringComparison.OrdinalIgnoreCase
               )
               || string.Equals(
                   answer,
                   "yes",
                   StringComparison.OrdinalIgnoreCase
               );
    }
}
