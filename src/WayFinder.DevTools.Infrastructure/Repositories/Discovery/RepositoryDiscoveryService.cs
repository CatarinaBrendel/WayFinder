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
        var files = fileSystem.GetFiles(project);

        var solutions = fileSystem
            .GetFiles(project)
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
            .Select(file =>
                new RepositorySolution(
                    Path.GetFileNameWithoutExtension(file.RelativePath),
                    file.RelativePath
                )
            )
            .ToArray();

        var projects = files.Where(file =>
                file.RelativePath.EndsWith(
                    ".csproj",
                    StringComparison.OrdinalIgnoreCase
                )
            )
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
