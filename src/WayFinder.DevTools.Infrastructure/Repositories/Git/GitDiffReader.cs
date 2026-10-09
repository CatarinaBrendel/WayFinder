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

        var path = ValidatePath(
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

    private static string? ValidatePath(
        string repositoryRoot,
        string? path
    )
    {
        if (path is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "Git diff path cannot be empty.",
                nameof(path)
            );
        }

        if (Path.IsPathRooted(path))
        {
            throw new ArgumentException(
                "Git diff path must be repository-relative.",
                nameof(path)
            );
        }

        if (path.StartsWith(':'))
        {
            throw new ArgumentException(
                "Git pathspec magic is not allowed.",
                nameof(path)
            );
        }

        if (path.Contains('*')
            || path.Contains('?')
            || path.Contains('[')
            || path.Contains(']'))
        {
            throw new ArgumentException(
                "Git diff path must be a literal path.",
                nameof(path)
            );
        }

        var root = Path.GetFullPath(repositoryRoot);

        var fullPath = Path.GetFullPath(
            path,
            root
        );

        if (!Path.IsPathFullyQualified(fullPath))
        {
            throw new ArgumentException(
                "Invalid Git diff path.",
                nameof(path)
            );
        }

        var relativePath = Path.GetRelativePath(
            root,
            fullPath
        );

        if (relativePath == ".."
            || relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal
            )
            || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException(
                "Git diff path must remain inside the repository.",
                nameof(path)
            );
        }

        return relativePath.Replace(
            Path.DirectorySeparatorChar,
            '/'
        );
    }
}
