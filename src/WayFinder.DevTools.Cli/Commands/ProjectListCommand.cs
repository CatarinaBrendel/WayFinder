using System.CommandLine;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Projects;

internal static class ProjectListCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var command = new Command(
            "list",
            "List projects registered with WayFinder"
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
        var projects =
            services.ProjectRegistry.GetAll();

        Console.WriteLine(
            ConsoleTheme.Bold(
                "Registered projects"
            )
        );

        Console.WriteLine(
            ConsoleTheme.Rule
        );

        if (projects.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                "  No waypoints yet."
            );

            Console.WriteLine(
                $"  Register one with {ConsoleTheme.Bold("wayfinder project add <path>")}."
            );

            return;
        }

        foreach (var project in projects)
        {
            Console.WriteLine();

            Console.WriteLine(
                $"{ConsoleTheme.Waypoint} {ConsoleTheme.Bold(project.Name)}"
            );

            Console.WriteLine(
                $"  ID       {project.Id}"
            );

            Console.WriteLine(
                $"  Root     {project.RootPath}"
            );

            Console.WriteLine(
                $"  Status   {GetStatus(project.RootPath)}"
            );
        }

        Console.WriteLine();

        Console.WriteLine(
            projects.Count == 1
                ? "1 project registered"
                : $"{projects.Count} projects registered"
        );
    }

    private static string GetStatus(
        string rootPath
    )
    {
        return Directory.Exists(rootPath)
            ? $"{ConsoleTheme.SuccessMark} {ConsoleTheme.Success("available")}"
            : $"{ConsoleTheme.WarningMark} {ConsoleTheme.Warning("missing")}";
    }
}
