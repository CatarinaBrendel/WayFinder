using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.Searching;

namespace WayFinder.DevTools.Infrastructure.Context;

internal sealed class ContextCandidateDiscovery
{
    private readonly IProjectFileSystem _fileSystem;
    private readonly IRepositorySearcher _searcher;

    public ContextCandidateDiscovery(
        IProjectFileSystem fileSystem,
        IRepositorySearcher searcher
    )
    {
        _fileSystem = fileSystem;
        _searcher = searcher;
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

            foreach (var term in terms)
            {
                if (!fileName.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase))
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
        foreach (var term in terms)
        {
            var result =
                _searcher.Search(
                    project,
                    term
                );

            foreach (var match in result.Matches)
            {
                GetOrCreate(
                    candidates,
                    match.Path
                ).AddContentMatch(
                    term,
                    match.LineNumber
                );
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
}
