using System.Text.Json;
using WayFinder.DevTools.Application.Configuration;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Registry;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Registry;

public sealed class JsonProjectRegistryTests : IDisposable
{
    private readonly string _directory;
    private readonly TestWayFinderEnvironment _environment;
    private readonly JsonProjectRegistry _registry;

    public JsonProjectRegistryTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-tests-{Guid.NewGuid():N}"
        );

        _environment =
            new TestWayFinderEnvironment(_directory);

        _registry =
            new JsonProjectRegistry(_environment);
    }

    [Fact]
    public void GetAll_WhenRegistryDoesNotExist_ReturnsEmptyCollection()
    {
        var projects = _registry.GetAll();

        Assert.Empty(projects);
    }

    [Fact]
    public void Add_CreatesRegistry()
    {
        var project = CreateProject("DeadRoute");

        _registry.Add(project);

        Assert.True(
            File.Exists(_environment.RegistryPath)
        );

        Assert.False(
            File.Exists(
                $"{_environment.RegistryPath}.tmp"
            )
        );
    }

    [Fact]
    public void Add_AssignsNonEmptyId()
    {
        var project = CreateProject("DeadRoute");

        var registered =
            _registry.Add(project);

        Assert.NotEqual(
            Guid.Empty,
            registered.Id
        );
    }

    [Fact]
    public void Add_StoresCanonicalAbsoluteRootPath()
    {
        var projectRoot = Path.Combine(
            _directory,
            "Projects",
            "..",
            "DeadRoute"
        );

        var project =
            new ProjectContext(
                Name: "DeadRoute",
                RootPath: projectRoot,
                IsGitRepository: true
            );

        var registered =
            _registry.Add(project);

        Assert.Equal(
            Path.GetFullPath(projectRoot)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                ),
            registered.RootPath
        );
    }

    [Fact]
    public void Add_WhenRootAlreadyRegistered_ReturnsExistingRegistration()
    {
        var project = CreateProject("DeadRoute");

        var first =
            _registry.Add(project);

        var second =
            _registry.Add(project);

        Assert.Equal(
            first,
            second
        );

        Assert.Single(
            _registry.GetAll()
        );
    }

    [Fact]
    public void Add_AllowsSameNameForDifferentRoots()
    {
        var firstProject =
            CreateProject(
                "DeadRoute",
                "DeadRoute-One"
            );

        var secondProject =
            CreateProject(
                "DeadRoute",
                "DeadRoute-Two"
            );

        _registry.Add(firstProject);
        _registry.Add(secondProject);

        var projects =
            _registry.GetAll();

        Assert.Equal(
            2,
            projects.Count
        );
    }

    [Fact]
    public void FindById_ReturnsRegisteredProject()
    {
        var registered =
            _registry.Add(
                CreateProject("DeadRoute")
            );

        var found =
            _registry.FindById(
                registered.Id
            );

        Assert.Equal(
            registered,
            found
        );
    }

    [Fact]
    public void FindByRootPath_ReturnsRegisteredProject()
    {
        var project =
            CreateProject("DeadRoute");

        var registered =
            _registry.Add(project);

        var found =
            _registry.FindByRootPath(
                project.RootPath
            );

        Assert.Equal(
            registered,
            found
        );
    }

    [Fact]
    public void Remove_WhenProjectExists_RemovesProject()
    {
        var registered =
            _registry.Add(
                CreateProject("DeadRoute")
            );

        var removed =
            _registry.Remove(
                registered.Id
            );

        Assert.True(removed);
        Assert.Empty(
            _registry.GetAll()
        );
    }

    [Fact]
    public void Remove_WhenProjectDoesNotExist_ReturnsFalse()
    {
        var removed =
            _registry.Remove(
                Guid.NewGuid()
            );

        Assert.False(removed);
    }

    [Fact]
    public void GetAll_WhenVersionIsUnsupported_ThrowsInvalidDataException()
    {
        WriteRegistry(
            """
            {
              "version": 999,
              "projects": []
            }
            """
        );

        Assert.Throws<InvalidDataException>(
            () => _registry.GetAll()
        );
    }

    [Fact]
    public void GetAll_WhenJsonIsMalformed_ThrowsInvalidDataException()
    {
        WriteRegistry(
            """
            {
              "version": 1,
              "projects":
            """
        );

        Assert.Throws<InvalidDataException>(
            () => _registry.GetAll()
        );
    }

    [Fact]
    public void GetAll_WhenProjectIdsAreDuplicated_ThrowsInvalidDataException()
    {
        var id = Guid.NewGuid();

        WriteRegistry(
            $$"""
            {
              "version": 1,
              "projects": [
                {
                  "id": "{{id}}",
                  "name": "One",
                  "rootPath": "{{JsonEscape(Path.Combine(_directory, "One"))}}"
                },
                {
                  "id": "{{id}}",
                  "name": "Two",
                  "rootPath": "{{JsonEscape(Path.Combine(_directory, "Two"))}}"
                }
              ]
            }
            """
        );

        Assert.Throws<InvalidDataException>(
            () => _registry.GetAll()
        );
    }

    [Fact]
    public void GetAll_WhenProjectRootsAreDuplicated_ThrowsInvalidDataException()
    {
        var rootPath =
            Path.Combine(
                _directory,
                "DeadRoute"
            );

        WriteRegistry(
            $$"""
            {
              "version": 1,
              "projects": [
                {
                  "id": "{{Guid.NewGuid()}}",
                  "name": "One",
                  "rootPath": "{{JsonEscape(rootPath)}}"
                },
                {
                  "id": "{{Guid.NewGuid()}}",
                  "name": "Two",
                  "rootPath": "{{JsonEscape(rootPath)}}"
                }
              ]
            }
            """
        );

        Assert.Throws<InvalidDataException>(
            () => _registry.GetAll()
        );
    }

    [Fact]
    public void GetAll_WhenProjectRootDoesNotExist_StillReturnsRegistration()
    {
        var missingRoot =
            Path.Combine(
                _directory,
                "DoesNotExist"
            );

        var id = Guid.NewGuid();

        WriteRegistry(
            $$"""
            {
              "version": 1,
              "projects": [
                {
                  "id": "{{id}}",
                  "name": "MissingProject",
                  "rootPath": "{{JsonEscape(missingRoot)}}"
                }
              ]
            }
            """
        );

        var project =
            Assert.Single(
                _registry.GetAll()
            );

        Assert.Equal(
            id,
            project.Id
        );

        Assert.Equal(
            "MissingProject",
            project.Name
        );

        Assert.Equal(
            Path.GetFullPath(missingRoot),
            project.RootPath
        );
    }

    [Fact]
    public void GetAll_ReturnsProjectsInDeterministicOrder()
    {
        _registry.Add(
            CreateProject(
                "Zulu",
                "Zulu"
            )
        );

        _registry.Add(
            CreateProject(
                "Alpha",
                "Two"
            )
        );

        _registry.Add(
            CreateProject(
                "Alpha",
                "One"
            )
        );

        var projects =
            _registry.GetAll()
                .ToArray();

        Assert.Equal(
            ["Alpha", "Alpha", "Zulu"],
            projects
                .Select(project => project.Name)
                .ToArray()
        );

        Assert.True(
            string.CompareOrdinal(
                projects[0].RootPath,
                projects[1].RootPath
            ) < 0
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(
                _directory,
                recursive: true
            );
        }
    }

    private ProjectContext CreateProject(
        string name,
        string? directoryName = null
    )
    {
        var rootPath = Path.Combine(
            _directory,
            directoryName ?? name
        );

        Directory.CreateDirectory(rootPath);

        return new ProjectContext(
            Name: name,
            RootPath: rootPath,
            IsGitRepository: true
        );
    }

    private void WriteRegistry(
        string content
    )
    {
        Directory.CreateDirectory(
            _environment.HomePath
        );

        File.WriteAllText(
            _environment.RegistryPath,
            content
        );
    }

    private static string JsonEscape(
        string value
    )
    {
        return JsonSerializer.Serialize(value)[1..^1];
    }

    private sealed class TestWayFinderEnvironment(
        string homePath
    ) : IWayFinderEnvironment
    {
        public string HomePath { get; } =
            homePath;

        public string RegistryPath =>
            Path.Combine(
                HomePath,
                "registry.json"
            );
    }
}
