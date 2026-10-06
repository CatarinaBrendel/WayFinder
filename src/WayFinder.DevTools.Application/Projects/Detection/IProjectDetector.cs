namespace WayFinder.DevTools.Application.Projects.Detection;

public interface IProjectDetector
{
    string Name { get; }

    IReadOnlyCollection<ProjectArtifact> Detect(ProjectContext project);
}
