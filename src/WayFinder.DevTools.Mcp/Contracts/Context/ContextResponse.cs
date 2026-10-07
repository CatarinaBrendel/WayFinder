namespace WayFinder.DevTools.Mcp.Contracts.Context;

public sealed record ContextResponse(
    string Project,
    string Task,
    ContextFileResponse? Guidance,
    IReadOnlyCollection<ContextFileResponse> Files,
    ContextStatisticsResponse Statistics
);
