using System.ComponentModel;
using ModelContextProtocol.Server;
using WayFinder.DevTools.Application.Context;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Mcp.Contracts.Context;
using WayFinder.DevTools.Mcp.Mapping;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class ContextTools
{
    private const int DefaultTokenBudget =
        8_000;

    private readonly WayFinderServices _services;

    public ContextTools(
        WayFinderServices services
    )
    {
        _services =
            services;
    }

    [McpServerTool]
    [Description(
        "Builds a bounded, relevant repository context package for a development task."
    )]
    public ContextResponse Context(
        [Description(
            "The ID of a project registered for AI access in WayFinder."
        )]
        string projectId,

        [Description(
            "The development task for which repository context is needed."
        )]
        string task,

        [Description(
            "Maximum estimated number of context tokens to return."
        )]
        int tokenBudget = DefaultTokenBudget
    )
    {
        if (!Guid.TryParse(
                projectId,
                out var parsedProjectId))
        {
            throw new ArgumentException(
                "Project ID must be a valid GUID.",
                nameof(projectId)
            );
        }

        if (string.IsNullOrWhiteSpace(
                task))
        {
            throw new ArgumentException(
                "Task must not be empty.",
                nameof(task)
            );
        }

        if (tokenBudget <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tokenBudget),
                tokenBudget,
                "Token budget must be greater than zero."
            );
        }

        var project =
            _services.RegisteredProjectResolver.Resolve(
                parsedProjectId
            );

        var package =
            _services.ContextCompiler.Compile(
                project,
                new ContextRequest(
                    task,
                    tokenBudget
                )
            );

        return ContextResponseMapper.Map(
            package
        );
    }
}
