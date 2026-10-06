namespace WayFinder.DevTools.Application.Repositories.Searching;

public sealed record RepositorySearchResult(
    IReadOnlyCollection<RepositorySearchMatch> Matches,
    bool Truncated
);
