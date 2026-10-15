using System.Xml;
using System.Xml.Linq;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.Discovery;
using WayFinder.DevTools.Application.Repositories.Relationships;

namespace WayFinder.DevTools.Infrastructure.Repositories.Relationships;

public sealed class RepositoryRelationshipReader(
    IRepositoryDiscoveryService discoveryService,
    IProjectFileSystem fileSystem
) : IRepositoryRelationshipReader
{
    private const int MaxProjectFileBytes = 1024 * 1024;

    public IReadOnlyList<RepositoryProjectRelationship> Read(
        ProjectContext project
    )
    {
        ArgumentNullException.ThrowIfNull(project);

        var discovered = discoveryService.Discover(project);

        var relationships = new List<RepositoryProjectRelationship>();

        foreach (var repositoryProject in discovered.Projects)
        {
            ProjectFileRead file;

            try
            {
                file = fileSystem.Read(
                    project,
                    repositoryProject.RelativePath,
                    MaxProjectFileBytes + 1
                );
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            if (file.Truncated || file.TotalBytes > MaxProjectFileBytes)
            {
                continue;
            }

            XDocument document;

            try
            {
                using var stream = new MemoryStream(file.Content, writable: false);

                using var reader = XmlReader.Create(
                    stream,
                    new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null
                    }
                );

                document = XDocument.Load(reader);
            }
            catch (XmlException)
            {
                continue;
            }

            var frameworks = document
                .Descendants()
                .Where(element =>
                    element.Name.LocalName is
                        "TargetFramework" or "TargetFrameworks"
                )
                .SelectMany(element =>
                    element.Value.Split(
                        ';',
                        StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries
                    )
                )
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            var references = document
                .Descendants()
                .Where(element =>
                    element.Name.LocalName == "ProjectReference"
                )
                .Select(element =>
                    element.Attribute("Include")?.Value
                )
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value =>
                    ResolveReference(
                        project,
                        repositoryProject.RelativePath,
                        value!
                    )
                )
                .Distinct()
                .OrderBy(reference => reference.Path, StringComparer.Ordinal)
                .ToArray();

            var assemblyReferences = document
                .Descendants()
                .Where(element =>
                    element.Name.LocalName == "Reference"
                )
                .Select(element =>
                {
                    var name = element.Attribute("Include")?.Value;

                    var hintPath = element.Elements()
                        .FirstOrDefault(child =>
                            child.Name.LocalName == "HintPath"
                        )?.Value;

                    var privateValue = element.Elements()
                        .FirstOrDefault(child =>
                            child.Name.LocalName == "Private"
                        )?.Value;

                    bool? copyLocal = bool.TryParse(
                        privateValue?.Trim(),
                        out var parsed
                    )
                        ? parsed
                        : null;

                    return new RepositoryAssemblyReference(
                        name?.Trim() ?? string.Empty,
                        string.IsNullOrWhiteSpace(hintPath)
                            ? null
                            : hintPath.Trim(),
                        copyLocal
                    );
                })
                .Where(reference =>
                    !string.IsNullOrWhiteSpace(reference.Name)
                )
                .OrderBy(reference =>
                    reference.Name,
                    StringComparer.Ordinal
                )
                .ToArray();

            relationships.Add(
                new RepositoryProjectRelationship(
                    repositoryProject.RelativePath,
                    frameworks,
                    references,
                    assemblyReferences
                )
            );
        }

        return relationships
            .OrderBy(
                relationship => relationship.ProjectPath,
                StringComparer.Ordinal
            )
            .ToArray();
    }

    private static RepositoryProjectReference ResolveReference(
        ProjectContext project,
        string projectPath,
        string include
    )
    {
        if (include.Contains("$(", StringComparison.Ordinal)
            || include.Contains("%(", StringComparison.Ordinal))
        {
            return new RepositoryProjectReference(
                include,
                IsExternal: true
            );
        }

        if (Path.IsPathFullyQualified(include)
            || (include.Length >= 2
                && char.IsLetter(include[0])
                && include[1] == ':')
            || include.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return new RepositoryProjectReference(
                include,
                IsExternal: true
            );
        }

        var normalizedInclude = include.Replace(
            '\\',
            Path.DirectorySeparatorChar
        ).Replace(
            '/',
            Path.DirectorySeparatorChar
        );

        var projectDirectory = Path.GetDirectoryName(
            Path.GetFullPath(
                Path.Combine(project.RootPath, projectPath)
            )
        )!;

        var resolvedPath = Path.GetFullPath(
            Path.Combine(projectDirectory, normalizedInclude)
        );

        var relativePath = Path.GetRelativePath(
            project.RootPath,
            resolvedPath
        );

        var isExternal =
            Path.IsPathRooted(relativePath)
            || relativePath == ".."
            || relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal
            );

        return new RepositoryProjectReference(
            isExternal
                ? include
                : relativePath.Replace(
                    Path.DirectorySeparatorChar,
                    '/'
                ),
            isExternal
        );
    }
}
