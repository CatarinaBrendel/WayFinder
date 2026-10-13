using WayFinder.DevTools.Application.Conversations;
using WayFinder.DevTools.Cli.Interactive.Commands;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Interactive;

public sealed class InteractiveShell
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly ConversationSession? _session;
    private readonly InteractiveCommandRegistry _registry;
    private readonly InteractiveLineEditor _lineEditor;
    private const string BlinkingBarCursor = "\u001b[5 q";
    private const string DefaultCursor = "\u001b[0 q";

    private string? ProviderName => _session?.ProviderId switch
    {
        "copilot" => "GitHub Copilot",
        null => null,
        var id => id
    };

    public InteractiveShell(
        TextReader input,
        TextWriter output,
        ConversationSession? session = null,
        WayFinderServices? services = null)
    {
        _input = input;
        _output = output;
        _session = session;

        _registry = new InteractiveCommandRegistry();
        _registry.Register(new ExitInteractiveCommand("/exit"));
        _registry.Register(new ExitInteractiveCommand("/quit"));
        _registry.Register(new StatusInteractiveCommand(ProviderName));
        _registry.Register(new ClearInteractiveCommand(ReferenceEquals(_output, Console.Out) && !Console.IsOutputRedirected));
        _registry.Register(new HelpInteractiveCommand(_registry));

        if (services is not null)
        {
            _registry.Register(
                new DoctorInteractiveCommand(services)
            );

            _registry.Register(
                new ProjectInteractiveCommand(
                    services.ProjectRegistry,
                    services.ProjectLocator,
                    services.ProjectManifestReader,
                    services.ProjectInspector,
                    services.ProjectFileSystem,
                    services.ProjectInitializer,
                    _input
                )
            );

            _registry.Register(
                new RepoInteractiveCommand(services)
            );

            _registry.Register(
                new GitInteractiveCommand(services)
            );

            _registry.Register(
                new TreeInteractiveCommand(services)
            );

            _registry.Register(
                new ContextInteractiveCommand(services)
            );
        }

        _lineEditor = new InteractiveLineEditor(
            _input,
            _output,
            new InteractiveCommandCompleter(_registry)
        );

    }

    public async Task<int> RunAsync(
    CancellationToken cancellationToken = default)
    {
        var isInteractiveTerminal =
            ReferenceEquals(_input, Console.In) &&
            ReferenceEquals(_output, Console.Out) &&
            !Console.IsInputRedirected &&
            !Console.IsOutputRedirected;

        if (!isInteractiveTerminal)
        {
            return await RunSessionAsync(cancellationToken);
        }

        try
        {
            _output.Write(BlinkingBarCursor);
            _output.Flush();

            return await RunSessionAsync(cancellationToken);
        }
        finally
        {
            _output.Write(DefaultCursor);
            _output.Flush();
        }
    }

    private async Task<int> RunSessionAsync(
        CancellationToken cancellationToken = default)
    {
        InteractivePresentation.WriteHeader(
            _output,
            ProviderName
        );

        while (!cancellationToken.IsCancellationRequested)
        {
            _output.WriteLine();

            InteractivePresentation.WritePrompt(
                _output
            );

            string? input;

            try
            {
                input = await _lineEditor.ReadLineAsync(
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (input is null)
            {
                break;
            }

            var command = input.Trim();

            if (command.Length == 0)
            {
                continue;
            }

            // Separate command name from arguments.
            if (command.StartsWith('/'))
            {
                IReadOnlyList<string> parts;

                try
                {
                    parts = InteractiveCommandParser.Parse(command);
                }
                catch (FormatException ex)
                {
                    _output.WriteLine($"Invalid command: {ex.Message}");
                    continue;
                }

                var commandName = parts[0];

                IReadOnlyList<string> arguments = parts
                    .Skip(1)
                    .ToArray();

                if (_registry.TryGet(commandName, out var handler))
                {
                    try
                    {
                        var result = await handler!.ExecuteAsync(
                            arguments,
                            _output,
                            cancellationToken
                        );

                        if (result == InteractiveCommandResult.Exit)
                        {
                            InteractivePresentation.WriteGoodbye(_output);
                            return 0;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        await _output.WriteLineAsync(
                            $"Error: {ex.Message}"
                        );
                    }

                    continue;
                }

                await _output.WriteLineAsync(
                    $"Unknown command: {commandName}. Type /help for available commands."
                );

                continue;
            }

            // Never forward unknown slash commands to the AI.
            if (command.StartsWith('/'))
            {
                await _output.WriteLineAsync(
                    $"Unknown command: {command}. Type /help for available commands."
                );

                continue;
            }

            // No AI provider configured.
            if (_session is null)
            {
                InteractivePresentation.WriteMissingProvider(_output);
                continue;
            }

            // Send normal messages to the AI provider.
            try
            {
                ConversationResponse response;
                var hasStreamedAnswer = false;

                var toolNames = new Dictionary<string, string>();
                var toolStatuses = new Dictionary<string, string>();

                ConsoleSpinner? spinner =
                    ReferenceEquals(_output, Console.Out)
                        ? ConsoleSpinner.StartRandom()
                        : null;

                try
                {
                    response = await _session.SendStreamingAsync(
                        command,
                        streamEvent =>
                        {
                            switch (streamEvent.Type)
                            {
                                case ConversationStreamEventType.Answer:
                                    {
                                        if (!hasStreamedAnswer)
                                        {
                                            spinner?.Dispose();
                                            spinner = null;

                                            InteractivePresentation.WriteStreamingHeader(_output);
                                            hasStreamedAnswer = true;
                                        }

                                        InteractivePresentation.WriteStreamingChunk(
                                            _output,
                                            streamEvent.Content
                                        );

                                        break;
                                    }

                                case ConversationStreamEventType.ToolStarted:
                                case ConversationStreamEventType.ToolUpdated:
                                    {
                                        var tool = streamEvent.Tool;

                                        if (tool is null || string.IsNullOrWhiteSpace(tool.Id))
                                            break;

                                        if (!string.IsNullOrWhiteSpace(tool.Name))
                                            toolNames[tool.Id] = tool.Name;

                                        if (!toolNames.TryGetValue(tool.Id, out var name))
                                            name = "MCP tool";

                                        var status = string.IsNullOrWhiteSpace(tool.Status)
                                            ? "pending"
                                            : tool.Status;

                                        if (toolStatuses.TryGetValue(tool.Id, out var previous) &&
                                            previous == status)
                                        {
                                            break;
                                        }

                                        toolStatuses[tool.Id] = status;

                                        spinner?.Dispose();
                                        spinner = null;

                                        if (hasStreamedAnswer)
                                        {
                                            InteractivePresentation.EndStreamingLine(_output);
                                        }

                                        InteractivePresentation.WriteToolEvent(
                                            _output,
                                            name,
                                            status
                                        );

                                        break;
                                    }
                            }
                        },
                        cancellationToken
                    );
                }
                finally
                {
                    spinner?.Dispose();
                }

                if (hasStreamedAnswer)
                {
                    InteractivePresentation.WriteStreamingFooter(
                        _output,
                        response
                    );
                }
                else
                {
                    InteractivePresentation.WriteResponse(
                        _output,
                        response
                    );
                }
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                await _output.WriteLineAsync(
                    $"Error: {ex.Message}"
                );
            }
        }

        return 0;
    }
}
