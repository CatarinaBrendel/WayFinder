namespace WayFinder.DevTools.Infrastructure.Context;

internal sealed record RankedContextCandidate(
    ContextCandidate Candidate,
    int Score
);
