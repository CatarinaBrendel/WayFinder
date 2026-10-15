using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registry;

namespace WayFinder.DevTools.Mcp.Tests.Fixtures;

internal sealed class InMemoryProjectRegistry : IProjectRegistry
{
    private readonly List<RegisteredProject> _projects = [];

    public IReadOnlyCollection<RegisteredProject> GetAll()
        => _projects.ToArray();

    public RegisteredProject? FindById(Guid id)
        => _projects.FirstOrDefault(project => project.Id == id);

    public RegisteredProject? FindByRootPath(string rootPath)
        => _projects.FirstOrDefault(project =>
            string.Equals(
                project.RootPath,
                rootPath,
                StringComparison.OrdinalIgnoreCase));

    public RegisteredProject Add(ProjectContext project)
    {
        var registered = new RegisteredProject(
            Guid.NewGuid(),
            project.Name,
            project.RootPath
        );

        _projects.Add(registered);

        return registered;
    }

    public bool Remove(Guid id)
        => _projects.RemoveAll(project => project.Id == id) > 0;
}
