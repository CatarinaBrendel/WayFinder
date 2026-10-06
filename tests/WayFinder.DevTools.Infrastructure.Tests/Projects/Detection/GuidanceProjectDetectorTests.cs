using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Detection;
using WayFinder.DevTools.Infrastructure.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Detection;

public sealed class GuidanceProjectDetectorTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly GuidanceProjectDetector _detector;

    public GuidanceProjectDetectorTests()
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

        _detector = new GuidanceProjectDetector(
            new ProjectFileSystem()
        );
    }

    [Fact]
    public void Detect_FindsAgentsFile()
    {
        File.WriteAllText(
            Path.Combine(_root, "AGENTS.md"),
            "# Instructions"
        );

        var artifacts = _detector.Detect(_project);

        var artifact = Assert.Single(artifacts);

        Assert.Equal("agents", artifact.Type);
        Assert.Equal("AGENTS.md", artifact.Path);
    }

    [Fact]
    public void Detect_FindsEditorConfig()
    {
        File.WriteAllText(
            Path.Combine(_root, ".editorconfig"),
            "root = true"
        );

        var artifacts = _detector.Detect(_project);

        var artifact = Assert.Single(artifacts);

        Assert.Equal("editorconfig", artifact.Type);
        Assert.Equal(".editorconfig", artifact.Path);
    }

    [Fact]
    public void Detect_FindsAllGuidanceArtifacts()
    {
        File.WriteAllText(
            Path.Combine(_root, "AGENTS.md"),
            "# Instructions"
        );

        File.WriteAllText(
            Path.Combine(_root, ".editorconfig"),
            "root = true"
        );

        var artifacts = _detector.Detect(_project);

        Assert.Equal(2, artifacts.Count);
    }

    [Fact]
    public void Detect_WhenNoGuidanceExists_ReturnsEmpty()
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
