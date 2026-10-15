namespace WayFinder.DevTools.Application.Repositories.Discovery;

public sealed record RepositoryDiscoveryResult(
    IReadOnlyList<RepositorySolution> Solutions,
    IReadOnlyList<RepositoryProject> Projects
);
