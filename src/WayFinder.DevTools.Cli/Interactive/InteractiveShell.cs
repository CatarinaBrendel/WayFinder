using WayFinder.DevTools.Application.Conversations;
using WayFinder.DevTools.Cli.Presentation;

namespace WayFinder.DevTools.Cli.Interactive;

public sealed class InteractiveShell
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly ConversationSession? _session;

    private string? ProviderName => _session?.ProviderId switch
    {
        "copilot" => "GitHub Copilot",
        null => null,
        var id => id
    };

    public InteractiveShell(
        TextReader input,
        TextWriter output,
        ConversationSession? session = null)
    {
        _input = input;
        _output = output;
        _session = session;
    }

    public async Task<int> RunAsync(
        CancellationToken cancellationToken = default)
    {
        InteractivePresentation.WriteHeader(
            _output,
            ProviderName
        );

        while (!cancellationToken.IsCancellationRequested)
        {
            InteractivePresentation.WritePrompt(
                _output
            );

            string? input;

            try
            {
                input = await _input.ReadLineAsync(
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

            switch (command.ToLowerInvariant())
            {
                case "/exit":
                case "/quit":

                    InteractivePresentation.WriteGoodbye(
                        _output
                    );

                    return 0;

                case "/help":

                    InteractivePresentation.WriteHelp(
                        _output
                    );

                    break;

                case "/status":

                    InteractivePresentation.WriteStatus(
                        _output,
                        ProviderName
                    );

                    break;

                case "/clear":

                    if (ReferenceEquals(
                        _output,
                        Console.Out
                    ) && !Console.IsOutputRedirected)
                    {
                        Console.Clear();
                    }

                    break;

                default:

                    if (_session is null)
                    {
                        InteractivePresentation.WriteMissingProvider(
                            _output
                        );

                        break;
                    }

                    try
                    {
                        ConversationResponse response;

                        using (var spinner =
                            ReferenceEquals(_output, Console.Out)
                                ? ConsoleSpinner.StartRandom()
                                : null)
                        {
                            response = await _session.SendAsync(
                                command,
                                cancellationToken
                            );
                        }

                        InteractivePresentation.WriteResponse(
                            _output,
                            response
                        );
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

                    break;
            }
        }

        return 0;
    }
}
