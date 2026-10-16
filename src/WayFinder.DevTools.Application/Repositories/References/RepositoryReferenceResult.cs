namespace WayFinder.DevTools.Application.Repositories.References;

public sealed record RepositoryReferenceResult(
    string Identifier,
    IReadOnlyList<RepositoryReference> References,
    int FilesExamined,
    bool Truncated,
    string Resolution
);
