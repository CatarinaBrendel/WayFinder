namespace WayFinder.DevTools.Application.Repositories.Git;

public sealed record GitLogRequest(
    int Count = 10,
    string? Path = null
);
