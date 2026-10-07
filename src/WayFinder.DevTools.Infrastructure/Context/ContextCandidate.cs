using WayFinder.DevTools.Infrastructure.Context;

internal sealed record ContextCandidate(
    string Path,
    IReadOnlyCollection<string> PathMatchedTerms,
    IReadOnlyCollection<string> ContentMatchedTerms,
    IReadOnlyCollection<ContextContentMatch> ContentMatches
);
