using System.Text.Json;
using WayFinder.DevTools.Application.Configuration;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registry;

namespace WayFinder.DevTools.Infrastructure.Projects.Registry;

public sealed class JsonProjectRegistry(
    IWayFinderEnvironment environment
) : IProjectRegistry
{
    private const int SupportedVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

    public IReadOnlyCollection<RegisteredProject> GetAll()
    {
        return ReadDocument().Projects
            .OrderBy(
                project => project.Name,
                StringComparer.Ordinal
            )
            .ThenBy(
                project => project.RootPath,
                StringComparer.Ordinal
            )
            .ToArray();
    }

    public RegisteredProject? FindById(Guid id)
    {
        return ReadDocument().Projects
            .SingleOrDefault(project => project.Id == id);
    }

    public RegisteredProject? FindByRootPath(
        string rootPath
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var canonicalPath =
            CanonicalizeRootPath(rootPath);

        return ReadDocument().Projects
            .SingleOrDefault(
                project =>
                    string.Equals(
                        CanonicalizeRootPath(project.RootPath),
                        canonicalPath,
                        StringComparison.Ordinal
                    )
            );
    }

    public RegisteredProject Add(
        ProjectContext project
    )
    {
        ArgumentNullException.ThrowIfNull(project);

        var document = ReadDocument();

        var canonicalPath =
            CanonicalizeRootPath(project.RootPath);

        var existing = document.Projects
            .SingleOrDefault(
                registered =>
                    string.Equals(
                        CanonicalizeRootPath(
                            registered.RootPath
                        ),
                        canonicalPath,
                        StringComparison.Ordinal
                    )
            );

        if (existing is not null)
        {
            return existing;
        }

        var registeredProject =
            new RegisteredProject(
                Id: Guid.NewGuid(),
                Name: project.Name,
                RootPath: canonicalPath
            );

        var projects = document.Projects
            .Append(registeredProject)
            .OrderBy(
                item => item.Name,
                StringComparer.Ordinal
            )
            .ThenBy(
                item => item.RootPath,
                StringComparer.Ordinal
            )
            .ToArray();

        WriteDocument(
            new ProjectRegistryDocument(
                Version: SupportedVersion,
                Projects: projects
            )
        );

        return registeredProject;
    }

    public bool Remove(Guid id)
    {
        var document = ReadDocument();

        var projects = document.Projects
            .Where(project => project.Id != id)
            .ToArray();

        if (projects.Length == document.Projects.Count)
        {
            return false;
        }

        WriteDocument(
            new ProjectRegistryDocument(
                Version: SupportedVersion,
                Projects: projects
            )
        );

        return true;
    }

    private ProjectRegistryDocument ReadDocument()
    {
        if (!File.Exists(environment.RegistryPath))
        {
            return new ProjectRegistryDocument(
                Version: SupportedVersion,
                Projects: []
            );
        }

        try
        {
            var json =
                File.ReadAllText(environment.RegistryPath);

            var document =
                JsonSerializer.Deserialize<ProjectRegistryDocument>(
                    json,
                    JsonOptions
                );

            if (document is null)
            {
                throw new InvalidDataException(
                    "The WayFinder project registry is empty."
                );
            }

            Validate(document);

            return document;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The WayFinder project registry contains invalid JSON.",
                exception
            );
        }
    }

    private void WriteDocument(
    ProjectRegistryDocument document
)
    {
        var directory =
            Path.GetDirectoryName(environment.RegistryPath)
            ?? throw new InvalidOperationException(
                "Could not determine the WayFinder home directory."
            );

        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(
            document,
            JsonOptions
        );

        var content =
            $"{json}{System.Environment.NewLine}";

        var temporaryPath =
            $"{environment.RegistryPath}.tmp";

        try
        {
            File.WriteAllText(
                temporaryPath,
                content
            );

            File.Move(
                temporaryPath,
                environment.RegistryPath,
                overwrite: true
            );
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void Validate(
        ProjectRegistryDocument document
    )
    {
        if (document.Version != SupportedVersion)
        {
            throw new InvalidDataException(
                $"Unsupported WayFinder project registry version '{document.Version}'."
            );
        }

        if (document.Projects is null)
        {
            throw new InvalidDataException(
                "The WayFinder project registry must define 'projects'."
            );
        }

        if (document.Projects.Any(
                project =>
                    project.Id == Guid.Empty
                    || string.IsNullOrWhiteSpace(project.Name)
                    || string.IsNullOrWhiteSpace(project.RootPath)))
        {
            throw new InvalidDataException(
                "The WayFinder project registry contains an invalid project."
            );
        }

        var duplicateIds = document.Projects
            .GroupBy(project => project.Id)
            .Any(group => group.Count() > 1);

        if (duplicateIds)
        {
            throw new InvalidDataException(
                "The WayFinder project registry contains duplicate project IDs."
            );
        }

        var duplicateRoots = document.Projects
            .GroupBy(
                project => CanonicalizeRootPath(
                    project.RootPath
                ),
                StringComparer.Ordinal
            )
            .Any(group => group.Count() > 1);

        if (duplicateRoots)
        {
            throw new InvalidDataException(
                "The WayFinder project registry contains duplicate project roots."
            );
        }
    }

    private static string CanonicalizeRootPath(
        string rootPath
    )
    {
        return Path.GetFullPath(rootPath)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );
    }
}
