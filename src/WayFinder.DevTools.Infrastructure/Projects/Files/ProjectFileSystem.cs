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
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);

        var files = new List<ProjectFile>();

        EnumerateDirectory(
            project,
            new DirectoryInfo(project.RootPath),
            searchPattern,
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
        string searchPattern,
        ICollection<ProjectFile> files
    )
    {
        foreach (var file in directory.EnumerateFiles(searchPattern))
        {
            var path = ResolveExistingPath(project, file.FullName);

            files.Add(
                new ProjectFile(
                    Path.GetRelativePath(project.RootPath, path)
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

            EnumerateDirectory(project, child, searchPattern, files);
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
}
