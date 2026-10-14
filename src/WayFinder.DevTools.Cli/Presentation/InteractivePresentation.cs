using WayFinder.DevTools.Application.Conversations;
using WayFinder.DevTools.Application.Diagnostics;
using WayFinder.DevTools.Cli.Interactive;

namespace WayFinder.DevTools.Cli.Presentation;

internal static class InteractivePresentation
{
    private static readonly string[] GoodbyeMessages =
    [
        "The rubber duck approves.",
        "May your builds stay green.",
        "No semicolons were harmed.",
        "The bugs can wait until tomorrow.",
        "Your coffee deserves a refill.",
        "Remember to commit your changes.",
        "Keep your stack traces short.",
        "The compiler will miss you.",
        "Zero additional technical debt. Probably.",
        "The silicon oracle needs a nap.",
        "May your merge conflicts be trivial.",
        "Stay curious, ship carefully."
    ];

    private static bool _streamingAtLineStart;
    private static int _streamingColumn;
    private const int StreamingPrefixWidth = 5;

    public static void WriteHeader(
        TextWriter output,
        string? provider = null)
    {
        ConsolePresenter.Header(output, "Interactive session");

        ConsolePresenter.Property(
            output,
            "Workspace",
            Environment.CurrentDirectory
        );

        ConsolePresenter.Property(
            output,
            "Provider",
            provider ?? "Not configured"
        );

        ConsolePresenter.Property(
            output,
            "Status",
            provider is not null
                ? ConsoleTheme.Success("Ready")
                : ConsoleTheme.Warning("Local mode")
        );

        output.WriteLine();

        if (provider is not null)
        {
            ConsolePresenter.Text(
                output,
                $"{ConsoleTheme.AiMark} " +
                "Ask your AI assistant anything about your project."
            );

            ConsolePresenter.Text(
                output,
                ConsoleTheme.Secondary(
                    "Type /help to explore available commands."
                )
            );
        }
        else
        {
            ConsolePresenter.Text(
                output,
                ConsoleTheme.Secondary(
                    "AI assistant unavailable. Running in local mode."
                )
            );

            ConsolePresenter.Text(
                output,
                ConsoleTheme.Secondary(
                    "Type /help to explore available commands."
                )
            );
        }
    }

    public static void WritePrompt(TextWriter output)
    {
        ConsolePresenter.Prompt(output);
    }
    public static void WriteHelp(
        TextWriter output,
        IEnumerable<IInteractiveCommand> commands)
    {
        ConsolePresenter.Header(output, "Available commands");

        foreach (var command in commands.OrderBy(c => c.Name))
        {
            ConsolePresenter.Command(
                output,
                command.Name,
                command.Description
            );
        }
    }

    public static void WriteStatus(
        TextWriter output,
        string? provider = null)
    {
        ConsolePresenter.Header(output, "Session status");

        ConsolePresenter.Property(
            output,
            "Workspace",
            Environment.CurrentDirectory
        );

        ConsolePresenter.Property(
            output,
            "Provider",
            provider ?? "Not configured"
        );

        ConsolePresenter.Property(
            output,
            "Status",
            provider is not null
                ? ConsoleTheme.Success("Ready")
                : ConsoleTheme.Warning("Local mode")
        );
    }

    public static void WriteMissingProvider(
        TextWriter output)
    {
        output.WriteLine();

        output.WriteLine(
            $"  {ConsoleTheme.WarningMark} " +
            ConsoleTheme.Warning(
                "No AI provider configured."
            )
        );

        output.WriteLine(
            "    Provider integration is coming next."
        );

        output.WriteLine();
    }

    public static void WriteGoodbye(TextWriter output)
    {
        var message = GoodbyeMessages[
            Random.Shared.Next(GoodbyeMessages.Length)
        ];

        output.WriteLine();

        output.WriteLine(
            $"  {ConsoleTheme.Waypoint} " +
            ConsoleTheme.Bold("Session ended.")
        );

        output.WriteLine(
            $"    {ConsoleTheme.Secondary(message)}"
        );

        output.WriteLine();
    }

    public static void WriteResponse(
    TextWriter output,
        ConversationResponse response)
    {
        WriteStreamingHeader(output);

        WriteStreamingChunk(
            output,
            response.Content
        );

        WriteStreamingFooter(
            output,
            response
        );
    }

    public static void WriteDoctorReport(
        TextWriter output,
        DoctorReport report)
    {
        ConsolePresenter.Header(
            output,
            "Environment diagnostics"
        );

        foreach (var check in report.Checks)
        {
            switch (check.Status)
            {
                case DoctorCheckStatus.Ok:
                    ConsolePresenter.Success(output, check.Message);
                    break;

                case DoctorCheckStatus.Warning:
                    ConsolePresenter.Warning(output, check.Message);
                    break;

                case DoctorCheckStatus.Error:
                    ConsolePresenter.Error(output, check.Message);
                    break;

                default:
                    ConsolePresenter.Info(output, check.Message);
                    break;
            }
        }

        ConsolePresenter.Section(output, "Summary");

        ConsolePresenter.Property(
            output,
            "Healthy",
            report.Checks.Count(
                check => check.Status == DoctorCheckStatus.Ok
            ).ToString()
        );

        ConsolePresenter.Property(
            output,
            "Warnings",
            report.WarningCount.ToString()
        );

        ConsolePresenter.Property(
            output,
            "Errors",
            report.ErrorCount.ToString()
        );
    }

