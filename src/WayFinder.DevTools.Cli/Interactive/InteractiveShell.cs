using WayFinder.DevTools.Cli.Presentation;

namespace WayFinder.DevTools.Cli.Interactive;

public sealed class InteractiveShell
{
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public InteractiveShell(
        TextReader input,
        TextWriter output)
    {
        _input = input;
        _output = output;
    }

    public async Task<int> RunAsync(
        CancellationToken cancellationToken = default)
    {
        InteractivePresentation.WriteHeader(
            _output
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
                        _output
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

                    InteractivePresentation.WriteMissingProvider(
                        _output
                    );

                    break;
            }
        }

        return 0;
    }
}
