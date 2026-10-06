namespace WayFinder.DevTools.Cli.Presentation;

internal static class ConsoleTheme
{
    private const string Reset = "\e[0m";
    private const string BoldCode = "\e[1m";
    private const string Green = "\e[32m";
    private const string Red = "\e[31m";
    private const string Cyan = "\e[36m";

    private const string Lilac =
        "\e[38;2;200;162;255m";

    private const string Orange =
        "\e[38;2;245;166;91m";

    private static bool UseColor =>
        !Console.IsOutputRedirected
        && string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable(
                "NO_COLOR"
            )
        );

    public static string Success(
        string text
    ) =>
        Colorize(text, Green);

    public static string Warning(
        string text
    ) =>
        Colorize(text, Orange);

    public static string Error(
        string text
    ) =>
        Colorize(text, Red);

    public static string Accent(
        string text
    ) =>
        Colorize(text, Lilac);

    public static string Secondary(
        string text
    ) =>
        Colorize(text, Cyan);

    public static string Bold(
        string text
    ) =>
        Colorize(text, BoldCode);

    private static string Colorize(
        string text,
        string color
    )
    {
        if (!UseColor)
        {
            return text;
        }

        return $"{color}{text}{Reset}";
    }
}
