using System.Text.Json;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Projects.Initialization;
using WayFinder.DevTools.Application.Projects.Inspection;

namespace WayFinder.DevTools.Infrastructure.Projects.Initialization;

public sealed class ProjectInitializer(
    IProjectInspector inspector,
    IProjectFileSystem fileSystem
) : IProjectInitializer
{
    private const string ManifestPath = "wayfinder.json";

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true,
        };

    public ProjectInitialization Prepare(ProjectContext project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var inspection = inspector.Inspect(project);

        var technologies = inspection.Artifacts
            .Where(entry =>
                entry.Key != "guidance"
                && entry.Value.Count > 0
            )
            .Select(entry => entry.Key)
            .OrderBy(
                technology => technology,
                StringComparer.Ordinal
            )
            .ToArray();

        return new ProjectInitialization(
            ManifestPath: ManifestPath,
            ProjectName: project.Name,
            Technologies: technologies
        );
    }

    public void Initialize(
        ProjectContext project,
        ProjectInitialization initialization
    )
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(initialization);

        if (fileSystem.FileExists(project, ManifestPath))
        {
            throw new InvalidOperationException(
                "The project already contains wayfinder.json."
            );
        }

        var manifest = new
        {
            version = 1,
            name = initialization.ProjectName,
            technologies = initialization.Technologies,
        };

        var json = JsonSerializer.Serialize(
            manifest,
            JsonOptions
        );

        fileSystem.WriteAllText(
            project,
            ManifestPath,
            $"{json}{Environment.NewLine}"
        );
    }
}
