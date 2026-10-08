namespace WayFinder.DevTools.Infrastructure.Context;

internal sealed class TaskTermExtractor
{
    private static readonly HashSet<string> StopWords =
        new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "a",
            "an",
            "and",
            "are",
            "be",
            "for",
            "from",
            "in",
            "into",
            "is",
            "it",
            "of",
            "on",
            "or",
            "should",
            "that",
            "the",
            "this",
            "to",
            "when",
            "with",

            // Low-information task verbs
            "add",
            "change",
            "create",
            "fix",
            "handle",
            "implement",
            "improve",
            "update",

            // Low-information task language
            "explain",
            "find",
            "how",
            "investigate",
            "over",
            "where",
            "work",
            "works",
        };

    public IReadOnlyCollection<string> Extract(
        string task,
        string projectName
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            task
        );

        return Tokenize(task)
            .Where(IsUseful)
            .Where(
                term => !string.Equals(
                    term,
                    projectName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Distinct(
                StringComparer.OrdinalIgnoreCase
            )
            .ToArray();
    }

    private static IEnumerable<string> Tokenize(
        string task
    )
    {
        var start = -1;

        for (var index = 0;
             index <= task.Length;
             index++)
        {
            var isTokenCharacter =
                index < task.Length
                && IsTokenCharacter(
                    task[index]
                );

            if (isTokenCharacter)
            {
                if (start < 0)
                {
                    start = index;
                }

                continue;
            }

            if (start < 0)
            {
                continue;
            }

            yield return task[
                start..index
            ];

            start = -1;
        }
    }

    private static bool IsTokenCharacter(
        char character
    )
    {
        return char.IsLetterOrDigit(
                   character
               )
               || character is '_'
                   or '.'
                   or '-';
    }

    private static bool IsUseful(
        string term
    )
    {
        if (term.Length < 2)
        {
            return false;
        }

        return !StopWords.Contains(
            term
        );
    }
}
