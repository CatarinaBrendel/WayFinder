namespace WayFinder.DevTools.Mcp.Contracts.Repositories.Searching;

public sealed record RepositorySearchMatchResponse(
    string Path,
    int LineNumber,
    string Line
);
