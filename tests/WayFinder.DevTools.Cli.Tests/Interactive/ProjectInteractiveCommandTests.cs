using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registry;
using WayFinder.DevTools.Cli.Interactive;
using WayFinder.DevTools.Cli.Interactive.Commands;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Manifest;
using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Projects.Initialization;
using Microsoft.Win32;

namespace WayFinder.DevTools.Tests.Cli;

public sealed class ProjectInteractiveCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WithoutArguments_DisplaysUsage()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync([], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains("Usage: /project <command>", output.ToString());
        Assert.Contains("list", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ListWithEmptyRegistry_DisplaysNoProjects()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["list"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains("Registered projects", output.ToString());
        Assert.Contains("No waypoints yet.", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ListWithProjects_DisplaysProjectDetails()
    {
        var project = new RegisteredProject(
            Guid.NewGuid(),
            "MyProject",
            Path.GetTempPath()
        );

        var command = CreateCommand(project);

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["list"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);

        var text = output.ToString();

        Assert.Contains("MyProject", text);
        Assert.Contains(project.Id.ToString(), text);
        Assert.Contains(project.RootPath, text);
        Assert.Contains("available", text);
        Assert.Contains("1 project registered", text);
    }

    [Fact]
    public async Task ExecuteAsync_ListWithMissingDirectory_DisplaysMissingStatus()
    {
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-test-{Guid.NewGuid():N}"
        );

        var project = new RegisteredProject(
            Guid.NewGuid(),
            "MissingProject",
            missingPath
        );

        var command = CreateCommand(project);

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["list"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);

        var text = output.ToString();

        Assert.Contains("MissingProject", text);
        Assert.Contains("missing", text);
    }

    [Fact]
    public async Task ExecuteAsync_List_IsCaseInsensitive()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["LIST"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains("Registered projects", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_UnknownSubcommand_DisplaysError()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["invalid"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains(
            "Unknown project command: invalid",
            output.ToString()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ListWithExtraArguments_DisplaysUsageError()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(
            ["list", "unexpected"],
            output
        );

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains("Usage: /project list", output.ToString());
        Assert.DoesNotContain("Registered projects", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_InfoWithCurrentProject_DisplaysProjectDetails()
    {
        var project = new ProjectContext(
            "WayFinder.DevTools",
            Path.GetTempPath(),
            true
        );

        var locator = new FakeProjectLocator
        {
            Project = project
        };

        var command = new ProjectInteractiveCommand(
            new FakeProjectRegistry([]),
            locator,
            new FakeProjectManifestReader(),
            new FakeProjectInspector(),
            new FakeProjectFileSystem(),
            new FakeProjectInitializer(),
            new StringReader("")
        );

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["info"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);

        var text = output.ToString();

        Assert.Contains("WayFinder.DevTools", text);
        Assert.Contains(project.RootPath, text);
        Assert.Contains("Git", text);
        Assert.Contains("yes", text);

        Assert.Equal(
            Environment.CurrentDirectory,
            locator.LastStartPath
        );
    }

    [Fact]
    public async Task ExecuteAsync_InfoWithoutProject_DisplaysError()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["info"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains("No project found.", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_InfoWithExtraArguments_DisplaysUsageError()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(
            ["info", "unexpected"],
            output
        );

        Assert.Equal(InteractiveCommandResult.Continue, result);

        Assert.Contains(
            "Usage: /project info",
            output.ToString()
        );

        Assert.DoesNotContain(
            "No project found.",
            output.ToString()
        );
    }

    [Fact]
    public async Task ExecuteAsync_Info_IsCaseInsensitive()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["INFO"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains("No project found.", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_InfoWithManifest_DisplaysDeclaredTechnologies()
    {
        var project = new ProjectContext(
            "TestProject",
            Path.GetTempPath(),
            true
        );

        var manifest = new ProjectManifest(
            1,
            "TestProject",
            ["dotnet", "typescript"]
        );

        var manifestReader = new FakeProjectManifestReader
        {
            Manifest = manifest
        };

        var command = new ProjectInteractiveCommand(
            new FakeProjectRegistry([]),
            new FakeProjectLocator { Project = project },
            manifestReader,
            new FakeProjectInspector(),
            new FakeProjectFileSystem(),
            new FakeProjectInitializer(),
            new StringReader("")
        );

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["info"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);

        var text = output.ToString();

        Assert.Contains("Declared", text);
        Assert.Contains("dotnet", text);
        Assert.Contains("typescript", text);
        Assert.Contains("configured", text);

        Assert.Equal(project, manifestReader.LastProject);
    }

    [Fact]
    public async Task ExecuteAsync_InfoWithoutManifest_DisplaysConfigurationHint()
    {
        var project = new ProjectContext(
            "TestProject",
            Path.GetTempPath(),
            false
        );

        var command = new ProjectInteractiveCommand(
            new FakeProjectRegistry([]),
            new FakeProjectLocator { Project = project },
            new FakeProjectManifestReader(),
            new FakeProjectInspector(),
            new FakeProjectFileSystem(),
            new FakeProjectInitializer(),
            new StringReader("")
        );

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["info"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);

        var text = output.ToString();

        Assert.Contains("not configured", text);
        Assert.Contains("/project init", text);
    }

    [Fact]
    public async Task ExecuteAsync_AddWithoutPath_DisplaysError()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(["add"], output);

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains("A project path is required.", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_AddWithValidProject_RegistersProject()
    {
        var project = new ProjectContext(
            "TestProject",
            Path.GetTempPath(),
            true
        );

        var registry = new FakeProjectRegistry([]);
        var locator = new FakeProjectLocator { Project = project };

        var command = new ProjectInteractiveCommand(
            registry,
            locator,
            new FakeProjectManifestReader(),
            new FakeProjectInspector(),
            new FakeProjectFileSystem(),
            new FakeProjectInitializer(),
            new StringReader("")
        );

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(
            ["add", project.RootPath],
            output
        );

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Equal(1, registry.AddCallCount);

        Assert.NotNull(
            registry.FindByRootPath(project.RootPath)
        );

        var text = output.ToString();

        Assert.Contains("Registered TestProject", text);
        Assert.Contains("AI visibility", text);
        Assert.Contains("not granted", text);
    }

    [Fact]
    public async Task ExecuteAsync_AddAlreadyRegistered_DoesNotRegisterAgain()
    {
        var project = new ProjectContext(
            "TestProject",
            Path.GetTempPath(),
            true
        );

        var registered = new RegisteredProject(
            Guid.NewGuid(),
            project.Name,
            project.RootPath
        );

        var registry = new FakeProjectRegistry([registered]);

        var command = new ProjectInteractiveCommand(
            registry,
            new FakeProjectLocator { Project = project },
            new FakeProjectManifestReader(),
            new FakeProjectInspector(),
            new FakeProjectFileSystem(),
            new FakeProjectInitializer(),
            new StringReader("")
        );

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(
            ["add", project.RootPath],
            output
        );

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Equal(0, registry.AddCallCount);

        Assert.Contains(
            "already registered",
            output.ToString()
        );
    }

    [Fact]
    public async Task ExecuteAsync_AddWithoutDiscoverableProject_DisplaysError()
    {
        var locator = new FakeProjectLocator();

        var registry = new FakeProjectRegistry([]);

        var command = new ProjectInteractiveCommand(
            registry,
            locator,
            new FakeProjectManifestReader(),
            new FakeProjectInspector(),
            new FakeProjectFileSystem(),
            new FakeProjectInitializer(),
            new StringReader("")
        );

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(
            ["add", Path.GetTempPath()],
            output
        );

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains("No project found.", output.ToString());
        Assert.Equal(0, registry.AddCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_Add_IsCaseInsensitive()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(
            ["ADD"],
            output
        );

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains(
            "A project path is required.",
            output.ToString()
        );
    }

    [Fact]
    public async Task ExecuteAsync_AddWithExtraArguments_DisplaysUsageError()
    {
        var command = CreateCommand();

        using var output = new StringWriter();

        var result = await command.ExecuteAsync(
            ["add", "/some/path", "unexpected"],
            output
        );

        Assert.Equal(InteractiveCommandResult.Continue, result);
        Assert.Contains("Usage: /project add <path>", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_InitWithExistingManifest_SkipsInitialization()
    {
        var project = new ProjectContext(
            "TestProject",
            Path.GetTempPath(),
            true
        );

        var fileSystem = new FakeProjectFileSystem
        {
            ManifestExists = true
        };

        var initializer = new FakeProjectInitializer();

        var command = new ProjectInteractiveCommand(
            new FakeProjectRegistry([]),
            new FakeProjectLocator { Project = project },
            new FakeProjectManifestReader(),
            new FakeProjectInspector(),
            fileSystem,
            initializer,
            new StringReader("y\n")
        );

        using var output = new StringWriter();

        await command.ExecuteAsync(["init"], output);

        Assert.Contains("already configured", output.ToString());
        Assert.Equal(0, initializer.InitializeCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_InitWithoutProject_DisplaysError()
    {
        var initializer = new FakeProjectInitializer();

        var command = new ProjectInteractiveCommand(
            new FakeProjectRegistry([]),
            new FakeProjectLocator(),
            new FakeProjectManifestReader(),
            new FakeProjectInspector(),
            new FakeProjectFileSystem(),
            initializer,
            new StringReader("y\n")
        );

        using var output = new StringWriter();

        await command.ExecuteAsync(["init"], output);

        Assert.Contains("No project found.", output.ToString());
        Assert.Equal(0, initializer.InitializeCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_RemoveExistingProject_RemovesRegistration()
    {
        var project = new RegisteredProject(
            Guid.NewGuid(),
            "TestProject",
            Path.GetTempPath()
        );

        var registry = new FakeProjectRegistry([project]);

        var command = new ProjectInteractiveCommand(
            registry,
            new FakeProjectLocator(),
            new FakeProjectManifestReader(),
            new FakeProjectInspector(),
            new FakeProjectFileSystem(),
            new FakeProjectInitializer(),
            new StringReader("")
        );

        using var output = new StringWriter();

        await command.ExecuteAsync(
            ["remove", project.Id.ToString()],
            output
        );

        Assert.Null(registry.FindById(project.Id));

        Assert.Contains(
            "Removed TestProject",
            output.ToString()
        );
    }

    private static ProjectInteractiveCommand CreateCommand(params RegisteredProject[] projects)
    {
        return new ProjectInteractiveCommand(
            new FakeProjectRegistry(projects),
            new FakeProjectLocator(),
            new FakeProjectManifestReader(),
            new FakeProjectInspector(),
            new FakeProjectFileSystem(),
            new FakeProjectInitializer(),
            new StringReader("")
        );
    }

    private sealed class FakeProjectRegistry : IProjectRegistry
    {
        private readonly List<RegisteredProject> _projects;
        public int AddCallCount { get; private set; }

        public FakeProjectRegistry(IEnumerable<RegisteredProject> projects)
        {
            _projects = projects.ToList();
        }

        public IReadOnlyCollection<RegisteredProject> GetAll()
        {
            return _projects.AsReadOnly();
        }

        public RegisteredProject? FindById(Guid id)
        {
            return _projects.FirstOrDefault(
                project => project.Id == id
            );
        }

        public RegisteredProject? FindByRootPath(string rootPath)
        {
            return _projects.FirstOrDefault(
                project => project.RootPath == rootPath
            );
        }

        public RegisteredProject Add(ProjectContext project)
        {
            AddCallCount++;

            var registeredProject = new RegisteredProject(
                Guid.NewGuid(),
                project.Name,
                project.RootPath
            );

            _projects.Add(registeredProject);

            return registeredProject;
        }

        public bool Remove(Guid id)
        {
            return _projects.RemoveAll(
                project => project.Id == id
            ) > 0;
        }
    }

    private sealed class FakeProjectLocator : IProjectLocator
    {
        public ProjectContext? Project { get; set; }

        public string? LastStartPath { get; private set; }

        public ProjectContext? Locate(string startPath)
        {
            LastStartPath = startPath;
            return Project;
        }
    }

    private sealed class FakeProjectManifestReader : IProjectManifestReader
    {
        public ProjectManifest? Manifest { get; set; }

        public ProjectContext? LastProject { get; private set; }

        public ProjectManifest? Read(ProjectContext project)
        {
            LastProject = project;
            return Manifest;
        }
    }

    private sealed class FakeProjectInspector : IProjectInspector
    {
        public ProjectContext? LastProject { get; private set; }

        public ProjectInspection Inspect(ProjectContext project)
        {
            LastProject = project;

            return new ProjectInspection(
                project,
                new Dictionary<string, IReadOnlyCollection<ProjectArtifact>>()
            );
        }
    }

    private sealed class FakeProjectFileSystem : IProjectFileSystem
    {
        public bool ManifestExists { get; set; }

        public bool FileExists(
            ProjectContext project,
            string relativePath)
        {
            return relativePath == "wayfinder.json" && ManifestExists;
        }

        public IReadOnlyCollection<ProjectFile> FindFiles(
            ProjectContext project,
            string searchPattern)
            => throw new NotSupportedException();

        public string ReadAllText(
            ProjectContext project,
            string relativePath)
            => throw new NotSupportedException();

        public void WriteAllText(
            ProjectContext project,
            string relativePath,
            string content)
            => throw new NotSupportedException();

        public ProjectFileRead Read(
            ProjectContext project,
            string relativePath,
            int maxBytes)
            => throw new NotSupportedException();

        public ProjectFileRead Read(
            ProjectContext project,
            string relativePath,
            long offset,
            int maxBytes)
            => throw new NotSupportedException();

        public IReadOnlyCollection<ProjectFile> GetFiles(
            ProjectContext project)
            => throw new NotSupportedException();

        public IReadOnlyCollection<ProjectDirectoryEntry> GetEntries(
            ProjectContext project,
            string relativePath)
            => throw new NotSupportedException();
    }

    private sealed class FakeProjectInitializer : IProjectInitializer
    {
        public ProjectInitialization? Initialization { get; set; }

        public int InitializeCallCount { get; private set; }

        public ProjectInitialization Prepare(ProjectContext project)
        {
            return Initialization
                ?? throw new InvalidOperationException(
                    "No test initialization configured."
                );
        }

        public void Initialize(
            ProjectContext project,
            ProjectInitialization initialization)
        {
            InitializeCallCount++;
        }
    }
}
