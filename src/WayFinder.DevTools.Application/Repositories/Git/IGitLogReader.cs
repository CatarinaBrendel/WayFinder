using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.Git;

public interface IGitLogReader
{
    GitLogResult Read(
        ProjectContext project,
        GitLogRequest request
    );
}
