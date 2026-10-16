using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.References;

public interface IRepositoryReferenceReader
{
    RepositoryReferenceResult Read(
        ProjectContext project,
        string identifier
    );
}
