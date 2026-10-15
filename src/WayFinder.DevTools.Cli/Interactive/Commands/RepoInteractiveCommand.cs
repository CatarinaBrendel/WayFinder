using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Navigation;
using WayFinder.DevTools.Application.Repositories.Reading;
using WayFinder.DevTools.Application.Repositories.Relationships;
using WayFinder.DevTools.Application.Repositories.Symbols;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Cli.Projects;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class RepoInteractiveCommand : IInteractiveCommand, IInteractiveCommandCompletion, IInteractiveArgumentCompletion
{
    private readonly WayFinderServices _services;

    public string Name => "/repo";

    public string Description => "Read and search repository content";

    private readonly ProjectContextResolver _projectResolver;

    public RepoInteractiveCommand(WayFinderServices services)
    {
        _services = services;
        _projectResolver = new ProjectContextResolver(
            services.ProjectRegistry,
            services.ProjectLocator
        );
    }

    public IReadOnlyList<string> Subcommands { get; } =
    [
        "read",
        "search",
        "discover",
        "relationships",
        "symbols",
        "source"
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

            case "discover":
                Discover(arguments, output);
                break;

            case "relationships":
                Relationships(arguments, output);
                break;

            case "symbols":
                Symbols(arguments, output);
                break;

            case "source":
                Source(arguments, output);
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

    private void Discover(
        IReadOnlyList<string> arguments,
        TextWriter output)
    {
        if (arguments.Count > 2)
        {
            ConsolePresenter.Info(
                output,
                "Usage: /repo discover [project-name]"
            );

            return;
        }

        var projectName = arguments.Count == 2
            ? arguments[1]
            : null;

        ProjectContext? project;

        try
        {
            project = _projectResolver.Resolve(projectName);
        }
        catch (Exception exception) when (
            exception is KeyNotFoundException
            or InvalidOperationException
            or DirectoryNotFoundException)
        {
            ConsolePresenter.Error(output, exception.Message);
            return;
        }

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No Git project found in the current directory."
            );

            return;
        }

        var result = _services.RepositoryDiscoveryService.Discover(
            project
        );

        ConsolePresenter.Header(
            output,
            "Repository discovery"
        );

        ConsolePresenter.Property(
            output,
            "Solutions",
            result.Solutions.Count.ToString()
        );

        ConsolePresenter.Property(
            output,
            "Projects",
            result.Projects.Count.ToString()
        );

        if (result.Solutions.Count > 0)
        {
            ConsolePresenter.Subsection(
                output,
                "Solutions"
            );

            foreach (var solution in result.Solutions)
            {
                ConsolePresenter.Item(
                    output,
                    $"{solution.Name} ({solution.RelativePath})"
                );
            }
        }

        if (result.Projects.Count > 0)
        {
            ConsolePresenter.Subsection(
                output,
                "Projects"
            );

            foreach (var repositoryProject in result.Projects)
            {
                ConsolePresenter.Item(
                    output,
                    $"{repositoryProject.Name} ({repositoryProject.RelativePath})"
                );
            }
        }

        if (result.Solutions.Count == 0 &&
            result.Projects.Count == 0)
        {
            ConsolePresenter.Info(
                output,
                "No .NET solutions or projects found."
            );
        }
    }

    private void Relationships(
        IReadOnlyList<string> arguments,
        TextWriter output
    )
    {
        if (arguments.Count > 2)
        {
            ConsolePresenter.Info(
                output,
                "Usage: /repo relationships [project-name]");

            return;
        }

        var projectName = arguments.Count == 2
            ? arguments[1]
            : null;

        ProjectContext? project;

        try
        {
            project = _projectResolver.Resolve(projectName);
        }
        catch (Exception exception) when (
            exception is KeyNotFoundException
            or InvalidOperationException
            or DirectoryNotFoundException)
        {
            ConsolePresenter.Error(output, exception.Message);
            return;
        }

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No project found in the current directory.");

            return;
        }

        IReadOnlyList<RepositoryProjectRelationship> relationships;

        using (ConsoleSpinner.Start("Analyzing project dependencies..."))
        {
            relationships =
                _services.RepositoryRelationshipReader.Read(project);
        }

        ConsolePresenter.Header(output, "Repository relationships");

        if (relationships.Count == 0)
        {
            ConsolePresenter.Info(
                output,
                "No .NET project relationships found.");

            return;
        }

        foreach (var relationship in relationships)
        {
            ConsolePresenter.Subsection(
                output,
                relationship.ProjectPath);

            // Target frameworks
            ConsolePresenter.Property(
                output,
                "Frameworks",
                relationship.TargetFrameworks.Count > 0
                    ? string.Join(", ", relationship.TargetFrameworks)
                    : "Not specified");

            // Project references
            ConsolePresenter.Property(
                output,
                "Project references",
                relationship.References.Count.ToString());

            foreach (var reference in relationship.References)
            {
                var label = reference.IsExternal
                    ? $"{reference.Path} [external]"
                    : reference.Path;

                ConsolePresenter.Item(output, label);
            }

            // Assembly references
            ConsolePresenter.Property(
                output,
                "Assembly references",
                relationship.AssemblyReferences.Count.ToString());

            foreach (var assembly in relationship.AssemblyReferences)
            {
                ConsolePresenter.Item(output, assembly.Name);

                if (!string.IsNullOrWhiteSpace(assembly.HintPath))
                {
                    ConsolePresenter.Property(
                        output,
                        "  Hint path",
                        assembly.HintPath);
                }

                ConsolePresenter.Property(
                    output,
                    "  Copy local",
                    assembly.CopyLocal switch
                    {
                        true => "Yes",
                        false => "No",
                        null => "Unspecified"
                    });
            }
        }
    }

    private void Symbols(
        IReadOnlyList<string> arguments,
        TextWriter output)
    {
        if (arguments.Count is < 2 or > 3)
        {
            ConsolePresenter.Info(
                output,
                "Usage: /repo symbols [project-name] <relative-file-path>");

            return;
        }

        var projectName = arguments.Count == 3
            ? arguments[1]
            : null;

        var relativePath = arguments[^1];

        try
        {
            var project = _projectResolver.Resolve(projectName);

            if (project is null)
            {
                ConsolePresenter.Error(
                    output,
                    "No project found in the current directory.");

                return;
            }

            RepositorySymbolResult result;

            using (ConsoleSpinner.Start("Analyzing C# symbols..."))
            {
                result = _services.RepositorySymbolReader.Read(
                    project,
                    relativePath);
            }

            ConsolePresenter.Header(
                output,
                "C# symbols");

            ConsolePresenter.Property(
                output,
                "File",
                relativePath);

            ConsolePresenter.Property(
                output,
                "Symbols",
                result.Symbols.Count.ToString());

            if (result.Symbols.Count == 0)
            {
                ConsolePresenter.Info(
                    output,
                    "No supported C# declarations found.");

                return;
            }

            foreach (var symbol in result.Symbols)
            {
                ConsolePresenter.Item(
                    output,
                    $"{symbol.Line,5}  " +
                    $"{symbol.Kind,-12} " +
                    symbol.QualifiedName);
            }

            if (result.Truncated)
            {
                ConsolePresenter.Warning(
                    output,
                    "Symbol results were truncated.");
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or KeyNotFoundException
            or InvalidOperationException
            or DirectoryNotFoundException
            or FileNotFoundException
            or UnauthorizedAccessException
            or IOException)
        {
            ConsolePresenter.Error(
                output,
                exception.Message);
        }
    }

    private void Source(
        IReadOnlyList<string> arguments,
        TextWriter output)
    {
        if (arguments.Count < 2)
        {
            WriteSourceHelp(output);
            return;
        }

        var operation = arguments[1].ToLowerInvariant();

        try
        {
            switch (operation)
            {
                case "lines":
                    SourceLines(arguments, output);
                    break;

                case "declaration":
                    SourceDeclaration(arguments, output);
                    break;

                default:
                    WriteSourceHelp(output);
                    break;
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or KeyNotFoundException
            or InvalidOperationException
            or DirectoryNotFoundException
            or FileNotFoundException
            or UnauthorizedAccessException
            or IOException)
        {
            ConsolePresenter.Error(output, exception.Message);
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

        ConsolePresenter.Command(
            output,
            "/repo discover",
            "Discover .NET projects: /repo discover [project-name]"
        );

        ConsolePresenter.Command(
            output,
            "/repo relationships",
            "Inspect project and DLL references: /repo relationships [project-name]"
        );

        ConsolePresenter.Command(
            output,
            "/repo symbols",
            "List C# declarations: /repo symbols [project-name] <file>"
        );

        ConsolePresenter.Command(
            output,
            "/repo source",
            "Inspect source files: /repo source [project-name] <file>"
        );
    }

    public IReadOnlyList<string> CompleteArguments(
        IReadOnlyList<string> arguments,
        string prefix)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(prefix);

        if (arguments.Count != 1)
        {
            return [];
        }

        var subcommand = arguments[0];

        if (subcommand is not (
            "discover" or
            "relationships" or
            "symbols"))
        {
            return [];
        }

        return _services.ProjectRegistry
            .GetAll()
            .Select(project => project.Name)
            .Where(name =>
                name.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                name => name,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void SourceLines(
        IReadOnlyList<string> arguments,
        TextWriter output)
    {
        // /repo source lines <file> <start> <count>
        // /repo source lines <project> <file> <start> <count>

        if (arguments.Count is not (5 or 6))
        {
            WriteSourceHelp(output);
            return;
        }

        var hasProjectName = arguments.Count == 6;

        var projectName = hasProjectName
            ? arguments[2]
            : null;

        var relativePath = arguments[hasProjectName ? 3 : 2];

        if (!int.TryParse(arguments[^2], out var startLine) ||
            !int.TryParse(arguments[^1], out var lineCount) ||
            startLine <= 0 ||
            lineCount <= 0)
        {
            ConsolePresenter.Error(
                output,
                "Start line and line count must be positive integers.");

            return;
        }

        var project = _projectResolver.Resolve(projectName);

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No project found in the current directory.");

            return;
        }

        RepositorySourceExcerpt excerpt;

        using (ConsoleSpinner.Start("Reading source lines..."))
        {
            excerpt = _services.RepositorySourceNavigator.ReadLines(
                project,
                relativePath,
                startLine,
                lineCount);
        }

        WriteSourceExcerpt(output, excerpt);
    }

    private void SourceDeclaration(
        IReadOnlyList<string> arguments,
        TextWriter output)
    {
        // /repo source declaration <file> <qualified-name>
        // /repo source declaration <project> <file> <qualified-name>

        if (arguments.Count is not (4 or 5))
        {
            WriteSourceHelp(output);
            return;
        }

        var hasProjectName = arguments.Count == 5;

        var projectName = hasProjectName
            ? arguments[2]
            : null;

        var relativePath = arguments[hasProjectName ? 3 : 2];

        var qualifiedName = arguments[^1];

        var project = _projectResolver.Resolve(projectName);

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No project found in the current directory.");

            return;
        }

        RepositorySourceExcerpt excerpt;

        using (ConsoleSpinner.Start("Locating C# declaration..."))
        {
            excerpt = _services.RepositorySourceNavigator.ReadDeclaration(
                project,
                relativePath,
                qualifiedName);
        }

        WriteSourceExcerpt(output, excerpt);
    }

    private static void WriteSourceExcerpt(
        TextWriter output,
        RepositorySourceExcerpt excerpt)
    {
        ConsolePresenter.Header(output, "Source navigation");

        ConsolePresenter.Property(
            output,
            "File",
            excerpt.RelativePath);

        ConsolePresenter.Property(
            output,
            "Lines",
            $"{excerpt.StartLine}-{excerpt.EndLine}");

        if (string.IsNullOrEmpty(excerpt.Content))
        {
            ConsolePresenter.Info(
                output,
                "No source lines found.");

            return;
        }

        output.WriteLine();

        var lines = excerpt.Content.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = excerpt.StartLine + index;

            output.WriteLine(
                $"    {lineNumber,5} │ {lines[index]}");
        }

        if (excerpt.Truncated)
        {
            ConsolePresenter.Warning(
                output,
                "Source output was truncated to the configured limit.");
        }
    }

    private static void WriteSourceHelp(TextWriter output)
    {
        ConsolePresenter.Info(
            output,
            "Usage:");

        ConsolePresenter.Text(
            output,
            "/repo source lines [project-name] <file> <start-line> <line-count>");

        ConsolePresenter.Text(
            output,
            "/repo source declaration [project-name] <file> <qualified-name>");
    }

}
