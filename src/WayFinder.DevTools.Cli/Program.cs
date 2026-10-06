using System.CommandLine;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Infrastructure.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Detection;
using WayFinder.DevTools.Infrastructure.Projects.Files;

var rootCommand = new RootCommand("WayFinder personal developer tools");

var projectCommand = new Command("project", "Inspect and manage projects");
var projectInfoCommand = new Command("info", "Show information about the current project");

projectInfoCommand.SetAction(_ =>
{
    var locator = new FileSystemProjectLocator();
    var project = locator.Locate(Environment.CurrentDirectory);

    if (project is null)
    {
        Console.Error.WriteLine("No project found.");
        return;
    }

    var fileSystem = new ProjectFileSystem();

    var inspector = new ProjectInspector(
    [
        new DotNetProjectDetector(fileSystem),
        new NodeProjectDetector(fileSystem),
        new GuidanceProjectDetector(fileSystem),
    ]);

    var inspection = inspector.Inspect(project);

    Console.WriteLine(project.Name);
    Console.WriteLine(new string('─', project.Name.Length));
    Console.WriteLine();

    Console.WriteLine("Project");
    Console.WriteLine($"  Root       {project.RootPath}");
    Console.WriteLine($"  Git        {(project.IsGitRepository ? "yes" : "no")}");

    WriteInspection(inspection);
});

projectCommand.Subcommands.Add(projectInfoCommand);
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
