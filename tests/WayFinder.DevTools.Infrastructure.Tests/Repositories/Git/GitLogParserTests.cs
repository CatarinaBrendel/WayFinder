using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitLogParserTests
{
    private readonly GitLogParser _parser = new();

    private const string Hash1 =
        "0123456789abcdef0123456789abcdef01234567";

    private const string Hash2 =
        "abcdef0123456789abcdef0123456789abcdef01";

    private const string Sha256Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Parse_EmptyOutput_ReturnsEmptyCollection()
    {
        var result = _parser.Parse(string.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_NullOutput_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => _parser.Parse(null!)
        );
    }

    [Fact]
    public void Parse_SingleCommit_ReturnsMetadata()
    {
        var output = Record(
            Hash1,
            "Catarina Müller",
            "catarina@example.com",
            "2026-10-09T08:30:00+02:00",
            "Initial commit"
        );

        var result = _parser.Parse(output);

        var commit = Assert.Single(result);

        Assert.Equal(Hash1, commit.Hash);
        Assert.Equal("Catarina Müller", commit.AuthorName);
        Assert.Equal("catarina@example.com", commit.AuthorEmail);
        Assert.Equal("Initial commit", commit.Subject);

        Assert.Equal(
            new DateTimeOffset(
                2026, 10, 9, 8, 30, 0,
                TimeSpan.FromHours(2)
            ),
            commit.AuthoredAt
        );

        Assert.Equal(
            TimeSpan.FromHours(2),
            commit.AuthoredAt.Offset
        );
    }

    [Fact]
    public void Parse_MultipleCommits_PreservesOrder()
    {
        var output =
            Record(
                Hash2,
                "Second Author",
                "second@example.com",
                "2026-10-09T09:00:00+02:00",
                "Second commit"
            )
            +
            Record(
                Hash1,
                "First Author",
                "first@example.com",
                "2026-10-08T09:00:00+02:00",
                "First commit"
            );

        var result = _parser.Parse(output);

        Assert.Equal(2, result.Count);

        Assert.Equal(Hash2, result[0].Hash);
        Assert.Equal("Second commit", result[0].Subject);

        Assert.Equal(Hash1, result[1].Hash);
        Assert.Equal("First commit", result[1].Subject);
    }

    [Fact]
    public void Parse_UnicodeMetadata_IsPreserved()
    {
        var output = Record(
            Hash1,
            "Renée García",
            "renee@example.com",
            "2026-10-09T08:30:00+02:00",
            "Änderung für café 🚀"
        );

        var commit = Assert.Single(_parser.Parse(output));

        Assert.Equal("Renée García", commit.AuthorName);
        Assert.Equal("Änderung für café 🚀", commit.Subject);
    }

    [Fact]
    public void Parse_Sha256Hash_IsAccepted()
    {
        var output = Record(
            Sha256Hash,
            "Author",
            "author@example.com",
            "2026-10-09T08:30:00+02:00",
            "SHA-256 commit"
        );

        var commit = Assert.Single(_parser.Parse(output));

        Assert.Equal(Sha256Hash, commit.Hash);
    }

    [Fact]
    public void Parse_NegativeTimezoneOffset_IsPreserved()
    {
        var output = Record(
            Hash1,
            "Author",
            "author@example.com",
            "2026-10-09T08:30:00-04:00",
            "Timezone test"
        );

        var commit = Assert.Single(_parser.Parse(output));

        Assert.Equal(
            TimeSpan.FromHours(-4),
            commit.AuthoredAt.Offset
        );
    }

    [Fact]
    public void Parse_NewlineInAuthorName_IsPreserved()
    {
        var output = Record(
            Hash1,
            "First Line\nSecond Line",
            "author@example.com",
            "2026-10-09T08:30:00+02:00",
            "Author name test"
        );

        var commit = Assert.Single(_parser.Parse(output));

        Assert.Equal(
            "First Line\nSecond Line",
            commit.AuthorName
        );
    }

    [Fact]
    public void Parse_IncompleteRecord_Throws()
    {
        var output = $"{Hash1}\0Author\0";

        Assert.Throws<FormatException>(
            () => _parser.Parse(output)
        );
    }

    [Fact]
    public void Parse_InvalidHashLength_Throws()
    {
        var output = Record(
            "abc123",
            "Author",
            "author@example.com",
            "2026-10-09T08:30:00+02:00",
            "Invalid hash"
        );

        Assert.Throws<FormatException>(
            () => _parser.Parse(output)
        );
    }

    [Fact]
    public void Parse_NonHexadecimalHash_Throws()
    {
        var output = Record(
            new string('z', 40),
            "Author",
            "author@example.com",
            "2026-10-09T08:30:00+02:00",
            "Invalid hash"
        );

        Assert.Throws<FormatException>(
            () => _parser.Parse(output)
        );
    }

    [Fact]
    public void Parse_InvalidTimestamp_Throws()
    {
        var output = Record(
            Hash1,
            "Author",
            "author@example.com",
            "not-a-timestamp",
            "Invalid timestamp"
        );

        Assert.Throws<FormatException>(
            () => _parser.Parse(output)
        );
    }

    [Fact]
    public void Parse_InvalidRecordSeparator_Throws()
    {
        var output = Record(
            Hash1,
            "Author",
            "author@example.com",
            "2026-10-09T08:30:00+02:00",
            "First commit"
        );

        // Replace the expected newline separator.
        output = output[..^1] + "X";

        Assert.Throws<FormatException>(
            () => _parser.Parse(output)
        );
    }

    private static string Record(
        string hash,
        string authorName,
        string authorEmail,
        string authoredAt,
        string subject
    )
    {
        return string.Join(
            '\0',
            hash,
            authorName,
            authorEmail,
            authoredAt,
            subject
        ) + "\0\n";
    }
}
