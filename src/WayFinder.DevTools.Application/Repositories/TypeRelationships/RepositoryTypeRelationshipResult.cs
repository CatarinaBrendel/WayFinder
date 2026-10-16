namespace WayFinder.DevTools.Application.Repositories.TypeRelationships;

public sealed record RepositoryTypeRelationshipResult(
    IReadOnlyList<RepositoryTypeRelationship> Relationships,
    bool Truncated
);
