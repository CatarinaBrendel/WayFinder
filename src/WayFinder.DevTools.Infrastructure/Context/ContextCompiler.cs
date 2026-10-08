using WayFinder.DevTools.Application.Context;
using WayFinder.DevTools.Application.Context.Estimation;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.Reading;

namespace WayFinder.DevTools.Infrastructure.Context;

public sealed class ContextCompiler
    : IContextCompiler
{
    private const int MaximumGuidanceTokens =
    2_000;

    private const string GuidancePath =
        "AGENTS.md";

    private readonly TaskTermExtractor _termExtractor;
    private readonly ContextCandidateDiscovery _candidateDiscovery;
    private readonly ContextCandidateRanker _candidateRanker;
    private readonly IRepositoryFileReader _fileReader;
    private readonly ITokenEstimator _tokenEstimator;
    private readonly ContextExcerptPlanner _excerptPlanner;
    private readonly ContextExcerptExtractor _excerptExtractor;

    public ContextCompiler(
        IProjectFileSystem fileSystem,
        IRepositoryFileReader fileReader,
        ITokenEstimator tokenEstimator
    )
    {
        ArgumentNullException.ThrowIfNull(
            fileSystem
        );

        _fileReader =
            fileReader
            ?? throw new ArgumentNullException(
                nameof(fileReader)
            );

        _tokenEstimator =
            tokenEstimator
            ?? throw new ArgumentNullException(
                nameof(tokenEstimator)
            );

        _termExtractor =
            new TaskTermExtractor();

        _candidateDiscovery =
            new ContextCandidateDiscovery(
                fileSystem
            );

        _candidateRanker =
            new ContextCandidateRanker();

        _excerptPlanner =
            new ContextExcerptPlanner();

        _excerptExtractor =
            new ContextExcerptExtractor();
    }

    public ContextPackage Compile(
        ProjectContext project,
        ContextRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(
            project
        );

        ArgumentNullException.ThrowIfNull(
            request
        );

        if (request.TokenBudget <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Token budget must be greater than zero."
            );
        }

        var terms =
            _termExtractor.Extract(
                request.Task,
                project.Name
            );

        var candidates =
            _candidateDiscovery.Discover(
                project,
                terms
            );

        var rankedCandidates =
            _candidateRanker.Rank(
                candidates
            );

        var guidance =
            TryReadGuidance(
                project,
                request.TokenBudget
            );

        var usedTokens =
            guidance?.EstimatedTokens
            ?? 0;

        var files =
            MaterializeFiles(
                project,
                rankedCandidates,
                request.TokenBudget,
                ref usedTokens
            );

        var includedPaths =
            files
                .Select(
                    file => file.Path
                )
                .ToHashSet(
                    StringComparer.Ordinal
                );

        var selections =
            rankedCandidates
                .Select(
                    candidate =>
                        new ContextSelection(
                            candidate.Candidate.Path,
                            candidate.Score,
                            candidate.Candidate.PathMatchedTerms,
                            candidate.Candidate.ContentMatchedTerms,
                            includedPaths.Contains(
                                candidate.Candidate.Path
                            )
                        )
                )
                .ToArray();

        var fileCount =
            files
                .Select(
                    file => file.Path
                )
                .Distinct(
                    StringComparer.Ordinal
                )
                .Count();

        return new ContextPackage(
            request.Task,
            project.Name,
            guidance,
            files,
            selections,
            new ContextStatistics(
                fileCount,
                usedTokens,
                request.TokenBudget
            )
        );
    }

    private ContextFile? TryReadGuidance(
        ProjectContext project,
        int tokenBudget
    )
    {
        RepositoryFileContent content;

        try
        {
            content =
                _fileReader.Read(
                    project,
                    GuidancePath
                );
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (BinaryFileNotSupportedException)
        {
            return null;
        }

        if (content.Truncated)
        {
            return null;
        }

        var estimatedTokens =
            _tokenEstimator.Estimate(
                content.Content
            );

        if (estimatedTokens >
            MaximumGuidanceTokens)
        {
            return null;
        }

        if (estimatedTokens >
            tokenBudget)
        {
            return null;
        }

        return new ContextFile(
            GuidancePath,
            content.Content,
            estimatedTokens
        );
    }

    private IReadOnlyCollection<ContextFile> MaterializeFiles(
        ProjectContext project,
        IReadOnlyCollection<RankedContextCandidate> candidates,
        int tokenBudget,
        ref int usedTokens
    )
    {
        var files =
            new List<ContextFile>();

        foreach (var rankedCandidate in candidates)
        {
            var path =
                rankedCandidate.Candidate.Path;

            if (string.Equals(
                    path,
                    GuidancePath,
                    StringComparison.Ordinal))
            {
                continue;
            }

            RepositoryFileContent content;

            try
            {
                content =
                    _fileReader.Read(
                        project,
                        path
                    );
            }
            catch (BinaryFileNotSupportedException)
            {
                continue;
            }
            catch (FileNotFoundException)
            {
                continue;
            }

            if (content.Truncated)
            {
                continue;
            }

            var estimatedTokens =
    _tokenEstimator.Estimate(
        content.Content
    );

            if (usedTokens + estimatedTokens <=
                tokenBudget)
            {
                files.Add(
                    new ContextFile(
                        path,
                        content.Content,
                        estimatedTokens,
                        ContextFileKind.Complete
                    )
                );

                usedTokens +=
                    estimatedTokens;

                continue;
            }

            MaterializeExcerpts(
                rankedCandidate.Candidate,
                content.Content,
                tokenBudget,
                files,
                ref usedTokens
            );
        }

        return files;
    }

    private void MaterializeExcerpts(
    ContextCandidate candidate,
    string content,
    int tokenBudget,
    ICollection<ContextFile> files,
    ref int usedTokens
)
    {
        var totalLines =
            CountLines(
                content
            );

        var excerpts =
            _excerptPlanner.Plan(
                totalLines,
                candidate.ContentMatches
            );

        foreach (var excerpt in excerpts)
        {
            var extracted =
                _excerptExtractor.Extract(
                    content,
                    excerpt
                );

            var estimatedTokens =
                _tokenEstimator.Estimate(
                    extracted.Content
                );

            if (usedTokens + estimatedTokens >
                tokenBudget)
            {
                continue;
            }

            files.Add(
                new ContextFile(
                    candidate.Path,
                    extracted.Content,
                    estimatedTokens,
                    ContextFileKind.Excerpt,
                    extracted.StartLine,
                    extracted.EndLine
                )
            );

            usedTokens +=
                estimatedTokens;
        }
    }

    private static int CountLines(
    string content
)
    {
        if (content.Length == 0)
        {
            return 1;
        }

        var lineCount =
            1;

        for (var index = 0;
             index < content.Length;
             index++)
        {
            if (content[index] == '\n')
            {
                lineCount++;
            }
            else if (content[index] == '\r'
                     && (index + 1 == content.Length
                         || content[index + 1] != '\n'))
            {
                lineCount++;
            }
        }

        if (content.EndsWith(
                '\n')
            || content.EndsWith(
                '\r'))
        {
            lineCount--;
        }

        return Math.Max(
            1,
            lineCount
        );
    }
}
