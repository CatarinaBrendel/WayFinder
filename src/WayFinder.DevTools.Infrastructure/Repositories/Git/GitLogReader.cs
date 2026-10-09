using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Repositories.Git;

public sealed class GitLogReader : IGitLogReader
{
    private readonly GitCommandRunner _runner = new();
    private readonly GitLogParser _parser = new();

    public GitLogResult Read(
        ProjectContext project,
        GitLogRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(request);

        if (!project.IsGitRepository)
        {
            throw new InvalidOperationException(
                "Git log requires a Git repository."
            );
        }

        if (request.Count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Commit count must be between 1 and 100."
            );
        }

        var path = GitRepositoryPathValidator.Validate(
            project.RootPath,
            request.Path
        );

        var output = _runner.GetLog(
            project.RootPath,
            request.Count,
            path
        );

        var commits = _parser.Parse(output);

        var branch = _runner.GetBranch(
            project.RootPath
        );

        return new GitLogResult(
            RepositoryName: project.Name,
            Branch: branch,
            Commits: commits
        );
    }
}
