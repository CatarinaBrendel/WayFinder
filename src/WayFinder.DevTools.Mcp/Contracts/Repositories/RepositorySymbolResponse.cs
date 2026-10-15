namespace WayFinder.DevTools.Mcp.Contracts.Repositories;

public sealed record RepositorySymbolResponse(
    string Name,
    string QualifiedName,
    string Kind,
    string RelativePath,
    int Line
);

public sealed record RepositorySymbolsResponse(
    IReadOnlyList<RepositorySymbolResponse> Symbols,
    bool Truncated
);
