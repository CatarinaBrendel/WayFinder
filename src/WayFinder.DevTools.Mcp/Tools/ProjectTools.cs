using ModelContextProtocol.Server;
using System.ComponentModel;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class ProjectTools
{
    private readonly WayFinderServices _services;

    public ProjectTools(
        WayFinderServices services
    )
    {
        _services =
            services;
    }

    [McpServerTool]
    [Description(
        "Lists projects that are registered for AI access in WayFinder."
    )]
    public object[] Projects()
    {
        return _services.ProjectRegistry
            .GetAll()
            .Select(
                project =>
                    new
                    {
                        project.Id,
                        project.Name,
                    }
            )
            .Cast<object>()
            .ToArray();
    }
}
