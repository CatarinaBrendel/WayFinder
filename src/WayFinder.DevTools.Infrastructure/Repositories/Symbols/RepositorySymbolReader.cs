using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.Symbols;

namespace WayFinder.DevTools.Infrastructure.Repositories.Symbols;

public sealed class RepositorySymbolReader(
    IProjectFileSystem fileSystem
) : IRepositorySymbolReader
{
    private const int MaxSourceFileBytes = 1024 * 1024;
    private const int MaxSymbols = 500;

    public RepositorySymbolResult Read(
        ProjectContext project,
        string relativePath
    )
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        if (!relativePath.EndsWith(
                ".cs",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The file must be a C# source file.",
                nameof(relativePath)
            );
        }

        var file = fileSystem.Read(
            project,
            relativePath,
            MaxSourceFileBytes + 1
        );

        if (file.Truncated || file.TotalBytes > MaxSourceFileBytes)
        {
            throw new InvalidOperationException(
                "The source file exceeds the maximum supported size."
            );
        }

        var source = SourceTextDecoder.Decode(file.Content);

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var root = syntaxTree.GetRoot();

        var symbols = new List<RepositorySymbol>();

        foreach (var node in root.DescendantNodesAndSelf())
        {
            var kind = GetSymbolKind(node);

            if (kind is null)
            {
                continue;
            }

            var name = CSharpDeclarationNames.GetName(node);

            if (name is null)
            {
                continue;
            }

            if (symbols.Count >= MaxSymbols)
            {
                return new RepositorySymbolResult(
                    symbols,
                    Truncated: true
                );
            }

            var qualifiedName = CSharpDeclarationNames.GetQualifiedName(node)!;

            var line = syntaxTree
                .GetLineSpan(node.Span)
                .StartLinePosition.Line + 1;

            symbols.Add(
                new RepositorySymbol(
                    name,
                    qualifiedName,
                    kind.Value,
                    relativePath,
                    line
                )
            );
        }

        return new RepositorySymbolResult(
            symbols,
            Truncated: false
        );
    }

    private static RepositorySymbolKind? GetSymbolKind(
        SyntaxNode node
    ) => node switch
    {
        BaseNamespaceDeclarationSyntax =>
            RepositorySymbolKind.Namespace,

        ClassDeclarationSyntax =>
            RepositorySymbolKind.Class,

        InterfaceDeclarationSyntax =>
            RepositorySymbolKind.Interface,

        RecordDeclarationSyntax record
            when record.ClassOrStructKeyword.IsKind(
                SyntaxKind.StructKeyword
            ) =>
            RepositorySymbolKind.Struct,

        RecordDeclarationSyntax =>
            RepositorySymbolKind.Record,

        StructDeclarationSyntax =>
            RepositorySymbolKind.Struct,

        EnumDeclarationSyntax =>
            RepositorySymbolKind.Enum,

        MethodDeclarationSyntax =>
            RepositorySymbolKind.Method,

        ConstructorDeclarationSyntax =>
            RepositorySymbolKind.Constructor,

        PropertyDeclarationSyntax =>
            RepositorySymbolKind.Property,

        VariableDeclaratorSyntax variable
            when variable.Parent?.Parent is FieldDeclarationSyntax =>
            RepositorySymbolKind.Field,

        VariableDeclaratorSyntax variable
            when variable.Parent?.Parent is EventFieldDeclarationSyntax =>
            RepositorySymbolKind.Event,

        EventDeclarationSyntax =>
            RepositorySymbolKind.Event,

        DelegateDeclarationSyntax =>
            RepositorySymbolKind.Delegate,

        IndexerDeclarationSyntax =>
            RepositorySymbolKind.Indexer,

        _ => null
    };
}
