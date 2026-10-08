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
        "Lists projects registered for AI access in WayFinder. "
        + "Use this to discover or confirm the exact registered project name or ID when needed. "
        + "If an exact registered name or ID is already known, other WayFinder tools can use it directly."
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
