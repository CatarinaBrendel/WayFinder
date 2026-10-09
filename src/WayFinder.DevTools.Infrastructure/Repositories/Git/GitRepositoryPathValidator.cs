namespace WayFinder.DevTools.Infrastructure.Repositories.Git;

public static class GitRepositoryPathValidator
{
    public static string? Validate(
        string repositoryRoot,
        string? path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        if (path is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw InvalidPath();
        }

        // Normalize separators consistently across operating systems.
        var normalizedPath = path.Replace('\\', '/');

        // Reject absolute paths on Windows, macOS, and Linux.
        if (normalizedPath.StartsWith('/'))
        {
            throw InvalidPath();
        }

        // Reject Windows drive-qualified paths, including C:relative.txt.
        if (normalizedPath.Length >= 2
            && char.IsLetter(normalizedPath[0])
            && normalizedPath[1] == ':')
        {
            throw InvalidPath();
        }

        // Reject Git pathspec magic and wildcard expressions.
        if (normalizedPath.StartsWith(':')
            || normalizedPath.IndexOfAny(['*', '?', '[', ']']) >= 0)
        {
            throw InvalidPath();
        }

        // Reject NUL characters.
        if (normalizedPath.Contains('\0'))
        {
            throw InvalidPath();
        }

        // Reject empty, current-directory, and parent-directory segments.
        var segments = normalizedPath.Split('/');

        if (segments.Any(segment =>
                segment is "" or "." or ".."))
        {
            throw InvalidPath();
        }

        // Verify that the resolved path remains within the repository.
        var root = Path.GetFullPath(repositoryRoot);

        var fullPath = Path.GetFullPath(
            normalizedPath.Replace('/', Path.DirectorySeparatorChar),
            root
        );

        var relativePath = Path.GetRelativePath(root, fullPath);

        if (relativePath == ".."
            || relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath))
        {
            throw InvalidPath();
        }

        return normalizedPath;
    }

    private static ArgumentException InvalidPath()
    {
        return new ArgumentException(
            "Git path must be a repository-relative literal path.",
            "path"
        );
    }
}
