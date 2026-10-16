using System.ComponentModel;
using ModelContextProtocol.Server;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Application.Repositories.TypeRelationships;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class RepositoryTypeRelationshipTools
{
    private readonly WayFinderServices _services;

    public RepositoryTypeRelationshipTools(WayFinderServices services)
    {
        _services = services;
    }

    [McpServerTool(Name = "repo_type_relationships")]
    [Description(
        "Returns syntactically declared C# base types and interfaces " +
        "for type declarations in a registered repository source file. " +
        "Does not perform semantic type resolution or read external assemblies."
    )]
    public RepositoryTypeRelationshipResult GetTypeRelationships(
    [Description(
        "The ID or exact name of a project registered for AI access."
    )]
    string project,

    [Description(
        "Repository-relative path to a C# source file."
    )]
    string relativePath
)
    {
        var context =
            _services.RegisteredProjectResolver.Resolve(project);

        return _services.RepositoryTypeRelationshipReader.Read(
            context,
            relativePath
        );
    }
}
