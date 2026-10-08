namespace WayFinder.DevTools.Mcp.Contracts.Repositories.Reading;

public sealed record RepositoryFileResponse(
    string Path,
    string Content,
    long TotalBytes,
    string Mode,
    bool? Truncated,
    int? StartLine,
    int? EndLine
);
