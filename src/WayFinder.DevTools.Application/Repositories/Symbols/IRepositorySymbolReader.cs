using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.Symbols;

public interface IRepositorySymbolReader
{
    RepositorySymbolResult Read(
        ProjectContext project,
        string relativePath
    );
}
