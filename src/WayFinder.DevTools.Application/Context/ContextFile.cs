namespace WayFinder.DevTools.Application.Context;

public sealed record ContextFile(
    string Path,
    string Content,
    int EstimatedTokens,
    ContextFileKind Kind = ContextFileKind.Complete,
    int? StartLine = null,
    int? EndLine = null
);
