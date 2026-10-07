namespace WayFinder.DevTools.Application.Context;

public sealed record ContextRequest(
    string Task,
    int TokenBudget = 8_000
);
