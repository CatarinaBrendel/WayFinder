using WayFinder.DevTools.Application.Projects.Detection;

namespace WayFinder.DevTools.Application.Projects.Inspection;

public sealed record ProjectInspection(
    ProjectContext Project,
    IReadOnlyDictionary<string, IReadOnlyCollection<ProjectArtifact>> Artifacts
);
