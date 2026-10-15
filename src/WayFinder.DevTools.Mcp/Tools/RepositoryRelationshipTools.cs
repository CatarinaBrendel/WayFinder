using System.ComponentModel;
using ModelContextProtocol.Server;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Mcp.Contracts.Repositories;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class RepositoryRelationshipTools
{
    private readonly WayFinderServices _services;

    public RepositoryRelationshipTools(WayFinderServices services)
    {
        _services = services;
    }

    [McpServerTool(Name = "repo_relationships")]
    [Description(
        "Returns project relationships for a registered repository, " +
        "including target frameworks, project references, and external " +
        "assembly references such as Sage 100 DLLs. " +
        "Assembly HintPath values are returned as metadata only; " +
        "this tool does not read external assemblies. " +
        "The project must be registered for AI access in WayFinder."
    )]
    public RepositoryRelationshipsResponse GetRelationships(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

        [Description(
            "Optional repository-relative path to a specific .csproj file. " +
            "When omitted, returns relationships for all discovered projects."
        )]
        string? projectFile = null
    )
    {
        // Enforce the registered-project boundary.
        var projectContext =
            _services.RegisteredProjectResolver.Resolve(project);

        // Reuse the existing application functionality.
        var relationships = _services.RepositoryRelationshipReader.Read(projectContext);

        if (!string.IsNullOrWhiteSpace(projectFile))
        {
            if (Path.IsPathRooted(projectFile)
                || projectFile.Split(
                    ['/', '\\'],
                    StringSplitOptions.RemoveEmptyEntries
                ).Contains(".."))
            {
                throw new ArgumentException(
                    "The project file must be a repository-relative path.",
                    nameof(projectFile)
                );
            }

            var normalizedPath = projectFile.Replace('\\', '/');

            relationships = relationships
                .Where(relationship =>
                    string.Equals(
                        relationship.ProjectPath.Replace('\\', '/'),
                        normalizedPath,
                        StringComparison.Ordinal
                    )
                )
                .ToArray();
        }

        // Convert application models into MCP response contracts.
        var projects = relationships
            .Select(relationship =>
                new RepositoryProjectRelationshipResponse(
                    ProjectPath: relationship.ProjectPath,
                    TargetFrameworks: relationship.TargetFrameworks,
                    References: relationship.References
                        .Select(reference =>
                            new RepositoryProjectReferenceResponse(
                                Path: reference.Path,
                                IsExternal: reference.IsExternal
                            )
                        )
                        .ToArray(),
                    AssemblyReferences: relationship.AssemblyReferences
                        .Select(assembly =>
                            new RepositoryAssemblyReferenceResponse(
                                Name: assembly.Name,
                                HintPath: assembly.HintPath,
                                CopyLocal: assembly.CopyLocal
                            )
                        )
                        .ToArray()
                )
            )
            .ToArray();

        return new RepositoryRelationshipsResponse(projects);
    }
}