    public static void WriteResponseMetrics(
        TextWriter output,
        ConversationResponse response)
    {
        var model = response.Model ?? "Unknown model";

        var metrics = model;

        if (response.InputTokens is int input &&
            response.OutputTokens is int outputTokens)
        {
            var cached = response.CachedInputTokens ?? 0;

            var cachePercentage = input > 0
                ? cached * 100.0 / input
                : 0;

            metrics +=
                $" · {input:N0} in" +
                $" · {outputTokens:N0} out" +
                $" · {cachePercentage:0.0}% cached";
        }

        if (response.DurationMs is double duration)
        {
            metrics += $" · {duration / 1000:0.0}s";
        }

        output.WriteLine(
            ConsoleTheme.Secondary(
                $"  ╰─ {ConsoleTheme.MetricsMark} {metrics}"
            )
        );
    }

    public static void WriteStreamingHeader(TextWriter output)
    {
        output.WriteLine();

        output.WriteLine(
            $"  {ConsoleTheme.AiMark} " +
            ConsoleTheme.Bold("Copilot")
        );

        output.WriteLine(
            ConsoleTheme.Secondary("  │")
        );

        _streamingAtLineStart = true;
        _streamingColumn = 0;
    }

    public static void WriteStreamingChunk(
        TextWriter output,
        string content)
    {
        var width = Console.IsOutputRedirected
            ? int.MaxValue
            : Math.Max(
                StreamingPrefixWidth + 1,
                Console.WindowWidth - 1);

        foreach (var character in content)
        {
            if (character == '\r')
                continue;

            if (character == '\n')
            {
                output.WriteLine();

                _streamingAtLineStart = true;
                _streamingColumn = 0;

                continue;
            }

            if (_streamingAtLineStart)
            {
                output.Write(
                    ConsoleTheme.Secondary("  │  ")
                );

                _streamingAtLineStart = false;
                _streamingColumn = StreamingPrefixWidth;
            }

            if (_streamingColumn >= width)
            {
                output.WriteLine();

                output.Write(
                    ConsoleTheme.Secondary("  │  ")
                );

                _streamingColumn = StreamingPrefixWidth;
            }

            output.Write(character);

            _streamingColumn++;
        }

        output.Flush();
    }

    public static void WriteStreamingFooter(
        TextWriter output,
        ConversationResponse response)
    {
        if (!_streamingAtLineStart)
        {
            output.WriteLine();
        }

        output.WriteLine(
            ConsoleTheme.Secondary("  │")
        );

        WriteResponseMetrics(output, response);

        _streamingAtLineStart = false;
    }

    public static void WriteToolEvent(
        TextWriter output,
        string name,
        string status)
    {
        var symbol = status switch
        {
            "completed" => ConsoleTheme.SuccessMark,
            "failed" => ConsoleTheme.ErrorMark,
            _ => ConsoleTheme.Secondary("◌")
        };

        var description = status switch
        {
            "completed" => $"{name} completed",
            "failed" => $"{name} failed",
            _ => name
        };

        output.WriteLine(
            $"{ConsoleTheme.Secondary("  ├─")} " +
            $"{symbol} " +
            $"{ConsoleTheme.Secondary(description)}"
        );

        output.Flush();
    }

    public static void BeginStreamingLine()
    {
        _streamingAtLineStart = true;
        _streamingColumn = 0;
    }

    public static void EndStreamingLine(TextWriter output)
    {
        if (!_streamingAtLineStart)
        {
            output.WriteLine();
            _streamingAtLineStart = true;
        }
    }

    public static void WritePermissionRequest(
        TextWriter output,
        string toolName,
        string? arguments)
    {
        var branch = ConsoleTheme.Secondary("  │");

        output.WriteLine(branch);

        output.WriteLine(
            $"{branch}  " +
            $"{ConsoleTheme.WarningMark} " +
            ConsoleTheme.Bold("Permission required")
        );

        output.WriteLine(branch);

        output.WriteLine(
            $"{branch}    " +
            $"{ConsoleTheme.Secondary("Tool").PadRight(10)} {toolName}"
        );

        if (!string.IsNullOrWhiteSpace(arguments))
        {
            output.WriteLine(
                $"{branch}    " +
                $"{ConsoleTheme.Secondary("Arguments").PadRight(10)} {arguments}"
            );
        }

        output.WriteLine(branch);

        // Reserve one row below the confirmation prompt.
        if (ReferenceEquals(output, Console.Out) &&
            !Console.IsOutputRedirected)
        {
            output.WriteLine();
            output.Flush();

            Console.SetCursorPosition(
                0,
                Math.Max(0, Console.CursorTop - 1)
            );
        }

        output.Write(
            $"{branch}    " +
            $"{ConsoleTheme.Success("[Y] Allow once")}  " +
            $"{ConsoleTheme.Warning("[N] Deny")}  " +
            $"{ConsoleTheme.Secondary("❯")} "
        );

        output.Flush();

        output.Flush();
    }

    public static void WritePermissionDecision(
        TextWriter output,
        bool allowed)
    {
        output.WriteLine(
            allowed
                ? ConsoleTheme.Success("Allowed")
                : ConsoleTheme.Warning("Denied")
        );

        output.Flush();
    }
}
