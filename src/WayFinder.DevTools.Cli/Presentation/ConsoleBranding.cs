namespace WayFinder.DevTools.Cli.Presentation;

internal static class ConsoleBranding
{
    private const string Reset = "\e[0m";
    private const string Cyan = "\e[36m";
    private const string Lilac = "\e[38;2;200;162;255m";
    private const string Bold = "\e[1m";

    public static void Write()
    {
        if (Console.IsOutputRedirected)
        {
            WritePlain();
            return;
        }

        Console.WriteLine(
            $"  {Lilac}◇{Cyan}─╮{Reset}"
        );

        Console.WriteLine(
            $"    {Cyan}│{Reset}  {Bold}WayFinder{Reset}"
        );

        Console.WriteLine(
            $"  {Cyan}╰─╯{Reset}  Developer tools"
        );

        Console.WriteLine();
    }

    private static void WritePlain()
    {
        Console.WriteLine("  ◇─╮");
        Console.WriteLine("    │  WayFinder");
        Console.WriteLine("  ╰─╯  Developer tools");
        Console.WriteLine();
    }
}
