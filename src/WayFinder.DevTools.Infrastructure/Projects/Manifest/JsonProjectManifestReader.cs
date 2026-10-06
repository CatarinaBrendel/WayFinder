using System.Text.Json;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Projects.Manifest;

namespace WayFinder.DevTools.Infrastructure.Projects.Manifest;

public sealed class JsonProjectManifestReader(
    IProjectFileSystem fileSystem
) : IProjectManifestReader
{
    private const int SupportedVersion = 1;
    private const string ManifestPath = "wayfinder.json";

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
        };

    public ProjectManifest? Read(ProjectContext project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (!fileSystem.FileExists(project, ManifestPath))
        {
            return null;
        }

        var json = fileSystem.ReadAllText(
            project,
            ManifestPath
        );

        ProjectManifest manifest;

        try
        {
            manifest =
                JsonSerializer.Deserialize<ProjectManifest>(
                    json,
                    JsonOptions
                )
                ?? throw new InvalidDataException(
                    "wayfinder.json does not contain a valid project manifest."
                );
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "wayfinder.json contains invalid JSON.",
                exception
            );
        }

        Validate(manifest);

        return manifest;
    }

    private static void Validate(ProjectManifest manifest)
    {
        if (manifest.Version != SupportedVersion)
        {
            throw new InvalidDataException(
                $"Unsupported WayFinder project manifest version '{manifest.Version}'."
            );
        }

        if (manifest.Technologies is null)
        {
            throw new InvalidDataException(
                "The WayFinder project manifest must define 'technologies'."
            );
        }

        if (manifest.Technologies.Any(
                string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException(
                "Technology IDs cannot be empty."
            );
        }

        var duplicateTechnologies = manifest.Technologies
            .GroupBy(
                technology => technology,
                StringComparer.Ordinal
            )
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(
                technology => technology,
                StringComparer.Ordinal
            )
            .ToArray();

        if (duplicateTechnologies.Length > 0)
        {
            throw new InvalidDataException(
                $"Duplicate technology IDs: {string.Join(", ", duplicateTechnologies)}."
            );
        }
    }
}
