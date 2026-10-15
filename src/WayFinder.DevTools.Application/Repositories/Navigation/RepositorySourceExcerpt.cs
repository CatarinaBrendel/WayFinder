namespace WayFinder.DevTools.Application.Repositories.Navigation;

public sealed record RepositorySourceExcerpt(
    string RelativePath,
    int StartLine,
    int EndLine,
    string Content,
    bool Truncated
);
