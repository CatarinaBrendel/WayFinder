namespace WayFinder.DevTools.Cli.Presentation;

internal sealed class ConsoleSpinner : IDisposable
{
    private static readonly char[] Frames =
        ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];

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

        while (!_cancellation.IsCancellationRequested)
        {
            Console.Error.Write(
                $"\r{Frames[index]} {_message}"
            );

            index =
                (index + 1) % Frames.Length;

            await Task.Delay(
                100,
                _cancellation.Token
            );
        }
    }

    private void ClearLine()
    {
        Console.Error.Write(
            $"\r{new string(' ', _message.Length + 3)}\r"
        );
    }
}
