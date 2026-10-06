namespace WayFinder.DevTools.Application.Repositories.Reading;

public sealed record RepositoryFileContent(
    string Path,
    string Content,
    long TotalBytes,
    bool Truncated
);
