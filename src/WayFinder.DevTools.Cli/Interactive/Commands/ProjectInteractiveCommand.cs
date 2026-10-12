using WayFinder.DevTools.Application.Projects.Registry;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Manifest;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Projects.Initialization;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class ProjectInteractiveCommand : IInteractiveCommand, IInteractiveCommandCompletion
{
    private readonly IProjectRegistry _projectRegistry;
    private readonly IProjectLocator _projectLocator;
    private readonly IProjectManifestReader _manifestReader;
    private readonly IProjectInspector _projectInspector;
    private readonly TextReader _input;
    private readonly IProjectFileSystem _projectFileSystem;
    private readonly IProjectInitializer _projectInitializer;

    public ProjectInteractiveCommand(
        IProjectRegistry projectRegistry,
        IProjectLocator projectLocator,
        IProjectManifestReader manifestReader,
        IProjectInspector projectInspector,
        IProjectFileSystem projectFileSystem,
        IProjectInitializer projectInitializer,
        TextReader input
    )
    {
        ArgumentNullException.ThrowIfNull(projectRegistry);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(manifestReader);
        ArgumentNullException.ThrowIfNull(projectInspector);
        ArgumentNullException.ThrowIfNull(projectFileSystem);
        ArgumentNullException.ThrowIfNull(projectInitializer);
        ArgumentNullException.ThrowIfNull(input);

        _projectRegistry = projectRegistry;
        _projectLocator = projectLocator;
        _manifestReader = manifestReader;
        _projectInspector = projectInspector;
        _projectFileSystem = projectFileSystem;
        _projectInitializer = projectInitializer;
        _input = input;
    }

    public string Name => "/project";

    public string Description => "Inspect and manage projects";

    public IReadOnlyList<string> Subcommands => new[]
    {
        "list",
        "info",
        "init",
        "add",
        "remove"
    };

    public async Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (arguments.Count == 0)
        {
            WriteHelp(output);
            return InteractiveCommandResult.Continue;
        }

        switch (arguments[0].ToLowerInvariant())
        {
            case "list":
                if (arguments.Count != 1)
                    ConsolePresenter.Info(output, "Usage: /project list");
                else
                    ListProjects(output);
                break;

            case "info":
                if (arguments.Count != 1)
                    ConsolePresenter.Info(output, "Usage: /project info");
                else
                    ShowProjectInfo(output);
                break;

            case "init":
                if (arguments.Count != 1)
                    ConsolePresenter.Info(output, "Usage: /project init");
                else
                    await InitializeProjectAsync(output, cancellationToken);
                break;

            case "add":
                if (arguments.Count == 1)
                {
                    ConsolePresenter.Error(output, "A project path is required.");
                }
                else if (arguments.Count != 2)
                {
                    ConsolePresenter.Info(output, "Usage: /project add <path>");
                }
                else
                {
                    AddProject(arguments[1], output);
                }
                break;

            case "remove":
                RemoveProject(arguments, output);
                break;

            default:
                ConsolePresenter.Error(
                    output,
                    $"Unknown project command: {arguments[0]}"
                );
                WriteHelp(output);
                break;
        }

        return InteractiveCommandResult.Continue;
    }

    private void ListProjects(TextWriter output)
    {
        var projects = _projectRegistry.GetAll();

        ConsolePresenter.Header(output, "Registered projects");

        if (projects.Count == 0)
        {
            ConsolePresenter.Info(output, "No waypoints yet.");

            ConsolePresenter.Text(
                output,
                $"Register one with {ConsoleTheme.Bold("/project add <path>")}."
            );

            return;
        }

        foreach (var project in projects)
        {
            ConsolePresenter.Subsection(output, project.Name);

            ConsolePresenter.Property(
                output,
                "ID",
                project.Id.ToString()
            );

            ConsolePresenter.Property(
                output,
                "Root",
                project.RootPath
            );

            ConsolePresenter.Property(
                output,
                "Status",
                GetStatus(project.RootPath)
            );
        }

        ConsolePresenter.Info(
            output,
            projects.Count == 1
                ? "1 project registered"
                : $"{projects.Count} projects registered"
        );
    }

    private static string GetStatus(string rootPath)
    {
        return Directory.Exists(rootPath)
            ? $"{ConsoleTheme.SuccessMark} {ConsoleTheme.Success("available")}"
            : $"{ConsoleTheme.WarningMark} {ConsoleTheme.Warning("missing")}";
    }

    private void ShowProjectInfo(TextWriter output)
    {
        var project = _projectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            ConsolePresenter.Error(output, "No project found.");
            return;
        }

        var manifest = _manifestReader.Read(project);
        var inspection = _projectInspector.Inspect(project);

        ProjectInfoPresentation.Write(
            output,
            project,
            manifest,
            inspection
        );
    }

    private void AddProject(
        string path,
        TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            ConsolePresenter.Error(
                output,
                "A project path is required."
            );

            return;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            var project = _projectLocator.Locate(fullPath);

            if (project is null)
            {
                ConsolePresenter.Error(output, "No project found.");
                return;
            }

            var existing = _projectRegistry.FindByRootPath(
                project.RootPath
            );

            if (existing is not null)
            {
                ConsolePresenter.Success(
                    output,
                    $"{ConsoleTheme.Bold(existing.Name)} is already registered"
                );

                ConsolePresenter.Property(
                    output,
                    "Root",
                    existing.RootPath
                );

                ConsolePresenter.Info(
                    output,
                    "Nothing to change. This waypoint is already known."
                );

                return;
            }

            var registered = _projectRegistry.Add(project);

            ConsolePresenter.Success(
                output,
                $"Registered {ConsoleTheme.Bold(registered.Name)}"
            );

            ConsolePresenter.Property(
                output,
                "Root",
                registered.RootPath
            );

            ConsolePresenter.Subsection(output, "AI visibility");

            ConsolePresenter.Property(
                output,
                "Read",
                $"{ConsoleTheme.SuccessMark} {ConsoleTheme.Success("granted")}"
            );

            ConsolePresenter.Property(
                output,
                "Write",
                $"{ConsoleTheme.WarningMark} {ConsoleTheme.Warning("not granted")}"
            );

            ConsolePresenter.Info(
                output,
                "WayFinder can show AI adapters the route, but they can't change it."
            );
        }
        catch (DirectoryNotFoundException)
        {
            ConsolePresenter.Error(
                output,
                $"Path does not exist: {path}"
            );
        }
    }

    private async Task InitializeProjectAsync(
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var project = _projectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            ConsolePresenter.Error(output, "No project found.");
            return;
        }

        if (_projectFileSystem.FileExists(project, "wayfinder.json"))
        {
            ConsolePresenter.Success(
                output,
                $"{ConsoleTheme.Bold(project.Name)} is already configured"
            );

            ConsolePresenter.Text(
                output,
                "wayfinder.json already exists."
            );

            ConsolePresenter.Info(
                output,
                "Nothing to change. This route is already mapped."
            );

            return;
        }

        var initialization = _projectInitializer.Prepare(project);

        WriteInitializationPreview(
            output,
            project.RootPath,
            initialization
        );

        output.WriteLine();

        ConsolePresenter.Confirm(
            output,
            "Create this configuration?"
        );

        await output.FlushAsync(cancellationToken);

        var answer = await _input.ReadLineAsync(cancellationToken);

        if (!IsConfirmed(answer))
        {
            output.WriteLine();

            ConsolePresenter.Info(
                output,
                "No changes made."
            );

            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        _projectInitializer.Initialize(project, initialization);

        output.WriteLine();

        ConsolePresenter.Success(
            output,
            $"Created {ConsoleTheme.Bold("wayfinder.json")}"
        );

        ConsolePresenter.Info(
            output,
            "WayFinder knows this route now."
        );
    }

    private static void WriteInitializationPreview(
        TextWriter output,
        string projectRoot,
        ProjectInitialization initialization)
    {
        ConsolePresenter.Header(output, "Project setup");

        ConsolePresenter.Property(
            output,
            "Name",
            initialization.ProjectName
        );

        ConsolePresenter.Property(
            output,
            "Manifest",
            Path.Combine(
                projectRoot,
                initialization.ManifestPath
            )
        );

        ConsolePresenter.Subsection(
            output,
            "Detected technologies"
        );

        if (initialization.Technologies.Count == 0)
        {
            ConsolePresenter.Info(output, "None detected.");
            return;
        }

        foreach (var technology in initialization.Technologies)
        {
            ConsolePresenter.Success(output, technology);
        }
    }

    private static bool IsConfirmed(string? answer)
    {
        return string.Equals(
                   answer,
                   "y",
                   StringComparison.OrdinalIgnoreCase
               )
               || string.Equals(
                   answer,
                   "yes",
                   StringComparison.OrdinalIgnoreCase
               );
    }

    private void RemoveProject(
        IReadOnlyList<string> arguments,
        TextWriter output)
    {
        if (arguments.Count != 2 ||
            !Guid.TryParse(arguments[1], out var id) ||
            id == Guid.Empty)
        {
            ConsolePresenter.Info(
                output,
                "Usage: /project remove <id>"
            );

            return;
        }

        var project = _projectRegistry.FindById(id);

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                $"No registered project found with ID '{id}'."
            );

            return;
        }

        if (!_projectRegistry.Remove(id))
        {
            ConsolePresenter.Error(
                output,
                $"Could not remove registered project '{id}'."
            );

            return;
        }

        ConsolePresenter.Success(
            output,
            $"Removed {ConsoleTheme.Bold(project.Name)}"
        );

        ConsolePresenter.Property(
            output,
            "Root",
            project.RootPath
        );

        ConsolePresenter.Subsection(output, "AI visibility");

        ConsolePresenter.Property(
            output,
            "Read",
            $"{ConsoleTheme.WarningMark} {ConsoleTheme.Warning("revoked")}"
        );

        ConsolePresenter.Info(
            output,
            "This waypoint is no longer visible to AI adapters."
        );
    }

    private static void WriteHelp(TextWriter output)
    {
        ConsolePresenter.Header(output, "Project commands");

        ConsolePresenter.Text(
            output,
            "Usage: /project <command>"
        );

        output.WriteLine();

        ConsolePresenter.Command(
            output,
            "/project list",
            "List registered projects"
        );

        ConsolePresenter.Command(
            output,
            "/project info",
            "Inspect the current project"
        );

        ConsolePresenter.Command(
            output,
            "/project init",
            "Initialize WayFinder configuration"
        );

        ConsolePresenter.Command(
            output,
            "/project add",
            "Register a project: /project add <path>"
        );

        ConsolePresenter.Command(
            output,
            "/project remove",
            "Remove a project: /project remove <id>"
        );
    }
}
