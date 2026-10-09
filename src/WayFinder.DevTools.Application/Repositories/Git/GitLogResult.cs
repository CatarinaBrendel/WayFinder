namespace WayFinder.DevTools.Application.Repositories.Git;

public sealed record GitLogResult(
    string RepositoryName,
    string? Branch,
    IReadOnlyList<GitCommit> Commits
);
