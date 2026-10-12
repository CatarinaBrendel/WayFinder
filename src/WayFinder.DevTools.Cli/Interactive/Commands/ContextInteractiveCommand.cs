using WayFinder.DevTools.Application.Context;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class ContextInteractiveCommand : IInteractiveCommand, IInteractiveCommandCompletion
{
    private const int DefaultBudget = 8_000;

    private readonly WayFinderServices _services;

    public string Name => "/context";

    public string Description => "Build relevant repository context for a development task";

    public ContextInteractiveCommand(WayFinderServices services)
    {
        _services = services;
    }

    public IReadOnlyList<string> Subcommands { get; } = [];

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Options { get; } =
        new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase)
        {
            [""] =
            [
                "--budget",
            "--explain",
            "--content"
            ]
        };

    public Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var budget = DefaultBudget;
        var explain = false;
        var content = false;

        var taskParts = new List<string>();

        for (var i = 0; i < arguments.Count; i++)
        {
            switch (arguments[i])
            {
                case "--budget":
                    if (++i >= arguments.Count ||
                        !int.TryParse(arguments[i], out budget))
                    {
                        WriteHelp(output);
                        return Continue();
                    }

                    break;

                case "--explain":
                    explain = true;
                    break;

                case "--content":
                    content = true;
                    break;

                default:
                    if (arguments[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        ConsolePresenter.Error(
                            output,
                            $"Unknown option: {arguments[i]}"
                        );
                        return Continue();
                    }

                    taskParts.Add(arguments[i]);
                    break;
            }
        }

        var task = string.Join(" ", taskParts);

        if (string.IsNullOrWhiteSpace(task))
        {
            ConsolePresenter.Error(
                output,
                "Task must not be empty."
            );

            WriteHelp(output);
            return Continue();
        }

        if (budget <= 0)
        {
            ConsolePresenter.Error(
                output,
                "Token budget must be greater than zero."
            );

            return Continue();
        }

        var project = _services.ProjectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "✗ No project found."
            );

            return Continue();
        }

        var package = _services.ContextCompiler.Compile(
            project,
            new ContextRequest(task, budget)
        );

        if (content)
        {
            if (package.Files.Count == 0 &&
                package.Guidance is null)
            {
                ConsolePresenter.Warning(
                    output,
                    "No repository context was selected."
                );
            }
            else
            {
                WritePackage(package, output);
            }
        }

        WriteDiagnostics(package, output);

        if (explain)
        {
            WriteExplanation(package, output);
        }

        return Continue();
    }

    private static Task<InteractiveCommandResult> Continue()
    {
        return Task.FromResult(InteractiveCommandResult.Continue);
    }

    private static void WritePackage(
        ContextPackage package,
        TextWriter output)
    {
        output.WriteLine(
            $"<context task=\"{EscapeAttribute(package.Task)}\" project=\"{EscapeAttribute(package.ProjectName)}\">"
        );

        if (package.Guidance is not null)
        {
            output.WriteLine(
                $"<guidance path=\"{EscapeAttribute(package.Guidance.Path)}\">"
            );

            WriteContent(package.Guidance.Content, output);

            output.WriteLine("</guidance>");
        }

        foreach (var file in package.Files)
        {
            WriteFile(file, output);
        }

        output.WriteLine("</context>");
    }

    private static void WriteFile(
        ContextFile file,
        TextWriter output)
    {
        switch (file.Kind)
        {
            case ContextFileKind.Complete:
                output.WriteLine(
                    $"<file path=\"{EscapeAttribute(file.Path)}\">"
                );

                WriteContent(file.Content, output);

                output.WriteLine("</file>");
                break;

            case ContextFileKind.Excerpt:
                if (file.StartLine is null || file.EndLine is null)
                {
                    throw new InvalidOperationException(
                        $"Excerpt '{file.Path}' does not have a valid line range."
                    );
                }

                output.WriteLine(
                    $"<excerpt path=\"{EscapeAttribute(file.Path)}\" start-line=\"{file.StartLine}\" end-line=\"{file.EndLine}\">"
                );

                WriteContent(file.Content, output);

                output.WriteLine("</excerpt>");
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported context file kind: {file.Kind}."
                );
        }
    }

    private static void WriteContent(
        string content,
        TextWriter output)
    {
        output.Write(content);

        if (!content.EndsWith('\n'))
        {
            output.WriteLine();
        }
    }

    private static void WriteDiagnostics(
        ContextPackage package,
        TextWriter output)
    {
        ConsolePresenter.Header(output, "Context");

        ConsolePresenter.Property(
            output,
            "Files",
            package.Statistics.FileCount.ToString()
        );

        ConsolePresenter.Property(
            output,
            "Tokens",
            $"~{package.Statistics.EstimatedTokens:N0} / " +
            $"{package.Statistics.TokenBudget:N0}"
        );

        if (package.Guidance is not null)
        {
            ConsolePresenter.Success(
                output,
                $"Guidance: {package.Guidance.Path}"
            );
        }
    }

    private static void WriteExplanation(
        ContextPackage package,
        TextWriter output)
    {
        ConsolePresenter.Section(output, "Selection");

        if (package.Selections.Count == 0)
        {
            ConsolePresenter.Info(
                output,
                "No files matched the task."
            );

            return;
        }

        foreach (var selection in package.Selections)
        {
            var status = selection.Included
                ? ConsoleTheme.SuccessMark
                : ConsoleTheme.WarningMark;

            ConsolePresenter.Item(
                output,
                $"{status} {selection.Score,3}  {selection.Path}"
            );

            if (selection.PathMatchedTerms.Count > 0)
            {
                ConsolePresenter.Property(
                    output,
                    "Filename",
                    string.Join(", ", selection.PathMatchedTerms)
                );
            }

            if (selection.ContentMatchedTerms.Count > 0)
            {
                ConsolePresenter.Property(
                    output,
                    "Content",
                    string.Join(", ", selection.ContentMatchedTerms)
                );
            }
        }
    }

    private static string EscapeAttribute(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    private static void WriteHelp(TextWriter output)
    {
        ConsolePresenter.Header(output, "Context commands");

        ConsolePresenter.Text(
            output,
            "Usage: /context <task> [--budget <tokens>] [--explain] [--content]"
        );

        ConsolePresenter.Subsection(output, "Options");

        ConsolePresenter.Command(
            output,
            "--budget <tokens>",
            "Maximum estimated tokens (default: 8000)"
        );

        ConsolePresenter.Command(
            output,
            "--explain",
            "Explain file selection"
        );

        ConsolePresenter.Command(
            output,
            "--content",
            "Display compiled context"
        );
    }
}
