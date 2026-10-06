using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Detection;
using WayFinder.DevTools.Infrastructure.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Detection;

public sealed class NodeProjectDetectorTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly NodeProjectDetector _detector;

    public NodeProjectDetectorTests()
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

        _detector = new NodeProjectDetector(
            new ProjectFileSystem()
        );
    }

    [Fact]
    public void Detect_FindsPackageManifest()
    {
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{}"
        );

        var artifacts = _detector.Detect(_project);

        var artifact = Assert.Single(artifacts);

        Assert.Equal("manifest", artifact.Type);
        Assert.Equal("package.json", artifact.Path);
    }

    [Fact]
    public void Detect_FindsPnpmLockFile()
    {
        File.WriteAllText(
            Path.Combine(_root, "pnpm-lock.yaml"),
            string.Empty
        );

        var artifacts = _detector.Detect(_project);

        var artifact = Assert.Single(artifacts);

        Assert.Equal("pnpm-lock", artifact.Type);
        Assert.Equal("pnpm-lock.yaml", artifact.Path);
    }

    [Fact]
    public void Detect_FindsNestedPackage()
    {
        var web = Path.Combine(_root, "src", "Web");

        Directory.CreateDirectory(web);

        File.WriteAllText(
            Path.Combine(web, "package.json"),
            "{}"
        );

        var artifacts = _detector.Detect(_project);

        var artifact = Assert.Single(artifacts);

        Assert.Equal(
            Path.Combine("src", "Web", "package.json"),
            artifact.Path
        );
    }

    [Fact]
    public void Detect_DoesNotInspectNodeModules()
    {
        var nodeModules = Path.Combine(
            _root,
            "node_modules",
            "some-package"
        );

        Directory.CreateDirectory(nodeModules);

        File.WriteAllText(
            Path.Combine(nodeModules, "package.json"),
            "{}"
        );

        var artifacts = _detector.Detect(_project);

        Assert.Empty(artifacts);
    }

    [Fact]
    public void Detect_ReturnsArtifactsInDeterministicOrder()
    {
        var web = Path.Combine(_root, "web");

        Directory.CreateDirectory(web);

        File.WriteAllText(
            Path.Combine(web, "pnpm-lock.yaml"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(web, "package.json"),
            "{}"
        );

        var artifacts = _detector.Detect(_project);

        Assert.Equal(
            [
                Path.Combine("web", "package.json"),
                Path.Combine("web", "pnpm-lock.yaml"),
            ],
            artifacts.Select(artifact => artifact.Path)
        );
    }

    [Fact]
    public void Detect_WhenNoNodeArtifactsExist_ReturnsEmpty()
    {
        var artifacts = _detector.Detect(_project);

        Assert.Empty(artifacts);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
