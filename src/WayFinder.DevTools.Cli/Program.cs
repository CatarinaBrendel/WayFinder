using System.CommandLine;
using WayFinder.DevTools.Infrastructure.Projects;

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

    Console.WriteLine($"Name: {project.Name}");
    Console.WriteLine($"Root: {project.RootPath}");
    Console.WriteLine($"Git:  {(project.IsGitRepository ? "yes" : "no")}");
});

projectCommand.Subcommands.Add(projectInfoCommand);
rootCommand.Subcommands.Add(projectCommand);

return rootCommand.Parse(args).Invoke();
