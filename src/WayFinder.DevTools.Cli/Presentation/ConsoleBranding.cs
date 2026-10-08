namespace WayFinder.DevTools.Cli.Presentation;

internal static class ConsoleBranding
{
    private const int FrameDelayMilliseconds = 120;

    public static void Write()
    {
        if (Console.IsOutputRedirected)
        {
            WriteStatic();
            return;
        }

        WriteAnimated();
    }

    private static void WriteAnimated()
    {
        var frames =
            new[]
            {
                (
                    $"  {ConsoleTheme.Accent("◇")}",
                    "",
                    ""
                ),
                (
                    $"  {ConsoleTheme.Accent("◇")}{ConsoleTheme.Secondary("─╮")}",
                    "",
                    ""
                ),
                (
                    $"  {ConsoleTheme.Accent("◇")}{ConsoleTheme.Secondary("─╮")}",
                    $"    {ConsoleTheme.Secondary("│")}",
                    ""
                ),
                (
                    $"  {ConsoleTheme.Accent("◇")}{ConsoleTheme.Secondary("─╮")}",
                    $"    {ConsoleTheme.Secondary("│")}",
                    $"    {ConsoleTheme.Secondary("╯")}"
                ),
                (
                    $"  {ConsoleTheme.Accent("◇")}{ConsoleTheme.Secondary("─╮")}",
                    $"    {ConsoleTheme.Secondary("│")}",
                    $"   {ConsoleTheme.Secondary("─╯")}"
                ),
                (
                    $"  {ConsoleTheme.Accent("◇")}{ConsoleTheme.Secondary("─╮")}",
                    $"    {ConsoleTheme.Secondary("│")}",
                    $"  {ConsoleTheme.Secondary("╰─╯")}"
                ),
                (
                    $"  {ConsoleTheme.Accent("◇")}{ConsoleTheme.Secondary("─╮")}",
                    $"    {ConsoleTheme.Secondary("│")}  {ConsoleTheme.Bold("WayFinder")}",
                    $"  {ConsoleTheme.Secondary("╰─╯")}  Developer tools"
                ),
            };
        for (var index = 0;
             index < frames.Length;
             index++)
        {
            var frame =
                frames[index];

            Console.WriteLine(frame.Item1);
            Console.WriteLine(frame.Item2);
            Console.WriteLine(frame.Item3);

            if (index ==
                frames.Length - 1)
            {
                break;
            }

            Thread.Sleep(
                FrameDelayMilliseconds
            );

            Console.Write(
                "\u001b[3A\u001b[J"
            );
        }

        Console.WriteLine();
    }

    private static void WriteStatic()
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
