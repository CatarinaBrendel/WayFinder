using System.ComponentModel;
using ModelContextProtocol.Server;
using WayFinder.DevTools.Application.Repositories.Navigation;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Mcp.Contracts.Repositories;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class RepositorySourceTools
{
    private readonly WayFinderServices _services;

    public RepositorySourceTools(WayFinderServices services)
    {
        _services = services;
    }

    [McpServerTool(Name = "repo_source_lines")]
    [Description(
        "Reads a specific range of source lines from a file in a registered " +
        "WayFinder repository. Use this to inspect a small section of source " +
        "code without retrieving the entire file."
    )]
    public RepositorySourceResponse ReadLines(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

        [Description(
            "Repository-relative path to the source file."
        )]
        string relativePath,

        [Description(
            "First source line to retrieve (1-based)."
        )]
        int startLine,

        [Description(
            "Number of source lines to retrieve."
        )]
        int lineCount
    )
    {
        var projectContext =
            _services.RegisteredProjectResolver.Resolve(project);

        var excerpt = _services.RepositorySourceNavigator.ReadLines(
            projectContext,
            relativePath,
            startLine,
            lineCount
        );

        return ToResponse(excerpt);
    }

    [McpServerTool(Name = "repo_source_declaration")]
    [Description(
        "Reads the source code of a specific C# declaration in a registered " +
        "WayFinder repository. Use repo_symbols first to discover qualified " +
        "symbol names, then retrieve only the declaration you need."
    )]
    public RepositorySourceResponse ReadDeclaration(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

        [Description(
            "Repository-relative path to the C# source file."
        )]
        string relativePath,

        [Description(
            "Fully qualified declaration name returned by repo_symbols."
        )]
        string qualifiedName
    )
    {
        var projectContext =
            _services.RegisteredProjectResolver.Resolve(project);

        var excerpt = _services.RepositorySourceNavigator.ReadDeclaration(
            projectContext,
            relativePath,
            qualifiedName
        );

        return ToResponse(excerpt);
    }

    private static RepositorySourceResponse ToResponse(
        RepositorySourceExcerpt excerpt
    )
    {
        return new RepositorySourceResponse(
            RelativePath: excerpt.RelativePath,
            StartLine: excerpt.StartLine,
            EndLine: excerpt.EndLine,
            Content: excerpt.Content,
            Truncated: excerpt.Truncated
        );
    }
}
