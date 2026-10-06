using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Application.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Projects.Detection;

public sealed class DotNetProjectDetector(
    IProjectFileSystem fileSystem
) : IProjectDetector
{
    public string Name => "dotnet";

    public IReadOnlyCollection<ProjectArtifact> Detect(ProjectContext project)
    {
        var artifacts = new List<ProjectArtifact>();

        AddFiles(
            artifacts,
            project,
            "*.sln",
            "solution"
        );

        AddFiles(
            artifacts,
            project,
            "*.slnx",
            "solution"
        );

        AddFiles(
            artifacts,
            project,
            "*.csproj",
            "project"
        );

        if (fileSystem.FileExists(project, "global.json"))
        {
            artifacts.Add(
                new ProjectArtifact(
                    Type: "globaljson",
                    Path: "global.json"
                )
            );
        }

        return artifacts;
    }

    private void AddFiles(
        ICollection<ProjectArtifact> artifacts,
        ProjectContext project,
        string searchPattern,
        string type
    )
    {
        foreach (var file in fileSystem.FindFiles(
                     project,
                     searchPattern))
        {
            artifacts.Add(
                new ProjectArtifact(
                    Type: type,
                    Path: file.RelativePath
                )
            );
        }
    }
}
