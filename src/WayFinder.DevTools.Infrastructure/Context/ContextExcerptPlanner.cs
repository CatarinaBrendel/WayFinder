namespace WayFinder.DevTools.Infrastructure.Context;

internal sealed class ContextExcerptPlanner
{
    private const int ContextLines =
        20;

    public IReadOnlyCollection<ContextExcerpt> Plan(
        int totalLines,
        IReadOnlyCollection<ContextContentMatch> matches
    )
    {
        if (totalLines <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalLines)
            );
        }

        ArgumentNullException.ThrowIfNull(
            matches
        );

        if (matches.Count == 0)
        {
            return [];
        }

        var windows =
            matches
                .Select(
                    match =>
                        new ContextExcerpt(
                            Math.Max(
                                1,
                                match.LineNumber - ContextLines
                            ),
                            Math.Min(
                                totalLines,
                                match.LineNumber + ContextLines
                            ),
                            [match.Term]
                        )
                )
                .OrderBy(
                    excerpt => excerpt.StartLine
                )
                .ThenBy(
                    excerpt => excerpt.EndLine
                )
                .ToArray();

        var merged =
            new List<ContextExcerpt>();

        foreach (var window in windows)
        {
            if (merged.Count == 0)
            {
                merged.Add(
                    window
                );

                continue;
            }

            var previous =
                merged[^1];

            if (window.StartLine <=
                previous.EndLine + 1)
            {
                merged[^1] =
                    Merge(
                        previous,
                        window
                    );

                continue;
            }

            merged.Add(
                window
            );
        }

        return merged
            .OrderByDescending(
                excerpt =>
                    excerpt.MatchedTerms.Count
            )
            .ThenBy(
                excerpt =>
                    excerpt.StartLine
            )
            .ThenBy(
                excerpt =>
                    excerpt.EndLine
            )
            .ToArray();
    }

    private static ContextExcerpt Merge(
        ContextExcerpt first,
        ContextExcerpt second
    )
    {
        var matchedTerms =
            first.MatchedTerms
                .Concat(
                    second.MatchedTerms
                )
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                )
                .OrderBy(
                    term => term,
                    StringComparer.OrdinalIgnoreCase
                )
                .ToArray();

        return new ContextExcerpt(
            Math.Min(
                first.StartLine,
                second.StartLine
            ),
            Math.Max(
                first.EndLine,
                second.EndLine
            ),
            matchedTerms
        );
    }
}
