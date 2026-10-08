namespace WayFinder.DevTools.Application.Repositories.Reading;

public sealed record RepositoryFileRangeContent(
    string Path,
    string Content,
    long TotalBytes,
    int StartLine,
    int? EndLine
);
