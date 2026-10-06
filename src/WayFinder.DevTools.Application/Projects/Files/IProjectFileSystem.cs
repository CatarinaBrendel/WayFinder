namespace WayFinder.DevTools.Application.Projects.Files;

public interface IProjectFileSystem
{
    bool FileExists(ProjectContext project, string relativePath);

    IReadOnlyCollection<ProjectFile> FindFiles(
        ProjectContext project,
        string searchPattern
    );

    string ReadAllText(
        ProjectContext project,
        string relativePath
    );

    void WriteAllText(
        ProjectContext project,
        string relativePath,
        string content
    );

    ProjectFileRead Read(
        ProjectContext project,
        string relativePath,
        int maxBytes
    );

    IReadOnlyCollection<ProjectFile> GetFiles(
        ProjectContext project
    );
}
