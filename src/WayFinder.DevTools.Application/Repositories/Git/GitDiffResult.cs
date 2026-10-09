namespace WayFinder.DevTools.Application.Repositories.Git;

public sealed record GitDiffResult(
    string RepositoryName,
    string? Branch,
    bool Staged,
    bool StatOnly,
    string? Path,
    string Content,
    bool Truncated
);
