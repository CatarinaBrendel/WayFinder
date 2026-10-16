using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.TypeRelationships;

public interface IRepositoryTypeRelationshipReader
{
    RepositoryTypeRelationshipResult Read(
        ProjectContext project,
        string relativePath
    );
}
