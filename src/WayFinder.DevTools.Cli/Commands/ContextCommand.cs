using System.CommandLine;
using WayFinder.DevTools.Application.Context;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands;

internal static class ContextCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var taskArgument =
            new Argument<string>(
                "task"
            )
            {
                Description =
                    "Development task to build repository context for."
            };

        var budgetOption =
            new Option<int>(
                "--budget"
            )
            {
                Description =
                    "Maximum estimated context tokens.",
                DefaultValueFactory =
                    _ => 8_000,
            };

        var explainOption =
            new Option<bool>(
                "--explain"
            )
            {
                Description =
                    "Explain how repository files were selected.",
            };

        var contentOption =
            new Option<bool>(
                "--content"
            )
            {
                Description =
                    "Write the compiled repository context to standard output.",
            };

        var command =
            new Command(
                "context",
                "Build relevant repository context for a development task."
            )
            {
                taskArgument,
                budgetOption,
                explainOption,
                contentOption,
            };

        command.SetAction(
            parseResult =>
            {
                var task =
                    parseResult.GetValue(
                        taskArgument
                    );

                var budget =
                    parseResult.GetValue(
                        budgetOption
                    );

                var explain =
                    parseResult.GetValue(
                        explainOption
                    );
                var content =
                    parseResult.GetValue(
                        contentOption
                    );

                return Execute(
                    services,
                    task!,
                    budget,
                    explain,
                    content
                );
            }
        );

        return command;
    }

    private static int Execute(
        WayFinderServices services,
        string task,
        int budget,
        bool explain,
        bool content
    )
    {
        if (string.IsNullOrWhiteSpace(
                task))
        {
            Console.Error.WriteLine(
                ConsoleTheme.Error(
                    "✗ Task must not be empty."
                )
            );

            return ExitCodes.Failure;
        }

        if (budget <= 0)
        {
            Console.Error.WriteLine(
                ConsoleTheme.Error(
                    "✗ Token budget must be greater than zero."
                )
            );

            return ExitCodes.Failure;
        }

        var project =
            services.ProjectLocator.Locate(
                Environment.CurrentDirectory
            );

        if (project is null)
        {
            Console.Error.WriteLine(
                ConsoleTheme.Error(
                    "✗ No project found."
                )
            );

            return ExitCodes.Failure;
        }

        ContextPackage package;

        using (ConsoleSpinner.Start(
                   "Mapping the terrain..."
               ))
        {
            package =
                services.ContextCompiler.Compile(
                    project,
                    new ContextRequest(
                        task,
                        budget
                    )
                );
        }

        if (content)
        {
            WritePackage(
                package
            );
        }

        WriteDiagnostics(
            package
        );

        if (explain)
        {
            WriteExplanation(
                package
            );
        }

        return ExitCodes.Success;
    }

    private static void WritePackage(
        ContextPackage package
    )
    {
        Console.WriteLine(
            $"<context task=\"{EscapeAttribute(package.Task)}\" project=\"{EscapeAttribute(package.ProjectName)}\">"
        );

        if (package.Guidance is not null)
        {
            Console.WriteLine(
                $"<guidance path=\"{EscapeAttribute(package.Guidance.Path)}\">"
            );

            WriteContent(
                package.Guidance.Content
            );

            Console.WriteLine(
                "</guidance>"
            );
        }

        foreach (var file in package.Files)
        {
            WriteFile(
                file
            );
        }
    }

    private static void WriteFile(
        ContextFile file
    )
    {
        switch (file.Kind)
        {
            case ContextFileKind.Complete:
                WriteCompleteFile(
                    file
                );

                break;

            case ContextFileKind.Excerpt:
                WriteExcerpt(
                    file
                );

                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported context file kind: {file.Kind}."
                );
        }
    }

    private static void WriteCompleteFile(
    ContextFile file
)
    {
        Console.WriteLine(
            $"<file path=\"{EscapeAttribute(file.Path)}\">"
        );

        WriteContent(
            file.Content
        );

        Console.WriteLine(
            "</file>"
        );
    }

    private static void WriteExcerpt(
    ContextFile file
)
    {
        if (file.StartLine is null
            || file.EndLine is null)
        {
            throw new InvalidOperationException(
                $"Excerpt '{file.Path}' does not have a valid line range."
            );
        }

        Console.WriteLine(
            $"<excerpt path=\"{EscapeAttribute(file.Path)}\" start-line=\"{file.StartLine}\" end-line=\"{file.EndLine}\">"
        );

        WriteContent(
            file.Content
        );

        Console.WriteLine(
            "</excerpt>"
        );
    }

    private static void WriteContent(
    string content
)
    {
        Console.Write(
            content
        );

        EnsureTrailingNewLine(
            content
        );
    }

    private static void WriteDiagnostics(
        ContextPackage package
    )
    {
        Console.Error.WriteLine();

        Console.Error.WriteLine(
            $"{ConsoleTheme.Waypoint} Context"
        );

        Console.Error.WriteLine(
            ConsoleTheme.Rule
        );

        Console.Error.WriteLine(
            $"  {package.Statistics.FileCount} files · ~{package.Statistics.EstimatedTokens:N0} / {package.Statistics.TokenBudget:N0} estimated tokens"
        );

        if (package.Guidance is not null)
        {
            Console.Error.WriteLine(
                $"  {ConsoleTheme.SuccessMark} guidance {package.Guidance.Path}"
            );
        }
    }

    private static void WriteExplanation(
        ContextPackage package
    )
    {
        Console.Error.WriteLine();

        Console.Error.WriteLine(
            ConsoleTheme.Bold(
                "Selection"
            )
        );

        Console.Error.WriteLine(
            ConsoleTheme.Rule
        );

        foreach (var selection in package.Selections)
        {
            var status =
                selection.Included
                    ? ConsoleTheme.SuccessMark
                    : ConsoleTheme.WarningMark;

            Console.Error.WriteLine(
                $"  {status} {selection.Score,3}  {selection.Path}"
            );

            if (selection.PathMatchedTerms.Count > 0)
            {
                Console.Error.WriteLine(
                    $"         filename: {string.Join(", ", selection.PathMatchedTerms)}"
                );
            }

            if (selection.ContentMatchedTerms.Count > 0)
            {
                Console.Error.WriteLine(
                    $"         content:  {string.Join(", ", selection.ContentMatchedTerms)}"
                );
            }
        }
    }

    private static void EnsureTrailingNewLine(
        string content
    )
    {
        if (content.EndsWith(
                '\n'))
        {
            return;
        }

        Console.WriteLine();
    }

    private static string EscapeAttribute(
        string value
    )
    {
        return value
            .Replace(
                "&",
                "&amp;",
                StringComparison.Ordinal
            )
            .Replace(
                "\"",
                "&quot;",
                StringComparison.Ordinal
            )
            .Replace(
                "<",
                "&lt;",
                StringComparison.Ordinal
            )
            .Replace(
                ">",
                "&gt;",
                StringComparison.Ordinal
            );
    }
}
