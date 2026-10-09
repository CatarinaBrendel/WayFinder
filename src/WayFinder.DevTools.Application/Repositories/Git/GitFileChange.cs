namespace WayFinder.DevTools.Application.Repositories.Git;

public sealed record GitFileChange(
    string Path,
    GitChangeKind Kind,
    string? OriginalPath = null
);
