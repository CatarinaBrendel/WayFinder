namespace WayFinder.DevTools.Application.Repositories.References;

public sealed record RepositoryReference(
    string RelativePath,
    int Line,
    int Column,
    string Kind
);
