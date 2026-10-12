namespace WayFinder.DevTools.Cli.Presentation;

internal sealed class ConsoleSpinner : IDisposable
{
    private static readonly char[] Frames =
        ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];

    private static readonly string[] Messages =
    [
        "Consulting the silicon oracle...",
        "Summoning the semicolons...",
        "Negotiating with the tokens...",
        "Asking the rubber duck...",
        "Untangling the stack...",
        "Compiling thoughts...",
        "Interrogating the bits...",
        "Connecting the dots...",
        "Following the breadcrumbs...",
        "Searching for the plot..."
    ];

    private const string Indent = "    ";

    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _animation;
    private readonly string _message;
    private readonly bool _enabled;

    private ConsoleSpinner(
        string message
    )
    {
        _message = message;
        _enabled = !Console.IsErrorRedirected;

        _animation =
            _enabled
                ? Task.Run(Animate)
                : Task.CompletedTask;
    }

    public static ConsoleSpinner Start(
        string message
    ) =>
        new(message);

    public static ConsoleSpinner StartRandom() =>
    new(Messages[Random.Shared.Next(Messages.Length)]);

    public void Dispose()
    {
        if (!_enabled)
        {
            _cancellation.Dispose();

            return;
        }

        _cancellation.Cancel();

        try
        {
            _animation.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }

        ClearLine();

        _cancellation.Dispose();
    }

    private async Task Animate()
    {
        var index = 0;

        try
        {
            while (!_cancellation.IsCancellationRequested)
            {
                Console.Error.Write(
                    $"\r{Indent}{Frames[index]} {_message}"
                );

                index = (index + 1) % Frames.Length;

                await Task.Delay(
                    100,
                    _cancellation.Token
                );
            }
        }
        catch (OperationCanceledException)
            when (_cancellation.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private void ClearLine()
    {
        Console.Error.Write(
            $"\r{Indent}{new string(' ', _message.Length + 3)}\r"
        );
    }
}
