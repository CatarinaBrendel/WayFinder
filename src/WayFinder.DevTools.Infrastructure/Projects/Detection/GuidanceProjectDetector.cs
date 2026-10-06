using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Application.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Projects.Detection;

public sealed class GuidanceProjectDetector(
    IProjectFileSystem fileSystem
) : IProjectDetector
{
    public string Name => "guidance";

    public IReadOnlyCollection<ProjectArtifact> Detect(ProjectContext project)
    {
        var artifacts = new List<ProjectArtifact>();

        AddIfExists(
            artifacts,
            project,
            "AGENTS.md",
            "agents"
        );

        AddIfExists(
            artifacts,
            project,
            ".editorconfig",
            "editorconfig"
        );

        return artifacts;
    }

    private void AddIfExists(
        ICollection<ProjectArtifact> artifacts,
        ProjectContext project,
        string path,
        string type
    )
    {
        if (fileSystem.FileExists(project, path))
        {
            artifacts.Add(
                new ProjectArtifact(
                    Type: type,
                    Path: path
                )
            );
        }
    }
}
