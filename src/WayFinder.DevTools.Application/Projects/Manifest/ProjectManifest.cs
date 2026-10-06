namespace WayFinder.DevTools.Application.Projects.Manifest;

public sealed record ProjectManifest(
    int Version,
    string? Name,
    IReadOnlyCollection<string> Technologies
);
