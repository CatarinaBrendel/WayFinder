using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.Searching;

public interface IRepositorySearcher
{
    RepositorySearchResult Search(
        ProjectContext project,
        string query
    );
}
