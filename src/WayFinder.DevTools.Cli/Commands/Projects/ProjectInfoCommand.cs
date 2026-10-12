using System.CommandLine;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Manifest;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Projects;

internal static class ProjectInfoCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var command = new Command(
            "info",
            "Show information about the current project"
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

        var manifest =
            services.ProjectManifestReader.Read(project);

        var inspection =
            services.ProjectInspector.Inspect(project);

        Console.WriteLine(
            $"{ConsoleTheme.Waypoint} {ConsoleTheme.Bold(project.Name)}"
        );

        Console.WriteLine(
            ConsoleTheme.Rule
        );

        Console.WriteLine();

        Console.WriteLine(
            ConsoleTheme.Bold("Project")
        );

        Console.WriteLine(
            $"  Root       {project.RootPath}"
        );

        Console.WriteLine(
            $"  Git        {FormatState(project.IsGitRepository, "yes", "no")}"
        );

        Console.WriteLine(
            $"  Manifest   {FormatState(
                manifest is not null,
                "configured",
                "not configured"
            )}"
        );

        if (manifest is not null)
        {
            WriteManifest(manifest);
        }

        WriteInspection(inspection);

        if (manifest is null)
        {
            WriteManifestHint();
        }

        return ExitCodes.Success;
    }

    private static void WriteManifest(
        ProjectManifest manifest
    )
    {
        Console.WriteLine();

        Console.WriteLine(
            ConsoleTheme.Bold("Declared")
        );

        if (manifest.Technologies.Count == 0)
        {
            Console.WriteLine("  none");
            return;
        }

        foreach (var technology in manifest.Technologies)
        {
            Console.WriteLine(
                $"  {ConsoleTheme.Waypoint} {technology}"
            );
        }
    }

    private static void WriteInspection(
        ProjectInspection inspection
    )
    {
        var detected =
            inspection.Artifacts
                .Where(
                    entry =>
                        entry.Value.Count > 0
                )
                .ToArray();

        if (detected.Length == 0)
        {
            return;
        }

        Console.WriteLine();

        Console.WriteLine(
            ConsoleTheme.Bold("Detected")
        );

        foreach (var (detector, artifacts) in detected)
        {
            Console.WriteLine(
                $"  {ConsoleTheme.SuccessMark} {ConsoleTheme.Bold(detector)}"
            );

            foreach (var artifact in artifacts)
            {
                Console.WriteLine(
                    $"      {artifact.Type,-12} {artifact.Path}"
                );
            }
        }
    }

    private static void WriteManifestHint()
    {
        Console.WriteLine();

        Console.WriteLine(
            $"{ConsoleTheme.WarningMark} WayFinder metadata is not configured."
        );

        Console.WriteLine(
            $"  Run {ConsoleTheme.Bold("wayfinder project init")} to set this waypoint up."
        );
    }

    private static string FormatState(
        bool value,
        string trueText,
        string falseText
    )
    {
        return value
            ? $"{ConsoleTheme.SuccessMark} {ConsoleTheme.Success(trueText)}"
            : $"{ConsoleTheme.WarningMark} {ConsoleTheme.Warning(falseText)}";
    }
}
