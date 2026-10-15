using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.Navigation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WayFinder.DevTools.Infrastructure.Repositories.Navigation;

public sealed class RepositorySourceNavigator(
    IProjectFileSystem fileSystem
) : IRepositorySourceNavigator
{
    private const int MaxSourceFileBytes = 1024 * 1024;
    private const int MaxLines = 100;

    public RepositorySourceExcerpt ReadLines(
        ProjectContext project,
        string relativePath,
        int startLine,
        int lineCount
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

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(startLine);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineCount);

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

        return ExtractLines(
            source,
            relativePath,
            startLine,
            lineCount
        );
    }

    public RepositorySourceExcerpt ReadDeclaration(
        ProjectContext project,
        string relativePath,
        string qualifiedName
    )
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedName);

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

        var matches = root
            .DescendantNodesAndSelf()
            .OfType<MemberDeclarationSyntax>()
            .Where(node =>
                CSharpDeclarationNames.GetQualifiedName(node) == qualifiedName
            )
            .Take(2)
            .ToArray();

        if (matches.Length == 0)
        {
            throw new KeyNotFoundException(
                $"Declaration '{qualifiedName}' was not found."
            );
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Declaration '{qualifiedName}' is ambiguous."
            );
        }

        var declaration = matches[0];

        var startLine = syntaxTree
            .GetLineSpan(declaration.Span)
            .StartLinePosition.Line + 1;

        var endLine = syntaxTree
            .GetLineSpan(declaration.Span)
            .EndLinePosition.Line + 1;

        var lineCount = endLine - startLine + 1;

        return ExtractLines(
            source,
            relativePath,
            startLine,
            lineCount
        );
    }

    private static RepositorySourceExcerpt ExtractLines(
        string source,
        string relativePath,
        int startLine,
        int lineCount
    )
    {
        using var reader = new StringReader(source);

        var lines = new List<string>();
        var currentLine = 0;

        var requestedEndLine = (long)startLine + lineCount - 1;

        var cappedEndLine = Math.Min(
            requestedEndLine,
            (long)startLine + MaxLines - 1
        );

        var truncated = false;

        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            currentLine++;

            if (currentLine < startLine)
                continue;

            if (currentLine > cappedEndLine)
            {
                truncated = currentLine <= requestedEndLine;
                break;
            }

            lines.Add(line);
        }

        var endLine = lines.Count == 0
            ? startLine - 1
            : startLine + lines.Count - 1;

        return new RepositorySourceExcerpt(
            RelativePath: relativePath,
            StartLine: startLine,
            EndLine: endLine,
            Content: string.Join("\n", lines),
            Truncated: truncated
        );
    }
}
