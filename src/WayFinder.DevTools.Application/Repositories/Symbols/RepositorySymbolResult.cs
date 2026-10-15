namespace WayFinder.DevTools.Application.Repositories.Symbols;

public sealed record RepositorySymbolResult(
    IReadOnlyList<RepositorySymbol> Symbols,
    bool Truncated
);
