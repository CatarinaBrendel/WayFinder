using System.CommandLine;
using WayFinder.DevTools.Application.Conversations;
using WayFinder.DevTools.Cli.Interactive;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Infrastructure.Conversations.Acp;

namespace WayFinder.DevTools.Cli.Commands.Interactive;

public static class InteractiveCommand
{
    public static Command Create(WayFinderServices services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var command = new Command(
            "interactive",
            "Start an interactive WayFinder session.");

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            ConsoleBranding.WriteStatic();

            await using var provider = new CopilotAcpProvider();

            var session = new ConversationSession(provider);

            var shell = new InteractiveShell(
                Console.In,
                Console.Out,
                session,
                services);

            return await shell.RunAsync(cancellationToken);
        });

        return command;
    }
}
