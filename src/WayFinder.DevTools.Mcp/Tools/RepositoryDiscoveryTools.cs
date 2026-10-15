using System.ComponentModel;
using ModelContextProtocol.Server;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Mcp.Contracts.Repositories;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class RepositoryDiscoveryTools
{
    private readonly WayFinderServices _services;

    public RepositoryDiscoveryTools(
        WayFinderServices services
    )
    {
        _services = services;
    }

    [McpServerTool(Name = "repo_discover")]
    [Description(
        "Discovers solution and project files within a registered repository. " +
        "Returns repository-relative paths. " +
        "Use this tool to understand repository structure before inspecting dependencies or source code. " +
        "The project must be registered for AI access in WayFinder."
    )]
    public RepositoryDiscoveryResponse Discover(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project
    )
    {
        var projectContext =
            _services.RegisteredProjectResolver.Resolve(
                project
            );

        var result =
            _services.RepositoryDiscoveryService.Discover(
                projectContext
            );

        return new RepositoryDiscoveryResponse(
            result.Solutions
                .Select(solution =>
                    new RepositorySolutionResponse(
                        solution.Name,
                        solution.RelativePath
                    )
                )
                .ToArray(),

            result.Projects
                .Select(repositoryProject =>
                    new RepositoryProjectResponse(
                        repositoryProject.Name,
                        repositoryProject.RelativePath
                    )
                )
                .ToArray()
        );
    }
}
