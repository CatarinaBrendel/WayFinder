namespace WayFinder.DevTools.Mcp.Contracts.Repositories.Reading;

public sealed record RepositoryFileResponse(
    string Path,
    string Content,
    long TotalBytes,
    bool Truncated
);
