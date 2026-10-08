using System.IO.Enumeration;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Projects.Files;

public sealed class ProjectFileSystem : IProjectFileSystem
{
    private static readonly HashSet<string> IgnoredDirectories =
    [
        ".git",
        ".idea",
        ".vs",
        ".vscode",
        "bin",
        "obj",
        "node_modules",
        "dist",
        "coverage",
    ];

    public bool FileExists(ProjectContext project, string relativePath)
    {
        var path = ResolvePath(project, relativePath);

        if (!File.Exists(path))
        {
            return false;
        }

        ResolveExistingPath(project, path);

        return true;
    }

    public string ReadAllText(
        ProjectContext project,
        string relativePath
    )
    {
        var path = ResolvePath(project, relativePath);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"The file '{relativePath}' does not exist.",
                path
            );
        }

        ResolveExistingPath(project, path);

        return File.ReadAllText(path);
    }

    public IReadOnlyCollection<ProjectFile> FindFiles(
        ProjectContext project,
        string searchPattern
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            searchPattern
        );

        return EnumerateFiles(project)
            .Where(
                file => FileSystemName.MatchesSimpleExpression(
                    searchPattern,
                    Path.GetFileName(file.RelativePath),
                    ignoreCase: false
                )
            )
            .ToArray();
    }

    private static IReadOnlyCollection<ProjectFile> EnumerateFiles(
        ProjectContext project
    )
    {
        var files =
            new List<ProjectFile>();

        EnumerateDirectory(
            project,
            new DirectoryInfo(project.RootPath),
            files
        );

        return files
            .OrderBy(
                file => file.RelativePath,
                StringComparer.Ordinal
            )
            .ToArray();
    }

    private static void EnumerateDirectory(
        ProjectContext project,
        DirectoryInfo directory,
        ICollection<ProjectFile> files
    )
    {
        foreach (var file in directory.EnumerateFiles())
        {
            var path =
                ResolveExistingPath(
                    project,
                    file.FullName
                );

            files.Add(
                new ProjectFile(
                    Path.GetRelativePath(
                        project.RootPath,
                        path
                    )
                )
            );
        }

        foreach (var child in directory.EnumerateDirectories())
        {
            if (IgnoredDirectories.Contains(child.Name))
            {
                continue;
            }

            if (child.LinkTarget is not null)
            {
                // V1: don't traverse directory symlinks at all.
                continue;
            }

            EnumerateDirectory(
                project,
                child,
                files
            );
        }
    }

    private static string ResolvePath(
        ProjectContext project,
        string relativePath
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        if (Path.IsPathRooted(relativePath))
        {
            throw new UnauthorizedAccessException(
                "Absolute paths are not allowed."
            );
        }

        var projectRoot = Path.GetFullPath(project.RootPath);
        var candidate = Path.GetFullPath(
            Path.Combine(projectRoot, relativePath)
        );

        EnsureContained(projectRoot, candidate);

        return candidate;
    }

    private static string ResolveExistingPath(
        ProjectContext project,
        string path
    )
    {
        var projectRoot = Path.GetFullPath(project.RootPath);
        var candidate = Path.GetFullPath(path);

        EnsureContained(projectRoot, candidate);

        var info = new FileInfo(candidate);

        if (info.LinkTarget is not null)
        {
            var target = info.ResolveLinkTarget(returnFinalTarget: true)
                ?? throw new IOException(
                    $"Could not resolve symbolic link '{candidate}'."
                );

            EnsureContained(
                projectRoot,
                Path.GetFullPath(target.FullName)
            );
        }

        return candidate;
    }

    private static void EnsureContained(
        string projectRoot,
        string candidate
    )
    {
        var relative = Path.GetRelativePath(projectRoot, candidate);

        if (relative == ".."
            || relative.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                "The requested path is outside the project root."
            );
        }
    }

    public void WriteAllText(
        ProjectContext project,
        string relativePath,
        string content
    )
    {
        var path = ResolvePath(project, relativePath);

        if (File.Exists(path))
        {
            ResolveExistingPath(project, path);
        }

        File.WriteAllText(path, content);
    }

    public ProjectFileRead Read(
        ProjectContext project,
        string relativePath,
        int maxBytes
    )
    {
        return Read(
            project,
            relativePath,
            offset: 0,
            maxBytes
        );
    }

    public ProjectFileRead Read(
        ProjectContext project,
        string relativePath,
        long offset,
        int maxBytes
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            maxBytes
        );

        ArgumentOutOfRangeException.ThrowIfNegative(
            offset
        );

        var path = ResolvePath(
            project,
            relativePath
        );

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"The file '{relativePath}' does not exist.",
                path
            );
        }

        ResolveExistingPath(
            project,
            path
        );

        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );

        var totalBytes = stream.Length;

        if (offset >= totalBytes)
        {
            return new ProjectFileRead(
                Content: [],
                TotalBytes: totalBytes,
                Offset: offset,
                Truncated: false
            );
        }

        stream.Seek(offset, SeekOrigin.Begin);

        var remainingBytes = totalBytes - offset;

        var bytesToRead =
            (int)Math.Min(
                remainingBytes,
                maxBytes
            );

        var content =
            new byte[bytesToRead];

        stream.ReadExactly(content);

        return new ProjectFileRead(
            Content: content,
            TotalBytes: totalBytes,
            Offset: offset,
            Truncated: remainingBytes > maxBytes
        );
    }

    public IReadOnlyCollection<ProjectFile> GetFiles(
    ProjectContext project
)
    {
        return EnumerateFiles(project);
    }


    public IReadOnlyCollection<ProjectDirectoryEntry> GetEntries(
        ProjectContext project,
        string relativePath
    )
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        var path = relativePath.Length == 0
            ? Path.GetFullPath(project.RootPath)
            : ResolvePath(project, relativePath);

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                $"The directory '{relativePath}' does not exist."
            );
        }

        var directory = new DirectoryInfo(path);

        if (directory.LinkTarget is not null)
        {
            throw new UnauthorizedAccessException(
                "Directory symbolic links cannot be traversed."
            );
        }

        var entries = new List<ProjectDirectoryEntry>();

        foreach (var child in directory.EnumerateDirectories())
        {
            if (IgnoredDirectories.Contains(child.Name))
            {
                continue;
            }

            if (child.LinkTarget is not null)
            {
                // V1: don't expose directory symlinks in the project tree.
                continue;
            }

            var childPath = ResolveExistingPath(
                project,
                child.FullName
            );

            entries.Add(
                new ProjectDirectoryEntry(
                    child.Name,
                    Path.GetRelativePath(
                        project.RootPath,
                        childPath
                    ),
                    IsDirectory: true
                )
            );
        }

        foreach (var file in directory.EnumerateFiles())
        {
            var filePath = ResolveExistingPath(
                project,
                file.FullName
            );

            entries.Add(
                new ProjectDirectoryEntry(
                    file.Name,
                    Path.GetRelativePath(
                        project.RootPath,
                        filePath
                    ),
                    IsDirectory: false
                )
            );
        }

        return entries
            .OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)
            .ToArray();
    }

}
