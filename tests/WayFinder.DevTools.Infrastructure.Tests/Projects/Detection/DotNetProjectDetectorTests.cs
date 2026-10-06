using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Detection;
using WayFinder.DevTools.Infrastructure.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Detection;

public sealed class DotNetProjectDetectorTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly DotNetProjectDetector _detector;

    public DotNetProjectDetectorTests()
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

        _detector = new DotNetProjectDetector(
            new ProjectFileSystem()
        );
    }

    [Fact]
    public void Detect_FindsDotNetArtifacts()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, "src", "App")
        );

        File.WriteAllText(
            Path.Combine(_root, "TestProject.slnx"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "global.json"),
            "{}"
        );

        File.WriteAllText(
            Path.Combine(
                _root,
                "src",
                "App",
                "App.csproj"
            ),
            string.Empty
        );

        var artifacts = _detector.Detect(_project);

        Assert.Contains(
            artifacts,
            artifact =>
                artifact.Type == "solution"
                && artifact.Path == "TestProject.slnx"
        );

        Assert.Contains(
            artifacts,
            artifact =>
                artifact.Type == "project"
                && artifact.Path == Path.Combine(
                    "src",
                    "App",
                    "App.csproj"
                )
        );

        Assert.Contains(
            artifacts,
            artifact =>
                artifact.Type == "globaljson"
                && artifact.Path == "global.json"
        );
    }

    [Fact]
    public void Detect_IgnoresBuildArtifacts()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, "obj")
        );

        File.WriteAllText(
            Path.Combine(
                _root,
                "obj",
                "Generated.csproj"
            ),
            string.Empty
        );

        var artifacts = _detector.Detect(_project);

        Assert.DoesNotContain(
            artifacts,
            artifact => artifact.Path.Contains("Generated.csproj")
        );
    }

    [Fact]
    public void Detect_WhenNoDotNetArtifactsExist_ReturnsEmpty()
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
