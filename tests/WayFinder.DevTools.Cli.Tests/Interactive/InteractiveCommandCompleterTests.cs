using WayFinder.DevTools.Cli.Interactive;
using WayFinder.DevTools.Cli.Interactive.Commands;

namespace WayFinder.DevTools.Cli.Tests.Interactive;

public sealed class InteractiveCommandCompleterTests
{
    private readonly InteractiveCommandCompleter _completer;

    public InteractiveCommandCompleterTests()
    {
        var registry =
            new InteractiveCommandRegistry();

        registry.Register(
            new ExitInteractiveCommand("/exit")
        );

        registry.Register(
            new ExitInteractiveCommand("/quit")
        );

        registry.Register(
            new ClearInteractiveCommand(false)
        );

        _completer =
            new InteractiveCommandCompleter(
                registry
            );
    }

    [Fact]
    public void Complete_ReturnsMatchingCommand()
    {
        var result =
            _completer.Complete("/ex");

        Assert.Equal(
            ["/exit"],
            result
        );
    }

    [Fact]
    public void Complete_ReturnsMultipleMatches()
    {
        var result =
            _completer.Complete("/");

        Assert.Equal(
            ["/clear", "/exit", "/quit"],
            result
        );
    }

    [Fact]
    public void Complete_IsCaseInsensitive()
    {
        var result =
            _completer.Complete("/EX");

        Assert.Equal(
            ["/exit"],
            result
        );
    }

    [Fact]
    public void Complete_ReturnsNoMatchesForUnknownCommand()
    {
        var result =
            _completer.Complete("/unknown");

        Assert.Empty(result);
    }

    [Fact]
    public void Complete_IgnoresRegularConversationInput()
    {
        var result =
            _completer.Complete("Explain this code");

        Assert.Empty(result);
    }

    [Fact]
    public void Complete_IgnoresCommandArguments()
    {
        var result =
            _completer.Complete("/exit something");

        Assert.Empty(result);
    }

    [Fact]
    public void Complete_ReturnsMatchingSubcommand()
    {
        var registry = new InteractiveCommandRegistry();

        registry.Register(new TestCommand());

        var completer = new InteractiveCommandCompleter(registry);

        var result = completer.Complete("/project l");

        Assert.Equal(
            ["/project list"],
            result
        );
    }

    [Fact]
    public void Complete_ReturnsAllSubcommands()
    {
        var registry = new InteractiveCommandRegistry();

        registry.Register(new TestCommand());

        var completer = new InteractiveCommandCompleter(registry);

        var result = completer.Complete("/project ");

        Assert.Equal(
            [
                "/project add",
            "/project info",
            "/project init",
            "/project list",
            "/project remove"
            ],
            result
        );
    }

    [Fact]
    public void Complete_IgnoresAdditionalArguments()
    {
        var registry = new InteractiveCommandRegistry();

        registry.Register(new TestCommand());

        var completer = new InteractiveCommandCompleter(registry);

        Assert.Empty(completer.Complete("/project add /tmp"));
    }

    [Theory]
    [InlineData("/git diff --sta", "/git diff --staged")]
    [InlineData("/git log --one", "/git log --oneline")]
    [InlineData(
    "/git diff --staged --sta",
    "/git diff --staged --stat")]
    public void Complete_CompletesOptions(
    string input,
    string expected)
    {
        var registry = new InteractiveCommandRegistry();
        registry.Register(new TestGitCommand());

        var completer = new InteractiveCommandCompleter(registry);

        var result = completer.Complete(input);

        Assert.Contains(expected, result);
    }

    [Fact]
    public void Complete_ExcludesPreviouslyUsedOptions()
    {
        var registry = new InteractiveCommandRegistry();
        registry.Register(new TestGitCommand());

        var completer = new InteractiveCommandCompleter(registry);

        var result = completer.Complete("/git diff --staged --");

        Assert.DoesNotContain(
            "/git diff --staged --staged",
            result
        );

        Assert.Contains(
            "/git diff --staged --stat",
            result
        );

        Assert.Contains(
            "/git diff --staged --path",
            result
        );
    }

    [Fact]
    public void Complete_DoesNotCompleteArbitraryArguments()
    {
        var registry = new InteractiveCommandRegistry();
        registry.Register(new TestGitCommand());

        var completer = new InteractiveCommandCompleter(registry);

        Assert.Empty(
            completer.Complete("/git diff --path src/")
        );
    }

    private sealed class TestCommand :
        IInteractiveCommand,
        IInteractiveCommandCompletion
    {
        public string Name => "/project";

        public string Description => "Test project command";

        public IReadOnlyList<string> Subcommands { get; } =
        [
            "add",
        "info",
        "init",
        "list",
        "remove"
        ];

        public Task<InteractiveCommandResult> ExecuteAsync(
            IReadOnlyList<string> arguments,
            TextWriter output,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(InteractiveCommandResult.Continue);
        }
    }

    private sealed class TestGitCommand :
        IInteractiveCommand,
        IInteractiveCommandCompletion
    {
        public string Name => "/git";

        public string Description => "Git test command";

        public IReadOnlyList<string> Subcommands { get; } =
        [
            "status",
        "diff",
        "log"
        ];

        public IReadOnlyDictionary<string, IReadOnlyList<string>> Options { get; } =
            new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["diff"] =
                [
                    "--staged",
                "--stat",
                "--path"
                ],
                ["log"] =
                [
                    "--count",
                "--oneline",
                "--path"
                ]
            };

        public Task<InteractiveCommandResult> ExecuteAsync(
            IReadOnlyList<string> arguments,
            TextWriter output,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(InteractiveCommandResult.Continue);
        }
    }
}
