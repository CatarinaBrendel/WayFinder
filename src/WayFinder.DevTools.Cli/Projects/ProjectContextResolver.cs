using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registry;

namespace WayFinder.DevTools.Cli.Projects;

public sealed class ProjectContextResolver(
    IProjectRegistry registry,
    IProjectLocator locator)
{
    public ProjectContext? Resolve(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return locator.Locate(Environment.CurrentDirectory);
        }

        var matches = registry.GetAll()
            .Where(project =>
                string.Equals(
                    project.Name,
                    projectName,
                    StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();

        if (matches.Length == 0)
        {
            throw new KeyNotFoundException(
                $"Registered project not found: {projectName}");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple registered projects named '{projectName}'. " +
                "Please use a unique project name.");
        }

        var registered = matches[0];

        if (!Directory.Exists(registered.RootPath))
        {
            throw new DirectoryNotFoundException(
                $"Project directory is unavailable: {registered.RootPath}");
        }

        return locator.Locate(registered.RootPath)
            ?? throw new InvalidOperationException(
                $"Unable to locate project at: {registered.RootPath}");
    }
}
