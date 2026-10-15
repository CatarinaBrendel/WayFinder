using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.Navigation;

public interface IRepositorySourceNavigator
{
    RepositorySourceExcerpt ReadLines(
        ProjectContext project,
        string relativePath,
        int startLine,
        int lineCount
    );

    RepositorySourceExcerpt ReadDeclaration(
        ProjectContext project,
        string relativePath,
        string qualifiedName
    );
}
