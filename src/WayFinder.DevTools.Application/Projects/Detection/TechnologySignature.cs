namespace WayFinder.DevTools.Application.Projects.Detection;

public sealed record TechnologySignature(
    string Id,
    string DisplayName,
    IReadOnlyCollection<ArtifactSignature> Artifacts
);

public sealed record ArtifactSignature(
    string Pattern,
    string Type
);
