namespace WayFinder.DevTools.Application.Projects.Files;

public interface IProjectFileSystem
{
    bool FileExists(ProjectContext project, string relativePath);

    IReadOnlyCollection<ProjectFile> FindFiles(
        ProjectContext project,
        string searchPattern
    );
}
