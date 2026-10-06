using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Infrastructure.Projects;

public sealed class FileSystemProjectLocator : IProjectLocator
{
    public ProjectContext? Locate(string startPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startPath);

        var directory = ResolveDirectory(startPath);

        while (directory is not null)
        {
            if (IsGitRepository(directory))
            {
                return new ProjectContext(
                    Name: directory.Name,
                    RootPath: directory.FullName,
                    IsGitRepository: true
                );
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static DirectoryInfo ResolveDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            return new DirectoryInfo(fullPath);
        }

        if (File.Exists(fullPath))
        {
            var file = new FileInfo(fullPath);

            return file.Directory
                ?? throw new InvalidOperationException(
                    $"Could not determine the directory containing '{fullPath}'."
                );
        }

        throw new DirectoryNotFoundException(
            $"The path '{fullPath}' does not exist."
        );
    }

    private static bool IsGitRepository(DirectoryInfo directory)
    {
        var gitPath = Path.Combine(directory.FullName, ".git");

        return Directory.Exists(gitPath) || File.Exists(gitPath);
    }
}
