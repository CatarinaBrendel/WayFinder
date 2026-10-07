namespace WayFinder.DevTools.Application.Context;

public sealed record ContextSelection(
    string Path,
    int Score,
    IReadOnlyCollection<string> PathMatchedTerms,
    IReadOnlyCollection<string> ContentMatchedTerms,
    bool Included
);
