using System.Text;
using WayFinder.DevTools.Cli.Presentation;

namespace WayFinder.DevTools.Cli.Interactive;

internal sealed class InteractiveLineEditor
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly InteractiveCommandCompleter _completer;
    private int _promptWidth;
    private int _cursorPosition;
    private int _horizontalOffset;

    private int _suggestionLineCount;

    public InteractiveLineEditor(
        TextReader input,
        TextWriter output,
        InteractiveCommandCompleter completer)
    {
        _input = input;
        _output = output;
        _completer = completer;
    }

    public async Task<string?> ReadLineAsync(
        CancellationToken cancellationToken = default)
    {
        if (!ReferenceEquals(_input, Console.In)
            || !ReferenceEquals(_output, Console.Out)
            || Console.IsInputRedirected
            || Console.IsOutputRedirected)
        {
            return await _input.ReadLineAsync(cancellationToken);
        }

        return ReadConsoleLine(cancellationToken);
    }

    private string ReadConsoleLine(
        CancellationToken cancellationToken)
    {
        var buffer = new StringBuilder();
        _promptWidth = Console.CursorLeft;
        _cursorPosition = 0;
        _horizontalOffset = 0;
        _suggestionLineCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    ClearSuggestions();
                    _output.WriteLine();
                    return buffer.ToString();

                case ConsoleKey.Backspace:
                    ClearSuggestions();

                    if (_cursorPosition > 0)
                    {
                        buffer.Remove(_cursorPosition - 1, 1);
                        _cursorPosition--;

                        RedrawInput(buffer);
                    }

                    break;

                case ConsoleKey.LeftArrow:
                    ClearSuggestions();

                    if (_cursorPosition > 0)
                    {
                        _cursorPosition--;
                        RedrawInput(buffer);
                    }

                    break;

                case ConsoleKey.RightArrow:
                    ClearSuggestions();

                    if (_cursorPosition < buffer.Length)
                    {
                        _cursorPosition++;
                        RedrawInput(buffer);
                    }

                    break;

                case ConsoleKey.Tab:
                    Complete(buffer);
                    break;

                case ConsoleKey.Home:
                    ClearSuggestions();

                    _cursorPosition = 0;
                    RedrawInput(buffer);
                    break;

                case ConsoleKey.End:
                    ClearSuggestions();

                    _cursorPosition = buffer.Length;
                    RedrawInput(buffer);
                    break;

                case ConsoleKey.Delete:
                    ClearSuggestions();

                    if (_cursorPosition < buffer.Length)
                    {
                        buffer.Remove(_cursorPosition, 1);
                        RedrawInput(buffer);
                    }

                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        ClearSuggestions();

                        buffer.Insert(_cursorPosition, key.KeyChar);
                        _cursorPosition++;

                        RedrawInput(buffer);
                    }

                    break;
            }

            _output.Flush();
        }
    }

    private void Complete(StringBuilder buffer)
    {
        ClearSuggestions();

        var matches = _completer.Complete(
            buffer.ToString()
        );

        if (matches.Count == 0)
        {
            return;
        }

        if (matches.Count == 1)
        {
            buffer.Clear();
            buffer.Append(matches[0]);

            _cursorPosition = buffer.Length;
            _horizontalOffset = 0;

            RedrawInput(buffer);
            return;
        }

        ShowSuggestions(matches, buffer);
    }

    private void ShowSuggestions(
        IReadOnlyList<string> matches,
        StringBuilder buffer)
    {
        // Reserve enough space for the completion menu.
        var requiredLines = matches.Count + 1;

        var availableLines =
            Console.WindowHeight - Console.CursorTop - 1;

        if (availableLines < requiredLines)
        {
            var missingLines = requiredLines - availableLines;

            // Move the prompt upward by scrolling the terminal.
            for (var i = 0; i < missingLines; i++)
            {
                _output.WriteLine();
            }

            Console.SetCursorPosition(
                0,
                Math.Max(0, Console.CursorTop - missingLines)
            );

            RedrawInput(buffer);
        }

        _output.WriteLine();

        foreach (var match in matches)
        {
            var lastSeparatorIndex = match.LastIndexOf(' ');

            var display = lastSeparatorIndex >= 0
                ? match[(lastSeparatorIndex + 1)..]
                : match;

            ConsolePresenter.Item(_output, display);
        }

        _suggestionLineCount = matches.Count;

        _output.Write(
            $"\u001b[{_suggestionLineCount + 1}A"
        );

        RedrawInput(buffer);
    }

    private void ClearSuggestions()
    {
        if (_suggestionLineCount == 0)
        {
            return;
        }

        // Move below the input line.
        _output.Write("\u001b[1B");

        for (var i = 0; i < _suggestionLineCount; i++)
        {
            // Clear the current suggestion line.
            _output.Write("\r\u001b[2K");

            if (i < _suggestionLineCount - 1)
            {
                _output.Write("\u001b[1B");
            }
        }

        // Return to the input line.
        _output.Write(
            $"\u001b[{_suggestionLineCount}A"
        );

        _suggestionLineCount = 0;
    }

    private void RedrawInput(StringBuilder buffer)
    {
        var terminalWidth = Console.WindowWidth;

        var availableWidth = Math.Max(
            1,
            terminalWidth - _promptWidth - 1
        );

        // Keep the cursor inside the visible window.
        if (_cursorPosition < _horizontalOffset)
        {
            _horizontalOffset = _cursorPosition;
        }
        else if (_cursorPosition > _horizontalOffset + availableWidth)
        {
            _horizontalOffset = _cursorPosition - availableWidth;
        }

        var text = buffer.ToString();

        var visibleLength = Math.Min(
            availableWidth,
            text.Length - _horizontalOffset
        );

        var visibleText = text.Substring(
            _horizontalOffset,
            visibleLength
        );

        _output.Write("\r\u001b[2K");

        InteractivePresentation.WritePrompt(_output);

        _output.Write(visibleText);

        // Position the terminal cursor at the logical insertion point.
        var cursorColumn =
            _promptWidth + _cursorPosition - _horizontalOffset;

        Console.SetCursorPosition(
            Math.Min(cursorColumn, terminalWidth - 1),
            Console.CursorTop
        );

        _output.Flush();
    }

    public async Task<bool> ReadConfirmationAsync(
        CancellationToken cancellationToken = default)
    {
        if (!ReferenceEquals(_input, Console.In)
            || !ReferenceEquals(_output, Console.Out)
            || Console.IsInputRedirected
            || Console.IsOutputRedirected)
        {
            var answer = await _input.ReadLineAsync(cancellationToken);

            return string.Equals(
                answer?.Trim(),
                "y",
                StringComparison.OrdinalIgnoreCase);
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Y:
                    return true;

                case ConsoleKey.N:
                case ConsoleKey.Escape:
                    return false;
            }
        }
    }
}
