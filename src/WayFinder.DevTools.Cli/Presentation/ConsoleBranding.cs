namespace WayFinder.DevTools.Cli.Presentation;

internal static class ConsoleBranding
{
    public static void Write()
    {
        Console.WriteLine(
            $"  {ConsoleTheme.Accent("◇")}{ConsoleTheme.Secondary("─╮")}"
        );

        Console.WriteLine(
            $"    {ConsoleTheme.Secondary("│")}  {ConsoleTheme.Bold("WayFinder")}"
        );

        Console.WriteLine(
            $"  {ConsoleTheme.Secondary("╰─╯")}  Developer tools"
        );

        Console.WriteLine();
    }
}
