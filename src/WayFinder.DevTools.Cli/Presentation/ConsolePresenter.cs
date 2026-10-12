namespace WayFinder.DevTools.Cli.Presentation;

internal static class ConsolePresenter
{
    private const string Margin = "  ";
    private const string Indent = "    ";
    private const int PropertyWidth = 12;

    public static void Header(
        TextWriter output,
        string title)
    {
        output.WriteLine();
        output.WriteLine(
            $"{Margin}{ConsoleTheme.Waypoint} {ConsoleTheme.Bold(title)}"
        );
        output.WriteLine($"{Margin}{ConsoleTheme.Rule}");
    }

    public static void Section(
        TextWriter output,
        string title)
    {
        Header(output, title);
    }

    public static void Property(
        TextWriter output,
        string name,
        string value)
    {
        output.WriteLine(
            $"{Indent}{ConsoleTheme.Secondary(name.PadRight(PropertyWidth))}{value}"
        );
    }

    public static void Text(
        TextWriter output,
        string message)
    {
        output.WriteLine($"{Indent}{message}");
    }

    public static void Success(
        TextWriter output,
        string message)
    {
        output.WriteLine(
            $"{Indent}{ConsoleTheme.SuccessMark} {message}"
        );
    }

    public static void Warning(
        TextWriter output,
        string message)
    {
        output.WriteLine(
            $"{Indent}{ConsoleTheme.WarningMark} {message}"
        );
    }

    public static void Error(
        TextWriter output,
        string message)
    {
        output.WriteLine(
            $"{Indent}{ConsoleTheme.ErrorMark} {message}"
        );
    }

    public static void Info(
        TextWriter output,
        string message)
    {
        output.WriteLine(
            $"{Indent}{ConsoleTheme.Waypoint} {message}"
        );
    }

    public static void Command(
        TextWriter output,
        string name,
        string description)
    {
        output.WriteLine(
            $"{Indent}{ConsoleTheme.Accent(name.PadRight(20))}{description}"
        );
    }

    public static void Prompt(
        TextWriter output)
    {
        output.Write(
            $"{Margin}{ConsoleTheme.Waypoint} wayfinder " +
            $"{ConsoleTheme.Secondary("›")} "
        );

        output.Flush();
    }

    public static void Subsection(
        TextWriter output,
        string title)
    {
        output.WriteLine();

        output.WriteLine(
            $"    {ConsoleTheme.Bold(title)}"
        );
    }

    public static void Item(
        TextWriter output,
        string text)
    {
        output.WriteLine($"    {text}");
    }

    public static void Raw(
        TextWriter output,
        string content)
    {
        output.Write(content);

        if (!content.EndsWith('\n'))
        {
            output.WriteLine();
        }
    }

    public static void Confirm(TextWriter output, string question)
    {
        output.Write(
            $"{Indent}{question} {ConsoleTheme.Warning("[y/N]")} "
        );

        output.Flush();
    }

    public static void TreeLine(
        TextWriter output,
        string line)
    {
        output.WriteLine($"{Indent}{line}");
    }
}
