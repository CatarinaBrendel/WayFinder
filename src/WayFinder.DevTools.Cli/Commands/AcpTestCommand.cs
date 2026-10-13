using System.CommandLine;
using WayFinder.DevTools.Application.Conversations;
using WayFinder.DevTools.Infrastructure.Conversations.Acp;

namespace WayFinder.DevTools.Cli.Commands;

internal static class AcpTestCommand
{
    public static Command Create()
    {
        var command = new Command(
            "acp-test",
            "Test the WayFinder ACP provider.");

        command.SetAction(async (_, cancellationToken) =>
        {
            return await RunAsync(cancellationToken);
        });

        return command;
    }

    private static async Task<int> RunAsync(
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting Copilot ACP provider...");

        await using var client = new AcpClient();

        var session = new AcpSession(client);

        await session.InitializeAsync(cancellationToken);

        Console.WriteLine();
        Console.WriteLine($"Session: {session.SessionId}");
        Console.WriteLine($"Model:   {session.CurrentModelId}");

        Console.WriteLine();
        Console.WriteLine("Testing tool restrictions...");
        Console.WriteLine();

        var toolCalls = 0;

        client.NotificationReceived += message =>
        {
            if (!message.TryGetProperty("method", out var method) ||
                method.GetString() != "session/update")
            {
                return;
            }

            if (!message.TryGetProperty("params", out var parameters) ||
                !parameters.TryGetProperty("sessionId", out var sessionId) ||
                sessionId.GetString() != session.SessionId ||
                !parameters.TryGetProperty("update", out var update) ||
                !update.TryGetProperty("sessionUpdate", out var updateType))
            {
                return;
            }

            switch (updateType.GetString())
            {
                case "agent_message_chunk":
                    if (update.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("text", out var text))
                    {
                        Console.Write(text.GetString());
                    }
                    break;

                case "tool_call":
                    Interlocked.Increment(ref toolCalls);

                    var title = update.TryGetProperty("title", out var toolTitle)
                        ? toolTitle.GetString()
                        : "Unknown tool";

                    Console.WriteLine();
                    Console.WriteLine($"[TOOL] {title}");
                    break;

                case "tool_call_update":
                    var status = update.TryGetProperty("status", out var toolStatus)
                        ? toolStatus.GetString()
                        : "unknown";

                    Console.WriteLine($"       Status: {status}");

                    if (status == "failed" &&
                        update.TryGetProperty("rawOutput", out var rawOutput))
                    {
                        Console.WriteLine($"       Error: {rawOutput}");
                    }

                    break;
            }
        };

        var result = await session.SendPromptAsync(
        """
        Use the WayFinder MCP tools to:

        1. List the registered projects.
        2. Identify the WayFinder.DevTools project.
        3. Read its AGENTS.md file.
        4. Return only the first three Markdown headings.

        Do not reproduce the file contents.
        Keep your response under 100 words.
        """,
        cancellationToken);

        Console.WriteLine();
        Console.WriteLine($"Tool calls observed: {toolCalls}");

        if (result.TryGetProperty("stopReason", out var stopReason))
        {
            Console.WriteLine($"Stop reason: {stopReason.GetString()}");
        }

        return 0;
    }
}
