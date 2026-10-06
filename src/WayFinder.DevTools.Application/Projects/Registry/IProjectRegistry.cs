namespace WayFinder.DevTools.Application.Projects.Registry;

public interface IProjectRegistry
{
    IReadOnlyCollection<RegisteredProject> GetAll();

    RegisteredProject? FindById(Guid id);

    RegisteredProject? FindByRootPath(string rootPath);

    RegisteredProject Add(ProjectContext project);

    bool Remove(Guid id);
}
