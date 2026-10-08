using WayFinder.DevTools.Application.Projects.Files;

namespace WayFinder.DevTools.Application.Projects.Tree;

public sealed class ProjectTreeReader(
    IProjectFileSystem fileSystem
) : IProjectTreeReader
{
    public ProjectTreeEntry Read(
        ProjectContext project,
        int? maxDepth = null
    )
    {
        if (maxDepth is < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDepth)
            );
        }

        var rootName = new DirectoryInfo(
            project.RootPath
        ).Name;

        return ReadDirectory(
            project,
            name: rootName,
            path: "",
            depth: 0,
            maxDepth
        );
    }

    private ProjectTreeEntry ReadDirectory(
        ProjectContext project,
        string name,
        string path,
        int depth,
        int? maxDepth
    )
    {
        if (maxDepth is not null
            && depth >= maxDepth)
        {
            return new ProjectTreeEntry(
                name,
                path,
                ProjectTreeEntryType.Directory,
                []
            );
        }

        var children = fileSystem
            .GetEntries(project, path)
            .Select(
                entry => entry.IsDirectory
                    ? ReadDirectory(
                        project,
                        entry.Name,
                        entry.Path,
                        depth + 1,
                        maxDepth
                    )
                    : new ProjectTreeEntry(
                        entry.Name,
                        entry.Path,
                        ProjectTreeEntryType.File,
                        []
                    )
            )
            .ToArray();

        return new ProjectTreeEntry(
            name,
            path,
            ProjectTreeEntryType.Directory,
            children
        );
    }
}
