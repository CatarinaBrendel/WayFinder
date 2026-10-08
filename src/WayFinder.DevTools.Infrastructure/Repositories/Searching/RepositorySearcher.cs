using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.Searching;

namespace WayFinder.DevTools.Infrastructure.Repositories.Searching;

public sealed class RepositorySearcher : IRepositorySearcher
{
    private const int MaximumMatches = 50;
    private const int MaximumLineLength = 300;

    private readonly IProjectFileSystem _fileSystem;
    private readonly RepositoryTextReader _textReader;

    public RepositorySearcher(
        IProjectFileSystem fileSystem
    )
    {
        _fileSystem = fileSystem;
        _textReader =
            new RepositoryTextReader(
                fileSystem
            );
    }

    public RepositorySearchResult Search(
        ProjectContext project,
        string query
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var matches =
            new List<RepositorySearchMatch>();

        foreach (var file in _fileSystem.GetFiles(project))
        {
            if (!_textReader.TryRead(
                    project,
                    file.RelativePath,
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
