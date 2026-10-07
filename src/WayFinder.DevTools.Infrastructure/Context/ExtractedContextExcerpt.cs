namespace WayFinder.DevTools.Infrastructure.Context;

internal sealed record ExtractedContextExcerpt(
    int StartLine,
    int EndLine,
    string Content
);
