namespace WayFinder.DevTools.Infrastructure.Repositories.Git;

internal sealed record GitDiffCommandResult(
    string Content,
    bool Truncated
);
