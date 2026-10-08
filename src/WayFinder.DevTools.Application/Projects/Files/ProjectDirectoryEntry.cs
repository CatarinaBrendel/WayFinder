namespace WayFinder.DevTools.Application.Projects.Files;

public enum ProjectDirectoryEntryType
{
    File,
    Directory
}

public sealed record ProjectDirectoryEntry(
    string Name,
    string Path,
    bool IsDirectory
);
