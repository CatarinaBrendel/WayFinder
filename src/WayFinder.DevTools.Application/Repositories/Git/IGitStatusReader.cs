using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Repositories.Git;

public interface IGitStatusReader
{
    GitStatus Read(ProjectContext project);
}
