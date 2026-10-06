using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Application.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Projects.Detection;

public sealed class NodeProjectDetector(
    IProjectFileSystem fileSystem
) : IProjectDetector
{
    private static readonly (string Pattern, string Type)[] Artifacts =
    [
        ("package.json", "manifest"),
        ("package-lock.json", "npm-lock"),
        ("npm-shrinkwrap.json", "npm-lock"),
        ("yarn.lock", "yarn-lock"),
        ("pnpm-lock.yaml", "pnpm-lock"),
        ("bun.lock", "bun-lock"),
        ("bun.lockb", "bun-lock"),
    ];

    public string Name => "node";

    public IReadOnlyCollection<ProjectArtifact> Detect(ProjectContext project)
    {
        var artifacts = new List<ProjectArtifact>();

        foreach (var (pattern, type) in Artifacts)
        {
            foreach (var file in fileSystem.FindFiles(project, pattern))
            {
                artifacts.Add(
                    new ProjectArtifact(
                        Type: type,
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
