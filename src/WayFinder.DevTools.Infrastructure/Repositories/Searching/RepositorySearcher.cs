using System.Text;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.Searching;

namespace WayFinder.DevTools.Infrastructure.Repositories.Searching;

public sealed class RepositorySearcher(
    IProjectFileSystem fileSystem
) : IRepositorySearcher
{
    private const int MaximumFileBytes = 1024 * 1024;
    private const int MaximumMatches = 50;
    private const int MaximumLineLength = 300;

    private static readonly UTF8Encoding StrictUtf8 =
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true
        );

    public RepositorySearchResult Search(
        ProjectContext project,
        string query
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var matches =
            new List<RepositorySearchMatch>();

        foreach (var file in fileSystem.GetFiles(project))
        {
            var read =
                fileSystem.Read(
                    project,
                    file.RelativePath,
                    MaximumFileBytes
                );

            if (read.Truncated)
            {
                continue;
            }

            if (!TryDecodeText(
                    read.Content,
                    out var content))
            {
                continue;
            }

            foreach (var match in FindMatches(
                         file.RelativePath,
                         content,
                         query))
            {
                if (matches.Count == MaximumMatches)
                {
                    return new RepositorySearchResult(
                        Matches: matches,
                        Truncated: true
                    );
                }

                matches.Add(match);
            }
        }

        return new RepositorySearchResult(
            Matches: matches,
            Truncated: false
        );
    }

    private static IEnumerable<RepositorySearchMatch> FindMatches(
        string path,
        string content,
        string query
    )
    {
        using var reader =
            new StringReader(content);

        var lineNumber = 0;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;

            if (!line.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new RepositorySearchMatch(
                Path: path,
                LineNumber: lineNumber,
                Line: TruncateLine(line)
            );
        }
    }

    private static bool TryDecodeText(
        byte[] content,
        out string text
    )
    {
        text = string.Empty;

        if (content.AsSpan().Contains((byte)0))
        {
            return false;
        }

        try
        {
            text =
                StrictUtf8.GetString(content);

            if (text.Length > 0
                && text[0] == '\uFEFF')
            {
                text = text[1..];
            }

            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string TruncateLine(
        string line
    )
    {
        if (line.Length <= MaximumLineLength)
        {
            return line;
        }

        return line[..MaximumLineLength];
    }
}
