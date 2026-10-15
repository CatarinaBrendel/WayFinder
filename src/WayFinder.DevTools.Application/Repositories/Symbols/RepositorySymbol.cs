namespace WayFinder.DevTools.Application.Repositories.Symbols;

public sealed record RepositorySymbol(
    string Name,
    string QualifiedName,
    RepositorySymbolKind Kind,
    string RelativePath,
    int Line
);
