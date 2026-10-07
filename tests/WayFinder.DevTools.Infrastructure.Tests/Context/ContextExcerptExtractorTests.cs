using WayFinder.DevTools.Infrastructure.Context;

namespace WayFinder.DevTools.Infrastructure.Tests.Context;

public sealed class ContextExcerptExtractorTests
{
    private readonly ContextExcerptExtractor _extractor =
        new();

    [Fact]
    public void Extract_ReturnsRequestedLines()
    {
        var content =
            """
            one
            two
            three
            four
            five
            """;

        var result =
            _extractor.Extract(
                content,
                new ContextExcerpt(
                    2,
                    4,
                    ["term"]
                )
            );

        Assert.Equal(
            2,
            result.StartLine
        );

        Assert.Equal(
            4,
            result.EndLine
        );

        Assert.Equal(
            """
            two
            three
            four
            """,
            result.Content
        );
    }

    [Fact]
    public void Extract_ClampsEndToFileLength()
    {
        var content =
            """
            one
            two
            three
            """;

        var result =
            _extractor.Extract(
                content,
                new ContextExcerpt(
                    2,
                    100,
                    ["term"]
                )
            );

        Assert.Equal(
            2,
            result.StartLine
        );

        Assert.Equal(
            3,
            result.EndLine
        );

        Assert.Equal(
            """
            two
            three
            """,
            result.Content
        );
    }

    [Fact]
    public void Extract_NormalizesWindowsLineEndings()
    {
        const string content =
            "one\r\ntwo\r\nthree\r\n";

        var result =
            _extractor.Extract(
                content,
                new ContextExcerpt(
                    1,
                    3,
                    ["term"]
                )
            );

        Assert.Equal(
            "one\ntwo\nthree",
            result.Content
        );
    }

    [Fact]
    public void Extract_NormalizesLegacyMacLineEndings()
    {
        const string content =
            "one\rtwo\rthree\r";

        var result =
            _extractor.Extract(
                content,
                new ContextExcerpt(
                    2,
                    3,
                    ["term"]
                )
            );

        Assert.Equal(
            "two\nthree",
            result.Content
        );
    }

    [Fact]
    public void Extract_RejectsStartBeyondFile()
    {
        const string content =
            """
            one
            two
            """;

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                _extractor.Extract(
                    content,
                    new ContextExcerpt(
                        3,
                        4,
                        ["term"]
                    )
                )
        );
    }
}
