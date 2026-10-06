namespace WayFinder.DevTools.Application.Projects.Initialization;

public sealed record ProjectInitialization(
    string ManifestPath,
    string ProjectName,
    IReadOnlyCollection<string> Technologies
);
