namespace WayFinder.DevTools.Application.Projects.Tree;

public enum ProjectTreeEntryType
{
    File,
    Directory
}

public sealed record ProjectTreeEntry(
    string Name,
    string Path,
    ProjectTreeEntryType Type,
    IReadOnlyList<ProjectTreeEntry> Children
);
