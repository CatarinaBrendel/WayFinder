namespace WayFinder.DevTools.Application.Repositories.Git;

public sealed record GitStatus(
    string RepositoryName,
    string? Branch,
    IReadOnlyList<GitFileChange> Staged,
    IReadOnlyList<GitFileChange> Unstaged,
    IReadOnlyList<string> Untracked,
    IReadOnlyList<GitFileChange> Conflicts
);
