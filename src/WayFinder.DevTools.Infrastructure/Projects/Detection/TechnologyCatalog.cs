namespace WayFinder.DevTools.Infrastructure.Projects.Detection;

internal sealed record TechnologyCatalog(
    int Version,
    IReadOnlyCollection<TechnologyCatalogEntry> Technologies
);

internal sealed record TechnologyCatalogEntry(
    string Id,
    string DisplayName,
    IReadOnlyCollection<TechnologyCatalogArtifact> Artifacts
);

internal sealed record TechnologyCatalogArtifact(
    string Pattern,
    string Type
);
