namespace WayFinder.DevTools.Mcp.Contracts.Repositories.Searching;

public sealed record RepositorySearchResponse(
    IReadOnlyCollection<RepositorySearchMatchResponse> Matches,
    bool Truncated
);
