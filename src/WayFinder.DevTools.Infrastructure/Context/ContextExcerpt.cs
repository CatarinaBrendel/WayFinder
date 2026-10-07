namespace WayFinder.DevTools.Infrastructure.Context;

internal sealed record ContextExcerpt(
    int StartLine,
    int EndLine,
    IReadOnlyCollection<string> MatchedTerms
);
