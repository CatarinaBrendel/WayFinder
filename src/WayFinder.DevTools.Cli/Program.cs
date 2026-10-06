using System.CommandLine;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Manifest;
using WayFinder.DevTools.Infrastructure.Composition;

var services = WayFinderComposition.Create();

var rootCommand = new RootCommand("WayFinder personal developer tools");

var projectCommand = new Command("project", "Inspect and manage projects");

var projectInfoCommand = new Command(
    "info",
    "Show information about the current project"
);

var projectInitCommand = new Command(
    "init",
    "Create WayFinder metadata for the current project"
);
projectInfoCommand.SetAction(_ =>
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
    Console.WriteLine(new string('─', project.Name.Length));
    Console.WriteLine();

    Console.WriteLine("Project");
    Console.WriteLine($"  Root       {project.RootPath}");
    Console.WriteLine($"  Git        {(project.IsGitRepository ? "yes" : "no")}");
    Console.WriteLine($"  Manifest   {(manifest is null ? "not configured" : "configured")}");

    if (manifest is not null)
    {
        WriteManifest(manifest);
    }

    WriteInspection(inspection);

    if (manifest is null)
    {
        Console.WriteLine();
        Console.WriteLine("WayFinder project metadata is not configured.");
        Console.WriteLine();
        Console.WriteLine("Run `wayfinder project init` to create it.");
    }
});

projectInitCommand.SetAction(_ =>
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
});

projectCommand.Subcommands.Add(projectInfoCommand);
projectCommand.Subcommands.Add(projectInitCommand);

rootCommand.Subcommands.Add(projectCommand);

return rootCommand.Parse(args).Invoke();

static void WriteInspection(ProjectInspection inspection)
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

static void WriteManifest(ProjectManifest manifest)
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
