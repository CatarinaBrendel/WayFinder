using System.ComponentModel;
using ModelContextProtocol.Server;
using WayFinder.DevTools.Application.Repositories.References;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class RepositoryReferenceTools
{
    private readonly WayFinderServices _services;

    public RepositoryReferenceTools(WayFinderServices services)
    {
        _services = services;
    }

    [McpServerTool(Name = "repo_references")]
    [Description(
        "Searches C# source files in a registered repository for syntactic " +
        "references to an identifier. Returns source locations and reference " +
        "kinds without returning source code. Results are based on syntax " +
        "only and do not establish semantic symbol identity. " +
        "The repository must be registered for AI access in WayFinder."
    )]
    public RepositoryReferenceResult GetReferences(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

        [Description(
            "The exact, case-sensitive C# identifier to search for, " +
            "for example 'CalculatePrice' or 'ICustomerService'."
        )]
        string identifier
    )
    {
        // Restrict access to registered repositories.
        var projectContext =
            _services.RegisteredProjectResolver.Resolve(project);

        return _services.RepositoryReferenceReader.Read(
            projectContext,
            identifier
        );
    }
}
