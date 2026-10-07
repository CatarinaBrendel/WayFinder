using WayFinder.DevTools.Infrastructure.Context;

namespace WayFinder.DevTools.Infrastructure.Tests.Context;

public sealed class ContextExcerptPlannerTests
{
    private readonly ContextExcerptPlanner _planner =
        new();

    [Fact]
    public void Plan_CentersWindowAroundMatch()
    {
        var result =
            _planner.Plan(
                100,
                [
                    Match(50),
                ]
            );

        var excerpt =
            Assert.Single(
                result
            );

        Assert.Equal(
            30,
            excerpt.StartLine
        );

        Assert.Equal(
            70,
            excerpt.EndLine
        );

        Assert.Equal(
            ["term"],
            excerpt.MatchedTerms
        );
    }

    [Fact]
    public void Plan_ClampsWindowToStartOfFile()
    {
        var result =
            _planner.Plan(
                100,
                [
                    Match(5),
                ]
            );

        var excerpt =
            Assert.Single(
                result
            );

        Assert.Equal(
            1,
            excerpt.StartLine
        );

        Assert.Equal(
            25,
            excerpt.EndLine
        );
    }

    [Fact]
    public void Plan_ClampsWindowToEndOfFile()
    {
        var result =
            _planner.Plan(
                100,
                [
                    Match(95),
                ]
            );

        var excerpt =
            Assert.Single(
                result
            );

        Assert.Equal(
            75,
            excerpt.StartLine
        );

        Assert.Equal(
            100,
            excerpt.EndLine
        );
    }

    [Fact]
    public void Plan_MergesOverlappingWindows()
    {
        var result =
            _planner.Plan(
                200,
                [
                    Match(
                        "doctor",
                        50
                    ),
                    Match(
                        "project",
                        70
                    ),
                ]
            );

        var excerpt =
            Assert.Single(
                result
            );

        Assert.Equal(
            30,
            excerpt.StartLine
        );

        Assert.Equal(
            90,
            excerpt.EndLine
        );

        Assert.Equal(
            ["doctor", "project"],
            excerpt.MatchedTerms
        );
    }

    [Fact]
    public void Plan_KeepsSeparatedWindowsSeparate()
    {
        var result =
            _planner.Plan(
                200,
                [
                    Match(30),
                    Match(150),
                ]
            )
            .ToArray();

        Assert.Equal(
            2,
            result.Length
        );

        Assert.Equal(
            10,
            result[0].StartLine
        );

        Assert.Equal(
            50,
            result[0].EndLine
        );

        Assert.Equal(
            130,
            result[1].StartLine
        );

        Assert.Equal(
            170,
            result[1].EndLine
        );
    }

    [Fact]
    public void Plan_ReturnsEmptyWhenThereAreNoMatches()
    {
        var result =
            _planner.Plan(
                100,
                []
            );

        Assert.Empty(
            result
        );
    }

    [Fact]
    public void Plan_PrioritizesExcerptWithMoreDistinctMatchedTerms()
    {
        var result =
            _planner.Plan(
                300,
                [
                    Match(
                        "RepositorySearcher",
                        30
                    ),
                    Match(
                        "RepositorySearcher",
                        100
                    ),
                    Match(
                        "skip",
                        200
                    ),
                    Match(
                        "UTF8",
                        205
                    ),
                ]
            )
            .ToArray();

        Assert.Equal(
            3,
            result.Length
        );

        Assert.Equal(
            180,
            result[0].StartLine
        );

        Assert.Equal(
            225,
            result[0].EndLine
        );

        Assert.Equal(
            ["skip", "UTF8"],
            result[0].MatchedTerms
        );
    }

    [Fact]
    public void Plan_DoesNotRewardRepeatedMatchesOfSameTerm()
    {
        var result =
            _planner.Plan(
                300,
                [
                    Match(
                        "doctor",
                        100
                    ),
                    Match(
                        "doctor",
                        105
                    ),
                    Match(
                        "doctor",
                        110
                    ),
                ]
            );

        var excerpt =
            Assert.Single(
                result
            );

        Assert.Equal(
            ["doctor"],
            excerpt.MatchedTerms
        );
    }

    private static ContextContentMatch Match(
        int lineNumber
    )
    {
        return Match(
            "term",
            lineNumber
        );
    }

    private static ContextContentMatch Match(
        string term,
        int lineNumber
    )
    {
        return new ContextContentMatch(
            term,
            lineNumber
        );
    }
}
