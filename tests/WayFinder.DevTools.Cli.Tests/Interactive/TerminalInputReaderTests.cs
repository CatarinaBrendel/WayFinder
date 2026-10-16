using WayFinder.DevTools.Cli.Interactive;

namespace WayFinder.DevTools.Cli.Tests.Interactive;

public sealed class TerminalInputReaderTests
{
    [Fact]
    public void Feed_Enter_ReturnsEnter()
    {
        var reader = new TerminalInputReader();

        var result = Assert.Single(reader.Feed("\n"));

        Assert.Equal(TerminalInputKind.Enter, result.Kind);
    }

    [Fact]
    public void Feed_ShiftEnter_ReturnsNewLine()
    {
        var reader = new TerminalInputReader();

        var result = Assert.Single(
            reader.Feed("\u001b[27;2;13~"));

        Assert.Equal(TerminalInputKind.NewLine, result.Kind);
    }

    [Fact]
    public void Feed_FragmentedShiftEnter_ReturnsNewLine()
    {
        var reader = new TerminalInputReader();

        Assert.Empty(reader.Feed("\u001b[27;"));
        Assert.Empty(reader.Feed("2;13"));

        var result = Assert.Single(reader.Feed("~"));

        Assert.Equal(TerminalInputKind.NewLine, result.Kind);
    }

    [Fact]
    public void Feed_FragmentedPaste_PreservesNewlines()
    {
        var reader = new TerminalInputReader();

        Assert.Empty(reader.Feed("\u001b[200~Hello"));
        Assert.Empty(reader.Feed("\nWorld\u001b[20"));

        var result = Assert.Single(reader.Feed("1~"));

        Assert.Equal(TerminalInputKind.Paste, result.Kind);
        Assert.Equal("Hello\nWorld", result.Text);
    }

    [Fact]
    public void Feed_PasteFollowedByEnter_ReturnsBothEvents()
    {
        var reader = new TerminalInputReader();

        var results = reader.Feed(
            "\u001b[200~Hello\nWorld\u001b[201~\r");

        Assert.Equal(2, results.Count);

        Assert.Equal(TerminalInputKind.Paste, results[0].Kind);
        Assert.Equal("Hello\nWorld", results[0].Text);

        Assert.Equal(TerminalInputKind.Enter, results[1].Kind);
    }

    [Theory]
    [InlineData("\u001b[C", TerminalInputKind.RightArrow)]
    [InlineData("\u001b[D", TerminalInputKind.LeftArrow)]
    [InlineData("\u001b[H", TerminalInputKind.Home)]
    [InlineData("\u001b[F", TerminalInputKind.End)]
    [InlineData("\u001b[1~", TerminalInputKind.Home)]
    [InlineData("\u001b[4~", TerminalInputKind.End)]
    [InlineData("\u001b[3~", TerminalInputKind.Delete)]
    [InlineData("\u001bOH", TerminalInputKind.Home)]
    [InlineData("\u001bOF", TerminalInputKind.End)]
    [InlineData("\t", TerminalInputKind.Tab)]
    [InlineData("\u007f", TerminalInputKind.Backspace)]
    internal void Feed_ControlSequence_ReturnsExpectedEvent(
        string sequence,
        TerminalInputKind expected)
    {
        var reader = new TerminalInputReader();

        var result = Assert.Single(reader.Feed(sequence));

        Assert.Equal(expected, result.Kind);
    }

    [Fact]
    public void Feed_FragmentedArrowSequence_ReturnsSingleEvent()
    {
        var reader = new TerminalInputReader();

        Assert.Empty(reader.Feed("\u001b["));

        var result = Assert.Single(reader.Feed("D"));

        Assert.Equal(TerminalInputKind.LeftArrow, result.Kind);
    }

    [Fact]
    public void Feed_CharactersAfterArrow_ArePreserved()
    {
        var reader = new TerminalInputReader();

        var results = reader.Feed("\u001b[Dabc");

        Assert.Equal(4, results.Count);
        Assert.Equal(TerminalInputKind.LeftArrow, results[0].Kind);

        Assert.Equal(
            "abc",
            string.Concat(results.Skip(1).Select(x => x.Text)));
    }

    [Fact]
    public void Feed_Tab_ReturnsTabEvent()
    {
        var reader = new TerminalInputReader();

        var result = Assert.Single(reader.Feed("\t"));

        Assert.Equal(TerminalInputKind.Tab, result.Kind);
    }

    [Theory]
    [InlineData("\u001b[D", (int)TerminalInputKind.LeftArrow)]
    [InlineData("\u001b[C", (int)TerminalInputKind.RightArrow)]
    public void Feed_ArrowKeys_ReturnExpectedEvents(
        string sequence,
        int expected)
    {
        var reader = new TerminalInputReader();

        var result = Assert.Single(reader.Feed(sequence));

        Assert.Equal((TerminalInputKind)expected, result.Kind);
    }

    [Fact]
    public void Feed_FragmentedArrowKey_ReturnsEvent()
    {
        var reader = new TerminalInputReader();

        Assert.Empty(reader.Feed("\u001b"));
        Assert.Empty(reader.Feed("["));

        var result = Assert.Single(reader.Feed("D"));

        Assert.Equal(TerminalInputKind.LeftArrow, result.Kind);
    }

    [Fact]
    public void Feed_ArrowKeyFollowedByCharacter_ReturnsBothEvents()
    {
        var reader = new TerminalInputReader();

        var results = reader.Feed("\u001b[Dx");

        Assert.Equal(2, results.Count);
        Assert.Equal(TerminalInputKind.LeftArrow, results[0].Kind);
        Assert.Equal(TerminalInputKind.Character, results[1].Kind);
        Assert.Equal("x", results[1].Text);
    }

    [Theory]
    [InlineData("\u001bOD", (int)TerminalInputKind.LeftArrow)]
    [InlineData("\u001bOC", (int)TerminalInputKind.RightArrow)]
    [InlineData("\u001b[1;2D", (int)TerminalInputKind.ShiftLeftArrow)]
    [InlineData("\u001b[1;2C", (int)TerminalInputKind.ShiftRightArrow)]
    public void Feed_AdditionalArrowSequences_ReturnExpectedEvents(
    string sequence,
    int expected)
    {
        var reader = new TerminalInputReader();

        var result = Assert.Single(reader.Feed(sequence));

        Assert.Equal((TerminalInputKind)expected, result.Kind);
    }

    [Fact]
    public void Feed_FragmentedApplicationArrow_ReturnsEvent()
    {
        var reader = new TerminalInputReader();

        Assert.Empty(reader.Feed("\u001b"));
        Assert.Empty(reader.Feed("O"));

        var result = Assert.Single(reader.Feed("D"));

        Assert.Equal(TerminalInputKind.LeftArrow, result.Kind);
    }

    [Fact]
    public void Feed_UnknownCsi_DoesNotLeakCharacters()
    {
        var reader = new TerminalInputReader();

        var results = reader.Feed("\u001b[1;5Ax");

        Assert.Equal(2, results.Count);
        Assert.Equal(TerminalInputKind.Unknown, results[0].Kind);
        Assert.Equal(TerminalInputKind.Character, results[1].Kind);
        Assert.Equal("x", results[1].Text);
    }
}
