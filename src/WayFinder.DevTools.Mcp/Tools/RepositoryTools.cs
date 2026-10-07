using System.ComponentModel;
using ModelContextProtocol.Server;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Mcp.Contracts.Repositories.Reading;
using WayFinder.DevTools.Mcp.Contracts.Repositories.Searching;
using WayFinder.DevTools.Mcp.Mapping;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class RepositoryTools
{
    private readonly WayFinderServices _services;

    public RepositoryTools(
        WayFinderServices services
    )
    {
        _services =
            services;
    }

    [McpServerTool(Name = "repo_search")]
    [Description(
        "Searches text files in a registered WayFinder project for a string. "
        + "Returns bounded repository-relative matches with line numbers."
    )]
    public RepositorySearchResponse Search(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

        [Description(
            "A single literal text string to search for, matched case-insensitively. "
            + "Regex, wildcards, and OR expressions are not supported. "
            + "Call repo_search separately for different search terms."
        )]
        string query
    )
    {
        if (string.IsNullOrWhiteSpace(
                query))
        {
            throw new ArgumentException(
                "Search query must not be empty.",
                nameof(query)
            );
        }

        var projectContext =
            _services.RegisteredProjectResolver.Resolve(
                project
            );

        var result =
            _services.RepositorySearcher.Search(
                projectContext,
                query
            );

        return RepositorySearchResponseMapper.Map(
            result
        );
    }

    [McpServerTool(Name = "repo_read")]
    [Description(
        "Reads a text file from a registered WayFinder project. "
        + "The path must be repository-relative and the returned content is bounded."
    )]
    public RepositoryFileResponse Read(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

        [Description(
            "Repository-relative path of the text file to read."
        )]
        string path
    )
    {
        if (string.IsNullOrWhiteSpace(
                path))
        {
            throw new ArgumentException(
                "Path must not be empty.",
                nameof(path)
            );
        }

        var projectContext =
            _services.RegisteredProjectResolver.Resolve(
                project
            );

        var content =
            _services.RepositoryFileReader.Read(
                projectContext,
                path
            );

        return RepositoryFileResponseMapper.Map(
            content
        );
    }
}
