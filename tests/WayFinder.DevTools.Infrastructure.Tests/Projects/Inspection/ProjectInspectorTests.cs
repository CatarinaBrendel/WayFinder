using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Application.Projects.Inspection;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Inspection;

public sealed class ProjectInspectorTests
{
    [Fact]
    public void Inspect_GroupsArtifactsByDetector()
    {
        var project = new ProjectContext(
            Name: "TestProject",
            RootPath: "/test",
            IsGitRepository: true
        );

        IProjectDetector[] detectors =
        [
            new FakeDetector(
                "dotnet",
                [
                    new ProjectArtifact(
                        "solution",
                        "Test.slnx"
                    ),
                ]
            ),
            new FakeDetector(
                "guidance",
                [
                    new ProjectArtifact(
                        "agents",
                        "AGENTS.md"
                    ),
                ]
            ),
        ];

        var inspector = new ProjectInspector(detectors);

        var inspection = inspector.Inspect(project);

        Assert.Same(project, inspection.Project);

        Assert.Equal(
            2,
            inspection.Artifacts.Count
        );

        Assert.Contains(
            inspection.Artifacts["dotnet"],
            artifact =>
                artifact.Type == "solution"
                && artifact.Path == "Test.slnx"
        );

        Assert.Contains(
            inspection.Artifacts["guidance"],
            artifact =>
                artifact.Type == "agents"
                && artifact.Path == "AGENTS.md"
        );
    }

    private sealed class FakeDetector(
        string name,
        IReadOnlyCollection<ProjectArtifact> artifacts
    ) : IProjectDetector
    {
        public string Name => name;

        public IReadOnlyCollection<ProjectArtifact> Detect(
            ProjectContext project
        )
        {
            return artifacts;
        }
    }
}
