using System.CommandLine;
using WayFinder.DevTools.Cli.Presentation;
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
                Description =
                    "Path inside the project to register",
            };

        var command = new Command(
            "add",
            "Register a project with WayFinder"
        );

        command.Arguments.Add(pathArgument);

        command.SetAction(
            parseResult =>
                Execute(
                    services,
                    parseResult.GetValue(
                        pathArgument
                    )
                )
        );

        return command;
    }

    private static int Execute(
    WayFinderServices services,
    string? path
)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine(
                $"{ConsoleTheme.ErrorMark} A project path is required."
            );

            return ExitCodes.Failure;
        }

        try
        {
            var fullPath =
                Path.GetFullPath(path);

            var project =
                services.ProjectLocator.Locate(
                    fullPath
                );

            if (project is null)
            {
                Console.Error.WriteLine(
                    $"{ConsoleTheme.ErrorMark} No project found."
                );

                return ExitCodes.Failure;
            }

            var existing =
                services.ProjectRegistry.FindByRootPath(
                    project.RootPath
                );

            if (existing is not null)
            {
                WriteAlreadyRegistered(
                    existing.Name,
                    existing.RootPath
                );

                return ExitCodes.Success;
            }

            var registered =
                services.ProjectRegistry.Add(project);

            WriteRegistered(
                registered.Name,
                registered.RootPath
            );

            return ExitCodes.Success;
        }
        catch (DirectoryNotFoundException)
        {
            Console.Error.WriteLine(
                $"{ConsoleTheme.ErrorMark} Path does not exist: {path}"
            );

            return ExitCodes.Failure;
        }
    }

    private static void WriteRegistered(
        string name,
        string rootPath
    )
    {
        Console.WriteLine(
            $"{ConsoleTheme.SuccessMark} Registered {ConsoleTheme.Bold(name)}"
        );

        Console.WriteLine(
            $"  {rootPath}"
        );

        Console.WriteLine();

        Console.WriteLine(
            ConsoleTheme.Bold(
                "AI visibility"
            )
        );

        Console.WriteLine(
            $"  Read     {ConsoleTheme.SuccessMark} {ConsoleTheme.Success("granted")}"
        );

        Console.WriteLine(
            $"  Write    {ConsoleTheme.WarningMark} {ConsoleTheme.Warning("not granted")}"
        );

        Console.WriteLine();

        Console.WriteLine(
            "WayFinder can show AI adapters the route, but they can't change it."
        );
    }

    private static void WriteAlreadyRegistered(
        string name,
        string rootPath
    )
    {
        Console.WriteLine(
            $"{ConsoleTheme.SuccessMark} {ConsoleTheme.Bold(name)} is already registered"
        );

        Console.WriteLine(
            $"  {rootPath}"
        );

        Console.WriteLine();

        Console.WriteLine(
            "Nothing to change. This waypoint is already known."
        );
    }
}
