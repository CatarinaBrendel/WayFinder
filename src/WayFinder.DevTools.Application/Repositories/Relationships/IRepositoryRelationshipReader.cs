using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.Relationships;

public interface IRepositoryRelationshipReader
{
    IReadOnlyList<RepositoryProjectRelationship> Read(
        ProjectContext project
    );
}
