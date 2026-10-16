using WayFinder.DevTools.Cli.Interactive;

namespace WayFinder.DevTools.Cli.Tests.Interactive;

public sealed class TerminalInputParserTests
{
    private readonly TerminalInputParser _parser = new();

    [Fact]
    public void Parse_Enter_ReturnsEnter()
    {
        var result = _parser.Parse("\r");

        Assert.Equal(TerminalInputKind.Enter, result.Kind);
    }

    [Fact]
    public void Parse_ShiftEnter_ReturnsNewLine()
    {
        var result = _parser.Parse("\u001b[27;2;13~");

        Assert.Equal(TerminalInputKind.NewLine, result.Kind);
    }

    [Fact]
    public void Parse_MultilinePaste_PreservesContent()
    {
        var result = _parser.Parse(
            "\u001b[200~Hello\nWorld\u001b[201~"
        );

        Assert.Equal(TerminalInputKind.Paste, result.Kind);
        Assert.Equal("Hello\nWorld", result.Text);
    }

    [Fact]
    public void Parse_Character_ReturnsCharacter()
    {
        var result = _parser.Parse("a");

        Assert.Equal(TerminalInputKind.Character, result.Kind);
        Assert.Equal("a", result.Text);
    }

    [Fact]
    public void ParseStream_ShiftEnter_ProducesNewLine()
    {
        var results = _parser.ParseStream(
            "Hello\u001b[27;2;13~World"
        );

        Assert.Equal(
            11,
            results.Count
        );

        Assert.Equal(
            TerminalInputKind.NewLine,
            results[5].Kind
        );
    }

    [Fact]
    public void ParseStream_BracketedPaste_ProducesSinglePasteEvent()
    {
        var results = _parser.ParseStream(
            "\u001b[200~Hello\nWorld\u001b[201~"
        );

        var result = Assert.Single(results);

        Assert.Equal(
            TerminalInputKind.Paste,
            result.Kind
        );

        Assert.Equal(
            "Hello\nWorld",
            result.Text
        );
    }
}
