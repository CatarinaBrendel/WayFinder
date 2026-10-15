namespace WayFinder.DevTools.Application.Repositories.Relationships;

public sealed record RepositoryProjectRelationship(
    string ProjectPath,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<RepositoryProjectReference> References,
    IReadOnlyList<RepositoryAssemblyReference> AssemblyReferences
);
