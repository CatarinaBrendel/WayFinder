using System.Diagnostics;

namespace WayFinder.DevTools.Cli.Interactive;

internal sealed class TerminalRawMode : IDisposable
{
    private readonly string _originalMode;
    private bool _disposed;

    public TerminalRawMode()
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException(
                "Raw terminal input is currently supported only on macOS and Linux.");
        }

        _originalMode = RunStty("-g");

        if (string.IsNullOrWhiteSpace(_originalMode))
        {
            throw new InvalidOperationException(
                "Could not determine the current terminal settings.");
        }

        try
        {
            // Disable canonical input and echo, while preserving
            // signal handling and normal output processing.
            RunStty("-icanon -echo -icrnl min 1 time 0");

            // Enable bracketed paste.
            Console.Out.Write("\u001b[?2004h");
            Console.Out.Flush();
        }
        catch
        {
            RunStty(_originalMode);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            Console.Out.Write("\u001b[?2004l");
            Console.Out.Flush();
        }
        finally
        {
            RunStty(_originalMode);
        }
    }

    private static string RunStty(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = $"-c \"stty {arguments} < /dev/tty\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException(
            "Could not start stty.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"stty failed: {error}");
        }

        return output.Trim();
    }
}
