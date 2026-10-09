namespace WayFinder.DevTools.Application.Repositories.Git;

public sealed record GitDiffRequest(
    bool Staged = false,
    bool StatOnly = false,
    string? Path = null,
    int MaxOutputBytes = 65_536
);
