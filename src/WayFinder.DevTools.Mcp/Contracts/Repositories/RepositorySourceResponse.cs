namespace WayFinder.DevTools.Mcp.Contracts.Repositories;

public sealed record RepositorySourceResponse(
    string RelativePath,
    int StartLine,
    int EndLine,
    string Content,
    bool Truncated
);
