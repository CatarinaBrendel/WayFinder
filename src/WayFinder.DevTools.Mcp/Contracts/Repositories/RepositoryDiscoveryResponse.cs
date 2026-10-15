namespace WayFinder.DevTools.Mcp.Contracts.Repositories;

public sealed record RepositoryDiscoveryResponse(
    IReadOnlyList<RepositorySolutionResponse> Solutions,
    IReadOnlyList<RepositoryProjectResponse> Projects
);
