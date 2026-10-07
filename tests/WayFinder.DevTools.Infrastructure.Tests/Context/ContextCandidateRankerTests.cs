using WayFinder.DevTools.Infrastructure.Context;

namespace WayFinder.DevTools.Infrastructure.Tests.Context;

public sealed class ContextCandidateRankerTests
{
    private readonly ContextCandidateRanker _ranker =
        new();

    [Fact]
    public void Rank_PrefersPathAndContentMatchOverContentOnlyMatch()
    {
        var pathAndContent =
            Candidate(
                "src/Doctor.cs",
                pathTerms: ["doctor"],
                contentTerms: ["doctor"]
            );

        var contentOnly =
            Candidate(
                "src/HealthCheck.cs",
                pathTerms: [],
                contentTerms: ["doctor", "project"]
            );

        var result =
            _ranker.Rank(
                [
                    contentOnly,
                    pathAndContent,
                ]
            )
            .ToArray();

        Assert.Equal(
            2,
            result.Length
        );

        Assert.Equal(
            "src/Doctor.cs",
            result[0].Candidate.Path
        );

        Assert.Equal(
            70,
            result[0].Score
        );

        Assert.Equal(
            "src/HealthCheck.cs",
            result[1].Candidate.Path
        );

        Assert.Equal(
            40,
            result[1].Score
        );
    }

    [Fact]
    public void Rank_RewardsMultipleContentMatches()
    {
        var oneTerm =
            Candidate(
                "src/One.cs",
                pathTerms: [],
                contentTerms: ["doctor"]
            );

        var twoTerms =
            Candidate(
                "src/Two.cs",
                pathTerms: [],
                contentTerms: ["doctor", "project"]
            );

        var result =
            _ranker.Rank(
                [
                    oneTerm,
                    twoTerms,
                ]
            )
            .ToArray();

        var ranked =
            Assert.Single(
                result
            );

        Assert.Equal(
            "src/Two.cs",
            ranked.Candidate.Path
        );

        Assert.Equal(
            40,
            ranked.Score
        );
    }

    [Fact]
    public void Rank_CombinesPathAndContentEvidence()
    {
        var candidate =
            Candidate(
                "src/Doctor.cs",
                pathTerms: ["doctor"],
                contentTerms: ["doctor", "project"]
            );

        var result =
            _ranker.Rank(
                [candidate]
            );

        var ranked =
            Assert.Single(
                result
            );

        Assert.Equal(
            90,
            ranked.Score
        );
    }

    [Fact]
    public void Rank_UsesPathAsDeterministicTieBreaker()
    {
        var second =
            Candidate(
                "src/Beta.cs",
                pathTerms: [],
                contentTerms: ["doctor", "project"]
            );

        var first =
            Candidate(
                "src/Alpha.cs",
                pathTerms: [],
                contentTerms: ["doctor", "project"]
            );

        var result =
            _ranker.Rank(
                [
                    second,
                    first,
                ]
            )
            .ToArray();

        Assert.Equal(
            2,
            result.Length
        );

        Assert.Equal(
            "src/Alpha.cs",
            result[0].Candidate.Path
        );

        Assert.Equal(
            "src/Beta.cs",
            result[1].Candidate.Path
        );

        Assert.Equal(
            40,
            result[0].Score
        );

        Assert.Equal(
            40,
            result[1].Score
        );
    }

    [Fact]
    public void Rank_ExcludesWeakSingleContentMatch()
    {
        var candidate =
            Candidate(
                "src/HealthCheck.cs",
                pathTerms: [],
                contentTerms: ["doctor"]
            );

        var result =
            _ranker.Rank(
                [candidate]
            );

        Assert.Empty(
            result
        );
    }

    [Fact]
    public void Rank_ExcludesFilenameOnlyMatch()
    {
        var candidate =
            Candidate(
                "src/ProjectFile.cs",
                pathTerms: ["project"],
                contentTerms: []
            );

        var result =
            _ranker.Rank(
                [candidate]
            );

        Assert.Empty(
            result
        );
    }

    [Fact]
    public void Rank_ExcludesCandidateWithoutAnchorWhenAnchorExists()
    {
        var doctor =
            Candidate(
                "src/Doctor.cs",
                pathTerms: ["doctor"],
                contentTerms: ["doctor"]
            );

        var unrelated =
            Candidate(
                "tests/TaskTermExtractorTests.cs",
                pathTerms: [],
                contentTerms: ["stale", "handling"]
            );

        var result =
            _ranker.Rank(
                [
                    unrelated,
                doctor,
                ]
            )
            .ToArray();

        var ranked =
            Assert.Single(
                result
            );

        Assert.Equal(
            "src/Doctor.cs",
            ranked.Candidate.Path
        );
    }

    [Fact]
    public void Rank_UsesLexicalScoringWhenNoAnchorExists()
    {
        var candidate =
            Candidate(
                "src/EncodingReader.cs",
                pathTerms: [],
                contentTerms:
                [
                    "malformed",
                "UTF8",
                ]
            );

        var result =
            _ranker.Rank(
                [candidate]
            );

        var ranked =
            Assert.Single(
                result
            );

        Assert.Equal(
            40,
            ranked.Score
        );
    }

    private static ContextCandidate Candidate(
        string path,
        IReadOnlyCollection<string> pathTerms,
        IReadOnlyCollection<string> contentTerms
    )
    {
        return new ContextCandidate(
            path,
            pathTerms,
            contentTerms,
            []
        );
    }


}
