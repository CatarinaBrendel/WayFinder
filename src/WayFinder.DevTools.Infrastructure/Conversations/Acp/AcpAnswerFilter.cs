using System.Text;

namespace WayFinder.DevTools.Infrastructure.Conversations.Acp;

internal sealed class AcpAnswerFilter
{
    private const string DiagnosticPrefix = "Info: Disabled tools:";
    private const int MaxDiagnosticLength = 4096;

    private readonly StringBuilder _buffer = new();
    private bool _resolved;

    public string Process(string chunk)
    {
        if (_resolved)
            return chunk;

        _buffer.Append(chunk);

        var text = _buffer.ToString();

        if (DiagnosticPrefix.StartsWith(text, StringComparison.Ordinal))
            return string.Empty;

        if (!text.StartsWith(DiagnosticPrefix, StringComparison.Ordinal))
        {
            _resolved = true;
            _buffer.Clear();
            return text;
        }

        var newlineIndex = text.IndexOf('\n');

        if (newlineIndex >= 0)
        {
            _resolved = true;
            _buffer.Clear();
            return text[(newlineIndex + 1)..];
        }

        // Copilot may emit the diagnostic and answer without a newline.
        // Do not discard buffered text when no reliable boundary exists.
        if (_buffer.Length > MaxDiagnosticLength)
        {
            _resolved = true;
            _buffer.Clear();
            return text;
        }

        return string.Empty;
    }

    public string Complete()
    {
        if (_resolved)
            return string.Empty;

        _resolved = true;

        var text = _buffer.ToString();
        _buffer.Clear();

        // Preserve ambiguous content rather than silently losing the answer.
        return text;
    }
}
