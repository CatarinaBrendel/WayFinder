namespace WayFinder.DevTools.Mcp.Contracts.Context;

public sealed record ContextFileResponse(
    string Path,
    string Kind,
    string Content,
    int EstimatedTokens,
    int? StartLine = null,
    int? EndLine = null
);
