using System.Text.Json;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Initialization;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Projects.Initialization;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Initialization;

public sealed class ProjectInitializerTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly ProjectFileSystem _fileSystem = new();

    public ProjectInitializerTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-tests-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);

        _project = new ProjectContext(
            Name: "TestProject",
            RootPath: _root,
            IsGitRepository: true
        );
    }

    [Fact]
    public void Prepare_ReturnsDetectedTechnologies()
    {
        var initializer = CreateInitializer(
            new FakeDetector("node", detected: true),
            new FakeDetector("dotnet", detected: true),
            new FakeDetector("swift", detected: false)
        );

        var initialization = initializer.Prepare(_project);

        Assert.Equal("wayfinder.json", initialization.ManifestPath);
        Assert.Equal("TestProject", initialization.ProjectName);

        Assert.Equal(
            [
                "dotnet",
                "node",
            ],
            initialization.Technologies
        );
    }

    [Fact]
    public void Prepare_ExcludesGuidance()
    {
        var initializer = CreateInitializer(
            new FakeDetector("dotnet", detected: true),
            new FakeDetector("guidance", detected: true)
        );

        var initialization = initializer.Prepare(_project);

        Assert.Equal(
            ["dotnet"],
            initialization.Technologies
        );
    }

    [Fact]
    public void Prepare_AllowsNoDetectedTechnologies()
    {
        var initializer = CreateInitializer(
            new FakeDetector("dotnet", detected: false)
        );

        var initialization = initializer.Prepare(_project);

        Assert.Empty(initialization.Technologies);
    }

    [Fact]
    public void Initialize_CreatesManifest()
    {
        var initializer = CreateInitializer();

        var initialization = new Application.Projects.Initialization.ProjectInitialization(
            ManifestPath: "wayfinder.json",
            ProjectName: "TestProject",
            Technologies:
            [
                "dotnet",
                "node",
            ]
        );

        initializer.Initialize(
            _project,
            initialization
        );

        var path = Path.Combine(
            _root,
            "wayfinder.json"
        );

        Assert.True(File.Exists(path));

        using var document =
            JsonDocument.Parse(File.ReadAllText(path));

        var root = document.RootElement;

        Assert.Equal(
            1,
            root.GetProperty("version").GetInt32()
        );

        Assert.Equal(
            "TestProject",
            root.GetProperty("name").GetString()
        );

        var technologies = root
            .GetProperty("technologies")
            .EnumerateArray()
            .Select(
                element =>
                    element.GetString()
                    ?? throw new InvalidDataException(
                        "Technology ID cannot be null."
                    )
            )
            .ToArray();

        Assert.Equal(
            [
                "dotnet",
                "node",
            ],
            technologies
        );

        Assert.Equal(
            [
                "dotnet",
                "node",
            ],
            technologies
        );
    }

    [Fact]
    public void Initialize_WhenManifestExists_RejectsOverwrite()
    {
        File.WriteAllText(
            Path.Combine(_root, "wayfinder.json"),
            "{}"
        );

        var initializer = CreateInitializer();

        var initialization = new Application.Projects.Initialization.ProjectInitialization(
            ManifestPath: "wayfinder.json",
            ProjectName: "TestProject",
            Technologies: []
        );

        Assert.Throws<InvalidOperationException>(
            () => initializer.Initialize(
                _project,
                initialization
            )
        );
    }

    private ProjectInitializer CreateInitializer(
        params IProjectDetector[] detectors
    )
    {
        var inspector =
            new ProjectInspector(detectors);

        return new ProjectInitializer(
            inspector,
            _fileSystem
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(
                _root,
                recursive: true
            );
        }
    }

    private sealed class FakeDetector(
        string name,
        bool detected
    ) : IProjectDetector
    {
        public string Name => name;

        public IReadOnlyCollection<ProjectArtifact> Detect(
            ProjectContext project
        )
        {
            return detected
                ?
                [
                    new ProjectArtifact(
                        Type: "test",
                        Path: "test.file"
                    ),
                ]
                : [];
        }
    }
}
