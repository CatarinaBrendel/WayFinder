namespace WayFinder.DevTools.Application.Projects.Manifest;

public interface IProjectManifestReader
{
    ProjectManifest? Read(ProjectContext project);
}
