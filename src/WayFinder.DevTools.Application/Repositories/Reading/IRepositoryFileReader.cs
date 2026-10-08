using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.Reading;

public interface IRepositoryFileReader
{
    RepositoryFileContent Read(
        ProjectContext project,
        string relativePath
    );

    RepositoryFileRangeContent Read(
        ProjectContext project,
        string relativePath,
        int startLine,
        int lineCount
    );
}
