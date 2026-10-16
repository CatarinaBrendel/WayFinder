namespace WayFinder.DevTools.Application.Repositories.TypeRelationships;

public sealed record RepositoryTypeRelationship(
    string Name,
    string QualifiedName,
    string Kind,
    string RelativePath,
    int Line,
    IReadOnlyList<string> DeclaredBaseTypes
);
