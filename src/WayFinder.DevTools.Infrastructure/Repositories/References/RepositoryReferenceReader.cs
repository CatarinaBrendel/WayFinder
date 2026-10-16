using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.References;
using WayFinder.DevTools.Infrastructure.Repositories;

namespace WayFinder.DevTools.Infrastructure.Repositories.References;

public sealed class RepositoryReferenceReader(
    IProjectFileSystem fileSystem
) : IRepositoryReferenceReader
{
    private const int MaximumFiles = 1000;
    private const int MaximumFileBytes = 1024 * 1024;
    private const int MaximumReferences = 200;

    public RepositoryReferenceResult Read(
        ProjectContext project,
        string identifier
    )
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        if (!SyntaxFacts.IsValidIdentifier(identifier))
        {
            throw new ArgumentException(
                "The value must be a valid C# identifier.",
                nameof(identifier)
            );
        }

        var files = fileSystem.GetFiles(project)
            .Where(file => file.RelativePath.EndsWith(
                ".cs",
                StringComparison.OrdinalIgnoreCase
            ))
            .OrderBy(
                file => file.RelativePath,
                StringComparer.Ordinal
            )
            .ToArray();

        var references = new List<RepositoryReference>();
        var filesExamined = 0;
        var truncated = false;

        foreach (var file in files)
        {
            if (filesExamined >= MaximumFiles)
            {
                truncated = true;
                break;
            }

            filesExamined++;

            var read = fileSystem.Read(
                project,
                file.RelativePath,
                MaximumFileBytes + 1
            );

            if (read.Truncated || read.TotalBytes > MaximumFileBytes)
            {
                // An unreadable file makes the search incomplete.
                truncated = true;
                continue;
            }

            string source;

            try
            {
                source = SourceTextDecoder.Decode(read.Content);
            }
            catch (System.Text.DecoderFallbackException)
            {
                // This file could not be analyzed.
                // Continue searching, but mark the result as incomplete.
                truncated = true;
                continue;
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var root = syntaxTree.GetRoot();

            foreach (var node in root.DescendantNodes()
                .OfType<SimpleNameSyntax>())
            {
                if (!string.Equals(
                    node.Identifier.ValueText,
                    identifier,
                    StringComparison.Ordinal
                ))
                {
                    continue;
                }

                // Stop only when another matching reference is found.
                if (references.Count >= MaximumReferences)
                {
                    truncated = true;
                    break;
                }

                var location = syntaxTree.GetLineSpan(node.Span);
                var position = location.StartLinePosition;

                references.Add(new RepositoryReference(
                    RelativePath: file.RelativePath,
                    Line: position.Line + 1,
                    Column: position.Character + 1,
                    Kind: Classify(node)
                ));
            }

            if (references.Count >= MaximumReferences && truncated)
            {
                break;
            }
        }

        return new RepositoryReferenceResult(
            Identifier: identifier,
            References: references,
            FilesExamined: filesExamined,
            Truncated: truncated,
            Resolution: "Syntactic"
        );
    }

    private static string Classify(SimpleNameSyntax node)
    {
        if (node.Parent is MemberAccessExpressionSyntax memberAccess
            && memberAccess.Name == node)
        {
            if (memberAccess.Parent is InvocationExpressionSyntax)
            {
                return "Invocation";
            }

            return "MemberAccess";
        }

        if (node.Parent is InvocationExpressionSyntax)
        {
            return "Invocation";
        }

        if (node.Parent is ObjectCreationExpressionSyntax)
        {
            return "ObjectCreation";
        }

        return "Identifier";
    }
}
