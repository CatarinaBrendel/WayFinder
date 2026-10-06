using System.CommandLine;
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

        Console.WriteLine("Registered projects");

        if (projects.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("  none");
            return;
        }

        foreach (var project in projects)
        {
            Console.WriteLine();
            Console.WriteLine(project.Name);

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
    }

    private static string GetStatus(
        string rootPath
    )
    {
        return Directory.Exists(rootPath)
            ? "available"
            : "missing";
    }
}
