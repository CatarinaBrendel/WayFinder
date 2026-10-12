using System.CommandLine;
using WayFinder.DevTools.Cli.Interactive;
using WayFinder.DevTools.Cli.Presentation;

namespace WayFinder.DevTools.Cli.Commands.Interactive;

public static class InteractiveCommand
{
    public static Command Create()
    {
        var command = new Command(
            "interactive",
            "Start an interactive WayFinder session."
        );

        command.SetAction(
            async (parseResult, cancellationToken) =>
            {
                ConsoleBranding.WriteStatic();

                var shell = new InteractiveShell(
                    Console.In,
                    Console.Out
                );

                return await shell.RunAsync(
                    cancellationToken
                );
            }
        );

        return command;
    }
}
