namespace WayFinder.DevTools.Application.Repositories.Relationships;

public sealed record RepositoryAssemblyReference(
    string Name,
    string? HintPath,
    bool? CopyLocal
);
