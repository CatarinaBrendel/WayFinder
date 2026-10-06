using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Infrastructure.Projects.Detection;
using WayFinder.DevTools.Infrastructure.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Detection;

public sealed class TechnologySignatureDetectorTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly ProjectFileSystem _fileSystem = new();

    public TechnologySignatureDetectorTests()
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
    public void Detect_FindsArtifactsForArbitraryTechnology()
    {
        var signature = new TechnologySignature(
            Id: "wibble",
            DisplayName: "Wibble",
            Artifacts:
            [
                new ArtifactSignature(
                    "*.wibble",
                    "source"
                ),
            ]
        );

        File.WriteAllText(
            Path.Combine(_root, "example.wibble"),
            "hello"
        );

        var detector = new TechnologySignatureDetector(
            _fileSystem,
            signature
        );

        var artifacts = detector.Detect(_project);

        var artifact = Assert.Single(artifacts);

        Assert.Equal("source", artifact.Type);
        Assert.Equal("example.wibble", artifact.Path);
    }

    [Fact]
    public void Detect_FindsMultipleArtifactTypes()
    {
        var signature = new TechnologySignature(
            Id: "wibble",
            DisplayName: "Wibble",
            Artifacts:
            [
                new ArtifactSignature(
                    "wibble.project",
                    "manifest"
                ),
                new ArtifactSignature(
                    "*.wibble",
                    "source"
                ),
            ]
        );

        File.WriteAllText(
            Path.Combine(_root, "wibble.project"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "main.wibble"),
            string.Empty
        );

        var detector = new TechnologySignatureDetector(
            _fileSystem,
            signature
        );

        var artifacts = detector.Detect(_project);

        Assert.Equal(2, artifacts.Count);

        Assert.Contains(
            artifacts,
            artifact =>
                artifact.Type == "manifest"
                && artifact.Path == "wibble.project"
        );

        Assert.Contains(
            artifacts,
            artifact =>
                artifact.Type == "source"
                && artifact.Path == "main.wibble"
        );
    }

    [Fact]
    public void Detect_FindsNestedArtifacts()
    {
        var signature = new TechnologySignature(
            Id: "wibble",
            DisplayName: "Wibble",
            Artifacts:
            [
                new ArtifactSignature(
                    "*.wibble",
                    "source"
                ),
            ]
        );

        var source = Path.Combine(
            _root,
            "src",
            "Something"
        );

        Directory.CreateDirectory(source);

        File.WriteAllText(
            Path.Combine(source, "main.wibble"),
            string.Empty
        );

        var detector = new TechnologySignatureDetector(
            _fileSystem,
            signature
        );

        var artifacts = detector.Detect(_project);

        var artifact = Assert.Single(artifacts);

        Assert.Equal(
            Path.Combine(
                "src",
                "Something",
                "main.wibble"
            ),
            artifact.Path
        );
    }

    [Fact]
    public void Detect_WhenTechnologyIsAbsent_ReturnsEmpty()
    {
        var signature = new TechnologySignature(
            Id: "wibble",
            DisplayName: "Wibble",
            Artifacts:
            [
                new ArtifactSignature(
                    "*.wibble",
                    "source"
                ),
            ]
        );

        var detector = new TechnologySignatureDetector(
            _fileSystem,
            signature
        );

        var artifacts = detector.Detect(_project);

        Assert.Empty(artifacts);
    }

    [Fact]
    public void Detect_ReturnsArtifactsInDeterministicOrder()
    {
        var signature = new TechnologySignature(
            Id: "wibble",
            DisplayName: "Wibble",
            Artifacts:
            [
                new ArtifactSignature(
                    "*.wibble",
                    "source"
                ),
            ]
        );

        File.WriteAllText(
            Path.Combine(_root, "zeta.wibble"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "alpha.wibble"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "middle.wibble"),
            string.Empty
        );

        var detector = new TechnologySignatureDetector(
            _fileSystem,
            signature
        );

        var artifacts = detector.Detect(_project);

        Assert.Equal(
            [
                "alpha.wibble",
                "middle.wibble",
                "zeta.wibble",
            ],
            artifacts.Select(
                artifact => artifact.Path
            )
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
