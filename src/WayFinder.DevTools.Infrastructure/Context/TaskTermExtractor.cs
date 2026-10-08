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
            .Select(Normalize)
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

    private static string Normalize(
        string term
    )
    {
        if (term.Length < 3)
        {
            return term;
        }

        var normalized =
            new char[term.Length];

        var length = 0;

        for (var index = 0;
             index < term.Length;
             index++)
        {
            var character =
                term[index];

            if (character is '-' or '_'
                && index > 0
                && index + 1 < term.Length
                && char.IsLetter(term[index - 1])
                && char.IsDigit(term[index + 1]))
            {
                continue;
            }

            normalized[length++] =
                character;
        }

        return new string(
            normalized,
            0,
            length
        );
    }
}
