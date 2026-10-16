namespace WayFinder.DevTools.Cli.Interactive;

internal enum TerminalInputKind
{
    Character,
    Enter,
    NewLine,
    Paste,
    Unknown,

    Backspace,
    Delete,
    LeftArrow,
    RightArrow,
    UpArrow,
    DownArrow,
    ShiftLeftArrow,
    ShiftRightArrow,
    ShiftUpArrow,
    ShiftDownArrow,
    Home,
    End,
    Tab,
    Escape
}

internal readonly record struct TerminalInput(
    TerminalInputKind Kind,
    string Text = ""
);

internal sealed class TerminalInputParser
{
    private const string ShiftEnterSequence = "\u001b[27;2;13~";
    private const string PasteStartSequence = "\u001b[200~";
    private const string PasteEndSequence = "\u001b[201~";

    public TerminalInput Parse(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new TerminalInput(TerminalInputKind.Unknown);
        }

        if (input == ShiftEnterSequence)
        {
            return new TerminalInput(TerminalInputKind.NewLine);
        }

        if (input == "\r" || input == "\n")
        {
            return new TerminalInput(TerminalInputKind.Enter);
        }

        if (input.StartsWith(PasteStartSequence, StringComparison.Ordinal)
            && input.EndsWith(PasteEndSequence, StringComparison.Ordinal))
        {
            var content = input[
                PasteStartSequence.Length..^PasteEndSequence.Length
            ];

            return new TerminalInput(
                TerminalInputKind.Paste,
                content
            );
        }

        if (input.Length == 1 && !char.IsControl(input[0]))
        {
            return new TerminalInput(
                TerminalInputKind.Character,
                input
            );
        }

        return new TerminalInput(TerminalInputKind.Unknown);
    }

    public IReadOnlyList<TerminalInput> ParseStream(
        string input)
    {
        var results = new List<TerminalInput>();

        var index = 0;

        while (index < input.Length)
        {
            if (input.AsSpan(index).StartsWith(
                    "\u001b[200~",
                    StringComparison.Ordinal))
            {
                var contentStart = index + 6;

                var contentEnd = input.IndexOf(
                    "\u001b[201~",
                    contentStart,
                    StringComparison.Ordinal
                );

                if (contentEnd < 0)
                {
                    break;
                }

                results.Add(new TerminalInput(
                    TerminalInputKind.Paste,
                    input[contentStart..contentEnd]
                ));

                index = contentEnd + 6;
                continue;
            }

            if (input.AsSpan(index).StartsWith(
                    "\u001b[27;2;13~",
                    StringComparison.Ordinal))
            {
                results.Add(new TerminalInput(
                    TerminalInputKind.NewLine
                ));

                index += 10;
                continue;
            }

            var character = input[index];

            if (character is '\r' or '\n')
            {
                results.Add(new TerminalInput(
                    TerminalInputKind.Enter
                ));
            }
            else if (!char.IsControl(character))
            {
                results.Add(new TerminalInput(
                    TerminalInputKind.Character,
                    character.ToString()
                ));
            }

            index++;
        }

        return results;
    }
}
