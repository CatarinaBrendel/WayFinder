namespace WayFinder.DevTools.Application.Repositories.Relationships;

public sealed record RepositoryProjectReference(
    string Path,
    bool IsExternal
);
