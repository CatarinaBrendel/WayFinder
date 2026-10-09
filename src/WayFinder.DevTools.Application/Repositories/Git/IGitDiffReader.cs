using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.Git;

public interface IGitDiffReader
{
    GitDiffResult Read(
        ProjectContext project,
        GitDiffRequest request
    );
}
