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
        4_000;

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
        "Preferred starting point for investigating or working on a development task. " +
        "Builds a bounded, relevant repository context package. " +
        "Use repo_search and repo_read afterward for targeted follow-up when more detail is needed. " +
        "The project may be specified by exact registered name or ID; calling projects first is not required."
    )]
    public ContextResponse Context(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

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

        var projectContext =
            _services.RegisteredProjectResolver.Resolve(
                project
            );

        var package =
            _services.ContextCompiler.Compile(
                projectContext,
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
