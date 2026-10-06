using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Projects.Manifest;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Manifest;

public sealed class JsonProjectManifestReaderTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly ProjectFileSystem _fileSystem = new();

    public JsonProjectManifestReaderTests()
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
    public void Read_WhenManifestDoesNotExist_ReturnsNull()
    {
        var reader = new JsonProjectManifestReader(_fileSystem);

        var manifest = reader.Read(_project);

        Assert.Null(manifest);
    }

    [Fact]
    public void Read_WhenManifestIsValid_LoadsManifest()
    {
        WriteManifest(
            """
            {
              "version": 1,
              "name": "MyProject",
              "technologies": [
                "dotnet",
                "node"
              ]
            }
            """
        );

        var reader = new JsonProjectManifestReader(_fileSystem);

        var manifest = reader.Read(_project);

        Assert.NotNull(manifest);
        Assert.Equal(1, manifest.Version);
        Assert.Equal("MyProject", manifest.Name);

        Assert.Equal(
            [
                "dotnet",
                "node",
            ],
            manifest.Technologies
        );
    }

    [Fact]
    public void Read_AllowsUnknownTechnology()
    {
        WriteManifest(
            """
            {
              "version": 1,
              "technologies": [
                "frobnicator-9000"
              ]
            }
            """
        );

        var reader = new JsonProjectManifestReader(_fileSystem);

        var manifest = reader.Read(_project);

        Assert.NotNull(manifest);

        Assert.Contains(
            "frobnicator-9000",
            manifest.Technologies
        );
    }

    [Fact]
    public void Read_AllowsEmptyTechnologies()
    {
        WriteManifest(
            """
            {
              "version": 1,
              "technologies": []
            }
            """
        );

        var reader = new JsonProjectManifestReader(_fileSystem);

        var manifest = reader.Read(_project);

        Assert.NotNull(manifest);
        Assert.Empty(manifest.Technologies);
    }

    [Fact]
    public void Read_RejectsUnsupportedVersion()
    {
        WriteManifest(
            """
            {
              "version": 2,
              "technologies": []
            }
            """
        );

        var reader = new JsonProjectManifestReader(_fileSystem);

        Assert.Throws<InvalidDataException>(
            () => reader.Read(_project)
        );
    }

    [Fact]
    public void Read_RejectsBlankTechnology()
    {
        WriteManifest(
            """
            {
              "version": 1,
              "technologies": [
                "dotnet",
                ""
              ]
            }
            """
        );

        var reader = new JsonProjectManifestReader(_fileSystem);

        Assert.Throws<InvalidDataException>(
            () => reader.Read(_project)
        );
    }

    [Fact]
    public void Read_RejectsWhitespaceTechnology()
    {
        WriteManifest(
            """
            {
              "version": 1,
              "technologies": [
                "dotnet",
                "   "
              ]
            }
            """
        );

        var reader = new JsonProjectManifestReader(_fileSystem);

        Assert.Throws<InvalidDataException>(
            () => reader.Read(_project)
        );
    }

    [Fact]
    public void Read_RejectsDuplicateTechnology()
    {
        WriteManifest(
            """
            {
              "version": 1,
              "technologies": [
                "dotnet",
                "dotnet"
              ]
            }
            """
        );

        var reader = new JsonProjectManifestReader(_fileSystem);

        Assert.Throws<InvalidDataException>(
            () => reader.Read(_project)
        );
    }

    [Fact]
    public void Read_RejectsMalformedJson()
    {
        WriteManifest(
            """
            {
              definitely-not-json
            }
            """
        );

        var reader = new JsonProjectManifestReader(_fileSystem);

        Assert.Throws<InvalidDataException>(
            () => reader.Read(_project)
        );
    }

    [Fact]
    public void Read_RejectsMissingTechnologies()
    {
        WriteManifest(
            """
            {
              "version": 1,
              "name": "MyProject"
            }
            """
        );

        var reader = new JsonProjectManifestReader(_fileSystem);

        Assert.Throws<InvalidDataException>(
            () => reader.Read(_project)
        );
    }

    private void WriteManifest(string content)
    {
        File.WriteAllText(
            Path.Combine(_root, "wayfinder.json"),
            content
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
}
