using WayFinder.DevTools.Application.Projects.Detection;

namespace WayFinder.DevTools.Application.Projects.Inspection;

public sealed class ProjectInspector(
    IEnumerable<IProjectDetector> detectors
) : IProjectInspector
{
    private readonly IReadOnlyCollection<IProjectDetector> _detectors =
        detectors.ToArray();

    public ProjectInspection Inspect(ProjectContext project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var artifacts =
            new Dictionary<string, IReadOnlyCollection<ProjectArtifact>>();

        foreach (var detector in _detectors)
        {
            artifacts.Add(
                detector.Name,
                detector.Detect(project)
            );
        }

        return new ProjectInspection(
            Project: project,
            Artifacts: artifacts
        );
    }
}
