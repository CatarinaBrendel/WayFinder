namespace WayFinder.DevTools.Mcp.Contracts.Repositories;

public sealed record RepositoryProjectReferenceResponse(
    string Path,
    bool IsExternal
);

public sealed record RepositoryAssemblyReferenceResponse(
    string Name,
    string? HintPath,
    bool? CopyLocal
);

public sealed record RepositoryProjectRelationshipResponse(
    string ProjectPath,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<RepositoryProjectReferenceResponse> References,
    IReadOnlyList<RepositoryAssemblyReferenceResponse> AssemblyReferences
);

public sealed record RepositoryRelationshipsResponse(
    IReadOnlyList<RepositoryProjectRelationshipResponse> Projects
);
