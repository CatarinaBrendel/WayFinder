using WayFinder.DevTools.Cli.Interactive;

namespace WayFinder.DevTools.Tests.Cli;

public sealed class InteractiveCommandParserTests
{
    [Theory]
    [InlineData("/project list", "/project", "list")]
    [InlineData("/project add /tmp/MyProject", "/project", "add", "/tmp/MyProject")]
    [InlineData("/project add \"/tmp/My Project\"", "/project", "add", "/tmp/My Project")]
    [InlineData("/project add '/tmp/My Project'", "/project", "add", "/tmp/My Project")]
    [InlineData("  /project   list  ", "/project", "list")]
    public void Parse_ValidInput_ReturnsExpectedArguments(
        string input,
        params string[] expected)
    {
        var result = InteractiveCommandParser.Parse(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyCollection()
    {
        var result = InteractiveCommandParser.Parse("");

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsEmptyCollection()
    {
        var result = InteractiveCommandParser.Parse("   ");

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_EmptyQuotedArgument_PreservesArgument()
    {
        var result = InteractiveCommandParser.Parse(
            "/project add \"\""
        );

        Assert.Equal(
            ["/project", "add", ""],
            result
        );
    }

    [Fact]
    public void Parse_UnterminatedQuote_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(
            () => InteractiveCommandParser.Parse(
                "/project add \"unfinished"
            )
        );
    }

    [Fact]
    public void Parse_WindowsPath_PreservesBackslashes()
    {
        var result = InteractiveCommandParser.Parse(
            @"/project add C:\Projects\MyApp"
        );

        Assert.Equal(
            ["/project", "add", @"C:\Projects\MyApp"],
            result
        );
    }

    [Fact]
    public void Parse_QuotedWindowsPath_PreservesBackslashes()
    {
        var result = InteractiveCommandParser.Parse(
            "/project add \"C:\\My Projects\\Test\""
        );

        Assert.Equal(
            ["/project", "add", @"C:\My Projects\Test"],
            result
        );
    }

    [Fact]
    public void Parse_MultipleQuotedArguments_ReturnsCorrectArguments()
    {
        var result = InteractiveCommandParser.Parse(
            "/example \"first argument\" 'second argument'"
        );

        Assert.Equal(
            ["/example", "first argument", "second argument"],
            result
        );
    }

    [Fact]
    public void Parse_QuotedPathWithSpaces_PreservesEntirePath()
    {
        var result = InteractiveCommandParser.Parse(
            "/project add \"/Users/test/My Projects/Example Project\""
        );

        Assert.Equal(
            [
                "/project",
            "add",
            "/Users/test/My Projects/Example Project"
            ],
            result
        );
    }
}
