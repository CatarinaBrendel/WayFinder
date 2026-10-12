using System.CommandLine;
using WayFinder.DevTools.Cli.Interactive;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Application.Conversations;
using WayFinder.DevTools.Infrastructure.Conversations;

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

                IAiProvider provider = new CopilotProvider();

                var session = new ConversationSession(provider);

                var shell = new InteractiveShell(
                    Console.In,
                    Console.Out,
                    session
                );

                return await shell.RunAsync(
                    cancellationToken
                );
            }
        );

        return command;
    }
}
