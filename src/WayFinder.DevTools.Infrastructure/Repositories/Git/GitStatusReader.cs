using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Repositories.Git;

public sealed class GitStatusReader : IGitStatusReader
{
    private readonly GitCommandRunner _runner = new();
    private readonly GitStatusParser _parser = new();

    public GitStatus Read(ProjectContext project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (!project.IsGitRepository)
        {
            throw new InvalidOperationException(
                "Git status requires a Git repository."
            );
        }

        var output = _runner.GetStatus(project.RootPath);
        var branch = _runner.GetBranch(project.RootPath);

        return _parser.Parse(
            project.Name,
            branch,
            output
        );
    }
}
