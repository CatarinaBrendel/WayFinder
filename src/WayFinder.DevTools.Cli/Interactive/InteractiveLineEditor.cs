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
    private int _renderedRowCount = 1;
    private int _renderedCursorRow;
    private int? _selectionAnchor;
    private const int ContinuationIndent = 2;

    private bool HasSelection =>
    _selectionAnchor.HasValue &&
    _selectionAnchor.Value != _cursorPosition;

    private void ClearSelection()
    {
        _selectionAnchor = null;
    }

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
        _renderedRowCount = 1;
        _renderedCursorRow = 0;
        _selectionAnchor = null;

        using var terminalMode = new TerminalRawMode();
        File.AppendAllText(
            "/tmp/wayfinder-input.log",
            "TERMINAL RAW MODE ENABLED\n");

        using var input = new FileStream(
            "/dev/tty",
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        var reader = new TerminalInputReader();
        var decoder = Encoding.UTF8.GetDecoder();
        var bytes = new byte[256];
        var characters = new char[256];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var count = input.Read(bytes, 0, bytes.Length);

            if (count == 0)
                return buffer.ToString();

            var characterCount = decoder.GetChars(
                bytes,
                0,
                count,
                characters,
                0,
                flush: false);

            var events = reader.Feed(
                new string(characters, 0, characterCount));
            foreach (var terminalEvent in events)
            {
                File.AppendAllText(
                    "/tmp/wayfinder-input.log",
                    $"EVENT: {terminalEvent.Kind} [{terminalEvent.Text}]\n");
            }

            foreach (var terminalInput in events)
            {
                switch (terminalInput.Kind)
                {
                    case TerminalInputKind.Enter:
                        ClearSuggestions();
                        MoveToEndOfInput();
                        _output.WriteLine();
                        return buffer.ToString();

                    case TerminalInputKind.NewLine:
                        ClearSuggestions();
                        InsertText(buffer, "\n");
                        break;

                    case TerminalInputKind.Paste:
                        ClearSuggestions();
                        InsertText(
                            buffer,
                            terminalInput.Text.Replace("\r\n", "\n")
                                              .Replace('\r', '\n'));
                        break;

                    case TerminalInputKind.Character:
                        ClearSuggestions();
                        InsertText(buffer, terminalInput.Text);
                        break;

                    case TerminalInputKind.Backspace:
                        ClearSuggestions();

                        if (DeleteSelection(buffer))
                        {
                            RedrawInput(buffer);
                        }
                        else if (_cursorPosition > 0)
                        {
                            buffer.Remove(_cursorPosition - 1, 1);
                            _cursorPosition--;
                            RedrawInput(buffer);
                        }

                        break;

                    case TerminalInputKind.Delete:
                        ClearSuggestions();

                        if (DeleteSelection(buffer))
                        {
                            RedrawInput(buffer);
                        }
                        else if (_cursorPosition < buffer.Length)
                        {
                            buffer.Remove(_cursorPosition, 1);
                            RedrawInput(buffer);
                        }

                        break;

                    case TerminalInputKind.LeftArrow:
                        ClearSuggestions();

                        if (HasSelection)
                        {
                            _cursorPosition = Math.Min(
                                _selectionAnchor!.Value,
                                _cursorPosition);
                        }
                        else if (_cursorPosition > 0)
                        {
                            _cursorPosition--;
                        }

                        ClearSelection();
                        RedrawInput(buffer);
                        break;

                    case TerminalInputKind.RightArrow:
                        ClearSuggestions();

                        if (HasSelection)
                        {
                            _cursorPosition = Math.Max(
                                _selectionAnchor!.Value,
                                _cursorPosition);
                        }
                        else if (_cursorPosition < buffer.Length)
                        {
                            _cursorPosition++;
                        }

                        ClearSelection();
                        RedrawInput(buffer);
                        break;

                    case TerminalInputKind.Home:
                        ClearSuggestions();
                        ClearSelection();
                        _cursorPosition = 0;
                        RedrawInput(buffer);
                        break;

                    case TerminalInputKind.End:
                        ClearSuggestions();
                        ClearSelection();
                        _cursorPosition = buffer.Length;
                        RedrawInput(buffer);
                        break;

                    case TerminalInputKind.Tab:
                        if (!buffer.ToString().Contains('\n'))
                        {
                            Complete(buffer);
                        }

                        break;

                    case TerminalInputKind.UpArrow:
                        ClearSuggestions();
                        ClearSelection();
                        MoveCursorVertically(buffer, -1);
                        break;

                    case TerminalInputKind.DownArrow:
                        ClearSuggestions();
                        ClearSelection();
                        MoveCursorVertically(buffer, 1);
                        break;

                    case TerminalInputKind.ShiftLeftArrow:
                    case TerminalInputKind.ShiftRightArrow:
                    case TerminalInputKind.ShiftUpArrow:
                    case TerminalInputKind.ShiftDownArrow:
                        ClearSuggestions();
                        ExtendSelection(buffer, terminalInput.Kind);
                        break;
                }
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
            ClearSelection();

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

    private void RedrawSingleLineInput(StringBuilder buffer)
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

    private void RedrawInput(StringBuilder buffer)
    {
        var text = buffer.ToString();
        var width = Math.Max(1, Console.WindowWidth);

        // Move from the current cursor row to the first input row.
        if (_renderedCursorRow > 0)
        {
            _output.Write($"\u001b[{_renderedCursorRow}A");
        }

        _output.Write("\r");

        // Clear the previously rendered input.
        for (var i = 0; i < _renderedRowCount; i++)
        {
            _output.Write("\u001b[2K");

            if (i < _renderedRowCount - 1)
            {
                _output.Write("\u001b[1B\r");
            }
        }

        // Return to the first input row.
        if (_renderedRowCount > 1)
        {
            _output.Write($"\u001b[{_renderedRowCount - 1}A");
        }

        _output.Write("\r");

        // Render the prompt.
        InteractivePresentation.WritePrompt(_output);

        var row = 0;
        var column = _promptWidth;

        var cursorRow = 0;
        var cursorColumn = _promptWidth;

        for (var i = 0; i < text.Length; i++)
        {
            if (i == _cursorPosition)
            {
                cursorRow = row;
                cursorColumn = column;
            }

            if (text[i] == '\n')
            {
                _output.Write("\r\n");
                row++;
                column = 0;

                var indent = Math.Min(ContinuationIndent, Math.Max(0, width - 2));

                if (indent > 0)
                {
                    _output.Write($"\u001b[{indent}C");
                }

                column = indent;

                continue;
            }

            // Avoid writing into the last column, where terminal
            // auto-wrapping behavior can vary.
            if (column >= width - 1)
            {
                _output.Write("\r\n");
                row++;
                column = 0;

                var indent = Math.Min(ContinuationIndent, Math.Max(0, width - 2));

                if (indent > 0)
                {
                    _output.Write($"\u001b[{indent}C");
                }

                column = indent;
            }

            var isSelected = _selectionAnchor.HasValue
                && i >= Math.Min(_selectionAnchor.Value, _cursorPosition)
                && i < Math.Max(_selectionAnchor.Value, _cursorPosition);

            if (isSelected)
            {
                _output.Write("\u001b[7m");
            }

            _output.Write(text[i]);

            if (isSelected)
            {
                _output.Write("\u001b[27m");
            }

            column++;
        }

        if (_cursorPosition == text.Length)
        {
            cursorRow = row;
            cursorColumn = column;
        }

        _renderedRowCount = row + 1;
        _renderedCursorRow = cursorRow;

        // Move from the final rendered row to the logical cursor row.
        var rowsUp = row - cursorRow;

        if (rowsUp > 0)
        {
            _output.Write($"\u001b[{rowsUp}A");
        }

        // Return to the beginning of the cursor's visual row.
        _output.Write("\r");

        // Position the cursor at the calculated visual column.
        _output.Write($"\u001b[{cursorColumn + 1}G");

        _output.Flush();
    }

    private void InsertText(
    StringBuilder buffer,
    string text)
    {
        if (HasSelection)
        {
            var start = Math.Min(
                _selectionAnchor!.Value,
                _cursorPosition);

            var end = Math.Max(
                _selectionAnchor.Value,
                _cursorPosition);

            buffer.Remove(start, end - start);
            _cursorPosition = start;
        }

        ClearSelection();

        buffer.Insert(_cursorPosition, text);
        _cursorPosition += text.Length;

        RedrawInput(buffer);
    }

    private void MoveToEndOfInput()
    {
        var rowsDown = _renderedRowCount - 1 - _renderedCursorRow;

        if (rowsDown > 0)
        {
            _output.Write($"\u001b[{rowsDown}B");
        }

        _output.Write("\r");
    }

    private void MoveCursorVertically(
    StringBuilder buffer,
    int direction,
        bool redraw = true)
    {
        var text = buffer.ToString();

        var lineStart = text.LastIndexOf(
            '\n',
            Math.Max(0, _cursorPosition - 1)) + 1;

        var column = _cursorPosition - lineStart;

        if (direction < 0)
        {
            if (lineStart == 0)
                return;

            var previousLineEnd = lineStart - 1;

            var previousLineStart = text.LastIndexOf(
                '\n',
                Math.Max(0, previousLineEnd - 1)) + 1;

            var previousLineLength =
                previousLineEnd - previousLineStart;

            _cursorPosition = previousLineStart +
                Math.Min(column, previousLineLength);
        }
        else
        {
            var nextLineStart = text.IndexOf(
                '\n',
                _cursorPosition);

            if (nextLineStart < 0)
                return;

            nextLineStart++;

            var nextLineEnd = text.IndexOf(
                '\n',
                nextLineStart);

            if (nextLineEnd < 0)
                nextLineEnd = text.Length;

            var nextLineLength = nextLineEnd - nextLineStart;

            _cursorPosition = nextLineStart +
                Math.Min(column, nextLineLength);
        }

        if (redraw)
        {
            RedrawInput(buffer);
        }
    }

    private void ExtendSelection(
        StringBuilder buffer,
        TerminalInputKind direction)
    {
        _selectionAnchor ??= _cursorPosition;

        switch (direction)
        {
            case TerminalInputKind.ShiftLeftArrow:
                _cursorPosition = Math.Max(0, _cursorPosition - 1);
                break;

            case TerminalInputKind.ShiftRightArrow:
                _cursorPosition = Math.Min(
                    buffer.Length,
                    _cursorPosition + 1);
                break;

            case TerminalInputKind.ShiftUpArrow:
                MoveCursorVertically(buffer, -1, redraw: false);
                break;

            case TerminalInputKind.ShiftDownArrow:
                MoveCursorVertically(buffer, 1, redraw: false);
                break;
        }

        if (_cursorPosition == _selectionAnchor)
        {
            _selectionAnchor = null;
        }

        RedrawInput(buffer);
    }

    private bool DeleteSelection(StringBuilder buffer)
    {
        if (!HasSelection)
            return false;

        var start = Math.Min(
            _selectionAnchor!.Value,
            _cursorPosition);

        var end = Math.Max(
            _selectionAnchor.Value,
            _cursorPosition);

        buffer.Remove(start, end - start);

        _cursorPosition = start;
        ClearSelection();

        return true;
    }

}
