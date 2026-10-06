using System.CommandLine;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Manifest;
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

        var manifest =
            services.ProjectManifestReader.Read(project);

        var inspection =
            services.ProjectInspector.Inspect(project);

        Console.WriteLine(project.Name);
        Console.WriteLine(
            new string('─', project.Name.Length)
        );

        Console.WriteLine();

        Console.WriteLine("Project");
        Console.WriteLine(
            $"  Root       {project.RootPath}"
        );

        Console.WriteLine(
            $"  Git        {(project.IsGitRepository ? "yes" : "no")}"
        );

        Console.WriteLine(
            $"  Manifest   {(manifest is null ? "not configured" : "configured")}"
        );

        if (manifest is not null)
        {
            WriteManifest(manifest);
        }

        WriteInspection(inspection);

        if (manifest is null)
        {
            Console.WriteLine();
            Console.WriteLine(
                "WayFinder project metadata is not configured."
            );

            Console.WriteLine();
            Console.WriteLine(
                "Run `wayfinder project init` to create it."
            );
        }
    }

    private static void WriteManifest(
        ProjectManifest manifest
    )
    {
        Console.WriteLine();
        Console.WriteLine("Declared");

        if (manifest.Technologies.Count == 0)
        {
            Console.WriteLine("  none");
            return;
        }

        foreach (var technology in manifest.Technologies)
        {
            Console.WriteLine($"  {technology}");
        }
    }

    private static void WriteInspection(
        ProjectInspection inspection
    )
    {
        var detected = inspection.Artifacts
            .Where(entry => entry.Value.Count > 0)
            .ToArray();

        if (detected.Length == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Detected");

        foreach (var (detector, artifacts) in detected)
        {
            Console.WriteLine($"  {detector}");

            foreach (var artifact in artifacts)
            {
                Console.WriteLine(
                    $"    {artifact.Type,-12} {artifact.Path}"
                );
            }
        }
    }
}
