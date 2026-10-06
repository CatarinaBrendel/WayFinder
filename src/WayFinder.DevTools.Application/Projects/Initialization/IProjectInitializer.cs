namespace WayFinder.DevTools.Application.Projects.Initialization;

public interface IProjectInitializer
{
    ProjectInitialization Prepare(ProjectContext project);

    void Initialize(
        ProjectContext project,
        ProjectInitialization initialization
    );
}
