using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.Searching;

namespace WayFinder.DevTools.Infrastructure.Context;

internal sealed class ContextCandidateDiscovery
{
    private readonly IProjectFileSystem _fileSystem;
    private readonly RepositoryTextReader _textReader;

    public ContextCandidateDiscovery(
        IProjectFileSystem fileSystem
    )
    {
        _fileSystem = fileSystem;
        _textReader =
            new RepositoryTextReader(
                fileSystem
            );
    }

    public IReadOnlyCollection<ContextCandidate> Discover(
        ProjectContext project,
        IReadOnlyCollection<string> terms
    )
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(terms);

        var candidates =
            new Dictionary<string, CandidateBuilder>(
                StringComparer.Ordinal
            );

        DiscoverPathMatches(
            project,
            terms,
            candidates
        );

        DiscoverContentMatches(
            project,
            terms,
            candidates
        );

        return candidates
            .OrderBy(
                pair => pair.Key,
                StringComparer.Ordinal
            )
            .Select(
                pair => pair.Value.Build(
                    pair.Key
                )
            )
            .ToArray();
    }

    private void DiscoverPathMatches(
        ProjectContext project,
        IReadOnlyCollection<string> terms,
        Dictionary<string, CandidateBuilder> candidates
    )
    {
        foreach (var file in _fileSystem.GetFiles(project))
        {
            var fileName =
                Path.GetFileNameWithoutExtension(
                    file.RelativePath
                );

            var components =
                SplitIdentifier(
                    fileName
                );

            foreach (var term in terms)
            {
                if (!components.Contains(
                        term,
                        StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                GetOrCreate(
                    candidates,
                    file.RelativePath
                ).AddPathMatch(
                    term
                );
            }
        }
    }

    private void DiscoverContentMatches(
        ProjectContext project,
        IReadOnlyCollection<string> terms,
        Dictionary<string, CandidateBuilder> candidates
    )
    {
        foreach (var file in _fileSystem.GetFiles(project))
        {
            if (!_textReader.TryRead(
                    project,
                    file.RelativePath,
                    out var content))
            {
                continue;
            }

            using var reader =
                new StringReader(content);

            var lineNumber = 0;

            while (reader.ReadLine() is { } line)
            {
                lineNumber++;

                foreach (var term in terms)
                {
                    if (!line.Contains(
                            term,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    GetOrCreate(
                        candidates,
                        file.RelativePath
                    ).AddContentMatch(
                        term,
                        lineNumber
                    );
                }
            }
        }
    }

    private static CandidateBuilder GetOrCreate(
        Dictionary<string, CandidateBuilder> candidates,
        string path
    )
    {
        if (candidates.TryGetValue(
                path,
                out var candidate))
        {
            return candidate;
        }

        candidate =
            new CandidateBuilder();

        candidates.Add(
            path,
            candidate
        );

        return candidate;
    }

    private sealed class CandidateBuilder
    {
        private readonly HashSet<string> _pathMatchedTerms =
            new(
                StringComparer.OrdinalIgnoreCase
            );

        private readonly HashSet<string> _contentMatchedTerms =
            new(
                StringComparer.OrdinalIgnoreCase
            );

        private readonly HashSet<ContextContentMatch> _contentMatches = [];

        public void AddPathMatch(
            string term
        )
        {
            _pathMatchedTerms.Add(
                term
            );
        }

        public void AddContentMatch(
            string term,
            int lineNumber
        )
        {
            _contentMatchedTerms.Add(
                term
            );

            _contentMatches.Add(
                new ContextContentMatch(
                    term,
                    lineNumber
                )
            );
        }

        public ContextCandidate Build(
            string path
        )
        {
            return new ContextCandidate(
                path,
                _pathMatchedTerms
                    .OrderBy(
                        term => term,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ToArray(),
                _contentMatchedTerms
                    .OrderBy(
                        term => term,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ToArray(),
                _contentMatches
                    .OrderBy(
                        match => match.LineNumber
                    )
                    .ThenBy(
                        match => match.Term,
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            );
        }
    }

    private static IReadOnlyCollection<string> SplitIdentifier(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        var components =
            new List<string>();

        var start = 0;

        for (var index = 1; index < value.Length; index++)
        {
            var current = value[index];
            var previous = value[index - 1];

            if (!char.IsLetterOrDigit(current))
            {
                AddComponent(
                    value,
                    start,
                    index,
                    components
                );

                start = index + 1;
                continue;
            }

            if (!char.IsLetterOrDigit(previous))
            {
                start = index;
                continue;
            }

            var startsNewWord =
                char.IsUpper(current)
                && char.IsLower(previous);

            var endsAcronym =
                char.IsUpper(current)
                && char.IsUpper(previous)
                && index + 1 < value.Length
                && char.IsLower(value[index + 1]);

            if (!startsNewWord
                && !endsAcronym)
            {
                continue;
            }

            AddComponent(
                value,
                start,
                index,
                components
            );

            start = index;
        }

        AddComponent(
            value,
            start,
            value.Length,
            components
        );

        return components;
    }

    private static void AddComponent(
        string value,
        int start,
        int end,
        ICollection<string> components
    )
    {
        if (end <= start)
        {
            return;
        }

        components.Add(
            value[start..end]
        );
    }
}
