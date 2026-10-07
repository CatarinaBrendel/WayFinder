namespace WayFinder.DevTools.Application.Context;

public sealed record ContextStatistics(
    int FileCount,
    int EstimatedTokens,
    int TokenBudget
);
