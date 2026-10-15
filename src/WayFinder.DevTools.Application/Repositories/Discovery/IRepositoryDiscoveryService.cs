using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.Discovery;

public interface IRepositoryDiscoveryService
{
    RepositoryDiscoveryResult Discover(ProjectContext project);
}
