using System.Text;

namespace WayFinder.DevTools.Cli.Interactive;

internal sealed class TerminalInputReader
{
    private const string PasteStart = "\u001b[200~";
    private const string PasteEnd = "\u001b[201~";

    private readonly StringBuilder _pending = new();
    private readonly StringBuilder _paste = new();

    private bool _isPasting;

    public IReadOnlyList<TerminalInput> Feed(string input)
    {
        _pending.Append(input);

        var results = new List<TerminalInput>();

        while (_pending.Length > 0)
        {
            var text = _pending.ToString();

            if (_isPasting)
            {
                var endIndex = text.IndexOf(
                    PasteEnd,
                    StringComparison.Ordinal);

                if (endIndex >= 0)
                {
                    _paste.Append(text.AsSpan(0, endIndex));

                    results.Add(new TerminalInput(
                        TerminalInputKind.Paste,
                        _paste.ToString()));

                    _paste.Clear();
                    _isPasting = false;

                    _pending.Remove(
                        0,
                        endIndex + PasteEnd.Length);

                    continue;
                }

                // Keep a possible partial closing marker.
                var retained = LongestSuffixMatchingPrefix(
                    text,
                    PasteEnd);

                var consumed = text.Length - retained;

                _paste.Append(text.AsSpan(0, consumed));
                _pending.Remove(0, consumed);

                break;
            }

            if (text[0] == '\u001b')
            {
                // An escape sequence may arrive across multiple reads.
                if (text.Length == 1)
                    break;

                // Bracketed paste.
                if (PasteStart.StartsWith(text, StringComparison.Ordinal))
                    break;

                if (text.StartsWith(PasteStart, StringComparison.Ordinal))
                {
                    _pending.Remove(0, PasteStart.Length);
                    _isPasting = true;
                    continue;
                }

                // CSI sequences: ESC [ ...
                if (text[1] == '[')
                {
                    var endIndex = -1;

                    for (var i = 2; i < text.Length; i++)
                    {
                        // CSI final bytes range from 0x40 to 0x7E.
                        if (text[i] is >= '@' and <= '~')
                        {
                            endIndex = i;
                            break;
                        }
                    }

                    if (endIndex < 0)
                        break;

                    var sequence = text[..(endIndex + 1)];

                    var kind = sequence switch
                    {
                        // Ordinary navigation.
                        "\u001b[A" => TerminalInputKind.UpArrow,
                        "\u001b[B" => TerminalInputKind.DownArrow,
                        "\u001b[C" => TerminalInputKind.RightArrow,
                        "\u001b[D" => TerminalInputKind.LeftArrow,

                        // Shift-modified navigation.
                        "\u001b[1;2A" => TerminalInputKind.ShiftUpArrow,
                        "\u001b[1;2B" => TerminalInputKind.ShiftDownArrow,
                        "\u001b[1;2C" => TerminalInputKind.ShiftRightArrow,
                        "\u001b[1;2D" => TerminalInputKind.ShiftLeftArrow,

                        // Home, End, Delete.
                        "\u001b[H" => TerminalInputKind.Home,
                        "\u001b[F" => TerminalInputKind.End,
                        "\u001b[1~" => TerminalInputKind.Home,
                        "\u001b[4~" => TerminalInputKind.End,
                        "\u001b[3~" => TerminalInputKind.Delete,

                        // Shift+Enter.
                        "\u001b[27;2;13~" => TerminalInputKind.NewLine,

                        _ => TerminalInputKind.Unknown
                    };

                    results.Add(new TerminalInput(kind));
                    _pending.Remove(0, sequence.Length);
                    continue;
                }

                // SS3 sequences: ESC O ...
                if (text[1] == 'O')
                {
                    if (text.Length < 3)
                        break;

                    var kind = text[2] switch
                    {
                        'C' => TerminalInputKind.RightArrow,
                        'D' => TerminalInputKind.LeftArrow,
                        'A' => TerminalInputKind.UpArrow,
                        'B' => TerminalInputKind.DownArrow,
                        'H' => TerminalInputKind.Home,
                        'F' => TerminalInputKind.End,
                        _ => TerminalInputKind.Unknown
                    };

                    results.Add(new TerminalInput(kind));
                    _pending.Remove(0, 3);
                    continue;
                }

                // Unsupported ESC-prefixed sequence.
                // Consume the prefix rather than inserting escape bytes as text.
                results.Add(new TerminalInput(TerminalInputKind.Unknown));
                _pending.Remove(0, 2);
                continue;
            }

            var character = text[0];

            switch (character)
            {
                case '\r':
                case '\n':
                    results.Add(new TerminalInput(TerminalInputKind.Enter));
                    break;

                case '\t':
                    results.Add(new TerminalInput(TerminalInputKind.Tab));
                    break;

                case '\b':
                case '\u007f':
                    results.Add(new TerminalInput(TerminalInputKind.Backspace));
                    break;

                case '\u001b':
                    results.Add(new TerminalInput(TerminalInputKind.Escape));
                    break;

                default:
                    if (!char.IsControl(character))
                    {
                        results.Add(new TerminalInput(
                            TerminalInputKind.Character,
                            character.ToString()));
                    }
                    break;
            }

            _pending.Remove(0, 1);
        }

        return results;
    }

    private static int LongestSuffixMatchingPrefix(
        string text,
        string marker)
    {
        var maximum = Math.Min(
            text.Length,
            marker.Length - 1);

        for (var length = maximum; length > 0; length--)
        {
            if (text.AsSpan(text.Length - length)
                .SequenceEqual(marker.AsSpan(0, length)))
            {
                return length;
            }
        }

        return 0;
    }

}
