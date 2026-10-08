namespace WayFinder.DevTools.Infrastructure.Context;

internal sealed class ContextCandidateRanker
{
    private const int PathMatchScore = 50;
    private const int MatchedTermScore = 20;
    private const int MinimumScore = 40;

    public IReadOnlyCollection<RankedContextCandidate> Rank(
        IReadOnlyCollection<ContextCandidate> candidates
    )
    {
        ArgumentNullException.ThrowIfNull(
            candidates
        );

        var anchorTerms =
            FindAnchorTerms(
                candidates
            );

        return candidates
            .Select(
                candidate =>
                    new RankedContextCandidate(
                        candidate,
                        CalculateScore(candidate)
                    )
            )
            .Where(
                candidate =>
                    IsEligible(
                        candidate,
                        anchorTerms
                    )
            )
            .OrderByDescending(
                candidate => candidate.Score
            )
            .ThenBy(
                candidate => candidate.Candidate.Path,
                StringComparer.Ordinal
            )
            .ToArray();
    }

    private static IReadOnlySet<string> FindAnchorTerms(
        IReadOnlyCollection<ContextCandidate> candidates
    )
    {
        return candidates
            .SelectMany(
                candidate =>
                    candidate.PathMatchedTerms.Intersect(
                        candidate.ContentMatchedTerms,
                        StringComparer.OrdinalIgnoreCase
                    )
            )
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase
            );
    }

    private static bool IsEligible(
        RankedContextCandidate candidate,
        IReadOnlySet<string> anchorTerms
    )
    {
        if (candidate.Score < MinimumScore)
        {
            return false;
        }

        if (candidate.Candidate.ContentMatchedTerms.Count == 0)
        {
            return false;
        }

        if (anchorTerms.Count == 0)
        {
            return true;
        }

        return HasAnchorMatch(
            candidate.Candidate,
            anchorTerms
        );
    }

    private static bool HasAnchorMatch(
        ContextCandidate candidate,
        IReadOnlySet<string> anchorTerms
    )
    {
        return candidate.PathMatchedTerms.Any(
                   anchorTerms.Contains
               )
               || candidate.ContentMatchedTerms.Any(
                   anchorTerms.Contains
               );
    }

    private static int CalculateScore(
        ContextCandidate candidate
    )
    {
        var matchedTerms =
            candidate.PathMatchedTerms
                .Union(
                    candidate.ContentMatchedTerms,
                    StringComparer.OrdinalIgnoreCase
                )
                .Count();

        var score =
            matchedTerms
            * MatchedTermScore;

        if (candidate.PathMatchedTerms.Any())
        {
            score +=
                PathMatchScore;
        }

        return score;
    }
}
