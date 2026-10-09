using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Repositories.Git;

public sealed class GitDiffReader : IGitDiffReader
{
    private readonly GitCommandRunner _runner = new();

    public GitDiffResult Read(
        ProjectContext project,
        GitDiffRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(request);

        if (!project.IsGitRepository)
        {
            throw new InvalidOperationException(
                "Git diff requires a Git repository."
            );
        }

        if (request.MaxOutputBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Maximum output size must be positive."
            );
        }

        var path = GitRepositoryPathValidator.Validate(
            project.RootPath,
            request.Path
        );

        var result = _runner.GetDiff(
            project.RootPath,
            request.Staged,
            request.StatOnly,
            path,
            request.MaxOutputBytes
        );

        var branch = _runner.GetBranch(
            project.RootPath
        );

        return new GitDiffResult(
            RepositoryName: project.Name,
            Branch: branch,
            Staged: request.Staged,
            StatOnly: request.StatOnly,
            Path: path,
            Content: result.Content,
            Truncated: result.Truncated
        );
    }

}
