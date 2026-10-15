using System.ComponentModel;
using ModelContextProtocol.Server;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Mcp.Contracts.Repositories;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class RepositorySymbolTools
{
    private readonly WayFinderServices _services;

    public RepositorySymbolTools(WayFinderServices services)
    {
        _services = services;
    }

    [McpServerTool(Name = "repo_symbols")]
    [Description(
        "Lists C# declarations in a specific source file of a registered " +
        "repository. Returns namespaces, types, methods, qualified names, " +
        "and source line numbers without returning source code. " +
        "Use repo_source to inspect individual declarations afterward. " +
        "The repository must be registered for AI access in WayFinder."
    )]
    public RepositorySymbolsResponse GetSymbols(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

        [Description(
            "Repository-relative path to the C# source file, for example " +
            "'src/Services/CustomerService.cs'."
        )]
        string relativePath
    )
    {
        // Restrict access to registered repositories.
        var projectContext =
            _services.RegisteredProjectResolver.Resolve(project);

        // Delegate filesystem validation and Roslyn analysis
        // to the existing repository symbol reader.
        var result = _services.RepositorySymbolReader.Read(
            projectContext,
            relativePath
        );

        // Convert application symbols into MCP response DTOs.
        var symbols = result.Symbols
            .Select(symbol =>
                new RepositorySymbolResponse(
                    Name: symbol.Name,
                    QualifiedName: symbol.QualifiedName,
                    Kind: symbol.Kind.ToString(),
                    RelativePath: symbol.RelativePath,
                    Line: symbol.Line
                )
            )
            .ToArray();

        return new RepositorySymbolsResponse(
            Symbols: symbols,
            Truncated: result.Truncated
        );
    }
}
