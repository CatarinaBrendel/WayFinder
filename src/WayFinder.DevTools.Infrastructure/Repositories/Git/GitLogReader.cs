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

        var path = ValidatePath(
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
                "Git log path cannot be empty.",
                nameof(path)
            );
        }

        if (Path.IsPathRooted(path))
        {
            throw new ArgumentException(
                "Git log path must be repository-relative.",
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
                "Git log path must be a literal path.",
                nameof(path)
            );
        }

        // Reject Windows drive-qualified paths on all platforms.
        if (path.Length >= 2
            && char.IsLetter(path[0])
            && path[1] == ':')
        {
            throw new ArgumentException(
                "Git log path must be repository-relative.",
                nameof(path)
            );
        }

        // Normalize Windows separators on macOS/Linux as well.
        var normalizedPath = path.Replace('\\', '/');

        var segments = normalizedPath.Split('/');

        if (segments.Any(segment =>
                segment is "" or "." or ".."))
        {
            throw new ArgumentException(
                "Git log path contains invalid segments.",
                nameof(path)
            );
        }

        var root = Path.GetFullPath(repositoryRoot);

        var fullPath = Path.GetFullPath(
            normalizedPath,
            root
        );

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
                "Git log path must remain inside the repository.",
                nameof(path)
            );
        }

        return relativePath.Replace(
            Path.DirectorySeparatorChar,
            '/'
        );
    }
}
