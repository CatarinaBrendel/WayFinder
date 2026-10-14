using System.Text;
using WayFinder.DevTools.Cli.Presentation;

namespace WayFinder.DevTools.Cli.Interactive;

internal sealed class InteractiveLineEditor
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly InteractiveCommandCompleter _completer;

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

                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        RedrawInput(buffer);
                    }

                    break;

                case ConsoleKey.Tab:
                    Complete(buffer);
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        ClearSuggestions();

                        buffer.Append(key.KeyChar);
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
        _output.Write("\r\u001b[2K");

        InteractivePresentation.WritePrompt(_output);

        _output.Write(buffer.ToString());
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
