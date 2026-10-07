namespace WayFinder.DevTools.Mcp.Contracts.Context;

public sealed record ContextStatisticsResponse(
    int FileCount,
    int EstimatedTokens,
    int TokenBudget
);
