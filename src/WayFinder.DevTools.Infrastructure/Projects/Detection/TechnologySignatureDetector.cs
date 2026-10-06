using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Application.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Projects.Detection;

public sealed class TechnologySignatureDetector(
    IProjectFileSystem fileSystem,
    TechnologySignature signature
) : IProjectDetector
{
    public string Name => signature.Id;

    public IReadOnlyCollection<ProjectArtifact> Detect(ProjectContext project)
    {
        var artifacts = new List<ProjectArtifact>();

        foreach (var artifactSignature in signature.Artifacts)
        {
            foreach (var file in fileSystem.FindFiles(
                         project,
                         artifactSignature.Pattern))
            {
                artifacts.Add(
                    new ProjectArtifact(
                        Type: artifactSignature.Type,
                        Path: file.RelativePath
                    )
                );
            }
        }

        return artifacts
            .OrderBy(
                artifact => artifact.Path,
                StringComparer.Ordinal
            )
            .ThenBy(
                artifact => artifact.Type,
                StringComparer.Ordinal
            )
            .ToArray();
    }
}
