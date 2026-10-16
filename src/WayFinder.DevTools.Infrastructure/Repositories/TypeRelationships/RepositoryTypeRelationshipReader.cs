using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.TypeRelationships;

namespace WayFinder.DevTools.Infrastructure.Repositories.TypeRelationships;

public sealed class RepositoryTypeRelationshipReader(
    IProjectFileSystem fileSystem
) : IRepositoryTypeRelationshipReader
{
    private const int MaxSourceFileBytes = 1024 * 1024;
    private const int MaxRelationships = 500;

    public RepositoryTypeRelationshipResult Read(
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

        var relationships = new List<RepositoryTypeRelationship>();

        foreach (var declaration in root
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>())
        {
            if (declaration.BaseList is null)
            {
                continue;
            }

            // A 501st matching declaration means the result is incomplete.
            if (relationships.Count >= MaxRelationships)
            {
                return new RepositoryTypeRelationshipResult(
                    Relationships: relationships,
                    Truncated: true
                );
            }

            var baseTypes = declaration.BaseList.Types
                .Select(type => type.Type.ToString())
                .ToArray();

            var qualifiedName =
                CSharpDeclarationNames.GetQualifiedName(declaration);

            if (qualifiedName is null)
            {
                continue;
            }

            var line = syntaxTree
                .GetLineSpan(declaration.Span)
                .StartLinePosition.Line + 1;

            relationships.Add(
                new RepositoryTypeRelationship(
                    Name: declaration.Identifier.ValueText,
                    QualifiedName: qualifiedName,
                    Kind: declaration.Kind().ToString(),
                    RelativePath: relativePath,
                    Line: line,
                    DeclaredBaseTypes: baseTypes
                )
            );
        }

        return new RepositoryTypeRelationshipResult(
            Relationships: relationships,
            Truncated: false
        );
    }
}
