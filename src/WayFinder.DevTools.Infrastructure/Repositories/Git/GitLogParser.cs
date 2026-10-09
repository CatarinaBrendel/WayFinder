using System.Globalization;
using WayFinder.DevTools.Application.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Repositories.Git;

internal sealed class GitLogParser
{
    public IReadOnlyList<GitCommit> Parse(string output)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (output.Length == 0)
        {
            return [];
        }

        var commits = new List<GitCommit>();
        var position = 0;

        while (position < output.Length)
        {
            var hash = ReadField(output, ref position);
            var authorName = ReadField(output, ref position);
            var authorEmail = ReadField(output, ref position);
            var authoredAt = ReadField(output, ref position);
            var subject = ReadField(output, ref position);

            ValidateHash(hash);

            if (!DateTimeOffset.TryParseExact(
                    authoredAt,
                    "yyyy-MM-dd'T'HH:mm:sszzz",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var timestamp))
            {
                throw new FormatException(
                    $"Invalid Git author timestamp: '{authoredAt}'."
                );
            }

            commits.Add(
                new GitCommit(
                    Hash: hash,
                    AuthorName: authorName,
                    AuthorEmail: authorEmail,
                    AuthoredAt: timestamp,
                    Subject: subject
                )
            );

            // Git appends a newline after each formatted commit.
            if (position < output.Length)
            {
                if (output[position] == '\n')
                {
                    position++;
                }
                else
                {
                    throw new FormatException(
                        "Invalid Git log record separator."
                    );
                }
            }
        }

        return commits;
    }

    private static string ReadField(
        string output,
        ref int position
    )
    {
        var end = output.IndexOf('\0', position);

        if (end < 0)
        {
            throw new FormatException(
                "Incomplete Git log record."
            );
        }

        var value = output[position..end];

        position = end + 1;

        return value;
    }

    private static void ValidateHash(string hash)
    {
        // Git supports SHA-1 and SHA-256 repositories.
        if (hash.Length is not (40 or 64))
        {
            throw new FormatException(
                "Invalid Git commit hash length."
            );
        }

        foreach (var character in hash)
        {
            if (!Uri.IsHexDigit(character))
            {
                throw new FormatException(
                    "Invalid Git commit hash."
                );
            }
        }
    }
}
