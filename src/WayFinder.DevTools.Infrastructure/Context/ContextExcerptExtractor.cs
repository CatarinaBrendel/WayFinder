namespace WayFinder.DevTools.Infrastructure.Context;

internal sealed class ContextExcerptExtractor
{
    public ExtractedContextExcerpt Extract(
        string content,
        ContextExcerpt excerpt
    )
    {
        ArgumentNullException.ThrowIfNull(
            content
        );

        ArgumentNullException.ThrowIfNull(
            excerpt
        );

        if (excerpt.StartLine <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(excerpt)
            );
        }

        if (excerpt.EndLine < excerpt.StartLine)
        {
            throw new ArgumentOutOfRangeException(
                nameof(excerpt)
            );
        }

        var lines =
            GetLines(
                content
            );

        if (excerpt.StartLine > lines.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(excerpt)
            );
        }

        var endLine =
            Math.Min(
                excerpt.EndLine,
                lines.Length
            );

        var extracted =
            lines[
                (excerpt.StartLine - 1)..endLine
            ];

        return new ExtractedContextExcerpt(
            excerpt.StartLine,
            endLine,
            string.Join(
                '\n',
                extracted
            )
        );
    }

    private static string NormalizeLineEndings(
        string content
    )
    {
        return content
            .Replace(
                "\r\n",
                "\n",
                StringComparison.Ordinal
            )
            .Replace(
                '\r',
                '\n'
            );
    }

    private static string[] GetLines(
    string content
)
    {
        var normalized =
            content
                .Replace(
                    "\r\n",
                    "\n",
                    StringComparison.Ordinal
                )
                .Replace(
                    '\r',
                    '\n'
                );

        if (normalized.EndsWith(
                '\n'))
        {
            normalized =
                normalized[..^1];
        }

        return normalized.Split(
            '\n'
        );
    }
}
