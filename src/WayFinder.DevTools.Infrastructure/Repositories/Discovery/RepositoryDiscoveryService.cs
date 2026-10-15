using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.Discovery;

namespace WayFinder.DevTools.Infrastructure.Repositories.Discovery;

public sealed class RepositoryDiscoveryService(
    IProjectFileSystem fileSystem
) : IRepositoryDiscoveryService
{
    public RepositoryDiscoveryResult Discover(ProjectContext project)
    {
        ArgumentNullException.ThrowIfNull(project);

        const int maxSolutions = 100;
        const int maxProjects = 500;

        var files = fileSystem.GetFiles(project);

        var solutions = files
            .Where(file =>
                file.RelativePath.EndsWith(
                    ".sln",
                    StringComparison.OrdinalIgnoreCase
                )
                || file.RelativePath.EndsWith(
                    ".slnx",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .OrderBy(
                file => file.RelativePath,
                StringComparer.Ordinal
            )
            .Take(maxSolutions)
            .Select(file =>
                new RepositorySolution(
                    Path.GetFileNameWithoutExtension(file.RelativePath),
                    file.RelativePath
                )
            )
            .ToArray();

        var projects = files
            .Where(file =>
                file.RelativePath.EndsWith(
                    ".csproj",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .OrderBy(
                file => file.RelativePath,
                StringComparer.Ordinal
            )
            .Take(maxProjects)
            .Select(file =>
                new RepositoryProject(
                    Path.GetFileNameWithoutExtension(file.RelativePath),
                    file.RelativePath
                )
            )
            .ToArray();

        return new RepositoryDiscoveryResult(
            Solutions: solutions,
            Projects: projects
        );
    }
}
