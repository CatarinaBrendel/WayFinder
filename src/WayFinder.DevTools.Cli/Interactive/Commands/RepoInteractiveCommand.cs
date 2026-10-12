using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Reading;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class RepoInteractiveCommand : IInteractiveCommand, IInteractiveCommandCompletion
{
    private readonly WayFinderServices _services;

    public string Name => "/repo";

    public string Description => "Read and search repository content";

    public RepoInteractiveCommand(WayFinderServices services)
    {
        _services = services;
    }

    public IReadOnlyList<string> Subcommands { get; } =
    [
        "read",
        "search"
    ];

    public Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (arguments.Count == 0)
        {
            WriteHelp(output);
            return Task.FromResult(InteractiveCommandResult.Continue);
        }

        switch (arguments[0].ToLowerInvariant())
        {
            case "read":
                Read(arguments, output);
                break;

            case "search":
                Search(arguments, output);
                break;

            default:
                ConsolePresenter.Error(
                    output,
                    $"Unknown repository command: {arguments[0]}"
                );

                WriteHelp(output);
                break;
        }

        return Task.FromResult(InteractiveCommandResult.Continue);
    }

    private void Read(
    IReadOnlyList<string> arguments,
    TextWriter output)
    {
        if (arguments.Count != 2 ||
            string.IsNullOrWhiteSpace(arguments[1]))
        {
            ConsolePresenter.Info(
                output,
                "Usage: /repo read <path>"
            );

            return;
        }

        var path = arguments[1];

        var project = _services.ProjectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No Git project found."
            );

            return;
        }

        try
        {
            var result = _services.RepositoryFileReader.Read(
                project,
                path
            );

            // Preserve the original file contents.
            ConsolePresenter.Raw(output, result.Content);

            if (result.Truncated)
            {
                ConsolePresenter.Warning(
                    output,
                    $"Output truncated at 64 KiB ({result.TotalBytes} bytes total)."
                );
            }
        }
        catch (BinaryFileNotSupportedException exception)
        {
            ConsolePresenter.Error(output, exception.Message);
        }
        catch (FileNotFoundException)
        {
            ConsolePresenter.Error(
                output,
                $"File not found: {path}"
            );
        }
        catch (UnauthorizedAccessException exception)
        {
            ConsolePresenter.Error(output, exception.Message);
        }
    }

    private void Search(
    IReadOnlyList<string> arguments,
    TextWriter output)
    {
        if (arguments.Count != 2 ||
            string.IsNullOrWhiteSpace(arguments[1]))
        {
            ConsolePresenter.Info(
                output,
                "Usage: /repo search <query>"
            );

            return;
        }

        var query = arguments[1];

        var project = _services.ProjectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No Git project found."
            );

            return;
        }

        var result = _services.RepositorySearcher.Search(
            project,
            query
        );

        ConsolePresenter.Header(output, "Repository search");

        ConsolePresenter.Property(
            output,
            "Query",
            query
        );

        ConsolePresenter.Property(
            output,
            "Matches",
            result.Matches.Count.ToString()
        );

        if (result.Matches.Count == 0)
        {
            ConsolePresenter.Info(
                output,
                "No matches found."
            );

            return;
        }

        ConsolePresenter.Subsection(output, "Results");

        foreach (var match in result.Matches)
        {
            ConsolePresenter.Item(
                output,
                ConsoleTheme.Secondary(
                    $"{match.Path}:{match.LineNumber}"
                )
            );

            ConsolePresenter.Text(
                output,
                match.Line
            );
        }

        ConsolePresenter.Info(
            output,
            result.Matches.Count == 1
                ? "1 match"
                : $"{result.Matches.Count} matches"
        );

        if (result.Truncated)
        {
            ConsolePresenter.Warning(
                output,
                $"Results truncated after {result.Matches.Count} matches."
            );
        }
    }

    private static void WriteHelp(TextWriter output)
    {
        ConsolePresenter.Header(
            output,
            "Repository commands"
        );

        ConsolePresenter.Command(
            output,
            "/repo read",
            "Read a repository file: /repo read <path>"
        );

        ConsolePresenter.Command(
            output,
            "/repo search",
            "Search repository content: /repo search <query>"
        );
    }
}
