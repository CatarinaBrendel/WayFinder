using WayFinder.DevTools.Application.Conversations;

namespace WayFinder.DevTools.Cli.Presentation;

internal static class InteractivePresentation
{
    public static void WriteHeader(
        TextWriter output,
        string? provider = null)
    {
        {
            output.WriteLine();
            output.WriteLine(
                $"  {ConsoleTheme.Rule}"
            );

            output.WriteLine();

            output.WriteLine(
                $"  {ConsoleTheme.Waypoint}  " +
                ConsoleTheme.Bold("Interactive session")
            );

            output.WriteLine();

            WriteProperty(
                output,
                "Workspace",
                Environment.CurrentDirectory
            );

            WriteProperty(
                output,
                "Provider",
                provider ?? "Not configured"
            );

            WriteProperty(
                output,
                "Status",
                ConsoleTheme.Success("Ready")
            );

            output.WriteLine();

            output.WriteLine(
                $"  {ConsoleTheme.Secondary("Type /help for available commands.")}"
            );

            output.WriteLine();
        }
    }

    public static void WritePrompt(
        TextWriter output)
    {
        output.Write(
            $"  {ConsoleTheme.Waypoint} wayfinder " +
            $"{ConsoleTheme.Secondary("›")} "
        );

        output.Flush();
    }

    public static void WriteHelp(
        TextWriter output)
    {
        WriteSection(
            output,
            "Available commands"
        );

        WriteCommand(
            output,
            "/help",
            "Show available commands"
        );

        WriteCommand(
            output,
            "/status",
            "Show session information"
        );

        WriteCommand(
            output,
            "/clear",
            "Clear the screen"
        );

        WriteCommand(
            output,
            "/exit",
            "Exit WayFinder"
        );

        output.WriteLine();
    }

    public static void WriteStatus(
        TextWriter output,
        string? provider = null)
    {
        WriteSection(
            output,
            "Session status"
        );

        WriteProperty(
            output,
            "Workspace",
            Environment.CurrentDirectory
        );

        WriteProperty(
            output,
            "Provider",
            provider ?? "Not configured"
        );

        WriteProperty(
            output,
            "Status",
            ConsoleTheme.Success("Ready")
        );

        output.WriteLine();
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

    public static void WriteGoodbye(
        TextWriter output)
    {
        output.WriteLine();

        output.WriteLine(
            $"  {ConsoleTheme.SuccessMark} Session ended."
        );

        output.WriteLine();
    }

    private static void WriteSection(
        TextWriter output,
        string title)
    {
        output.WriteLine();

        output.WriteLine(
            $"  {ConsoleTheme.Waypoint}  " +
            ConsoleTheme.Bold(title)
        );

        output.WriteLine();
    }

    private static void WriteProperty(
        TextWriter output,
        string name,
        string value)
    {
        output.WriteLine(
            $"     {ConsoleTheme.Secondary(name.PadRight(12))}{value}"
        );
    }

    private static void WriteCommand(
        TextWriter output,
        string command,
        string description)
    {
        output.WriteLine(
            $"    {ConsoleTheme.Accent(command.PadRight(12))}{description}"
        );
    }

    public static void WriteResponse(
        TextWriter output,
        ConversationResponse response)
    {
        output.WriteLine();

        output.WriteLine(
            $"  {ConsoleTheme.Waypoint} " +
            ConsoleTheme.Bold("Copilot")
        );

        output.WriteLine();

        foreach (var line in response.Content.Split('\n'))
        {
            output.WriteLine(
                $"    {line.TrimEnd('\r')}"
            );
        }

        if (response.InputTokens is not int input ||
            response.OutputTokens is not int outputTokens)
        {
            output.WriteLine();
            return;
        }

        var model = response.Model ?? "Unknown model";

        var cached = response.CachedInputTokens ?? 0;

        var cachePercentage = input > 0
            ? cached * 100.0 / input
            : 0;

        var duration = response.DurationMs.HasValue
            ? $" · {response.DurationMs.Value / 1000:0.0}s"
            : "";

        output.WriteLine();

        output.WriteLine(
            ConsoleTheme.Secondary(
                $"  {ConsoleTheme.Waypoint} {model} · " +
                $"{input:N0} in · " +
                $"{outputTokens:N0} out · " +
                $"{cachePercentage:0.0}% cached" +
                duration
            )
        );

        output.WriteLine();
    }
}
