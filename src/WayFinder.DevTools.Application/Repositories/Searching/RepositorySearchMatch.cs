namespace WayFinder.DevTools.Application.Repositories.Searching;

public sealed record RepositorySearchMatch(
    string Path,
    int LineNumber,
    string Line
);
