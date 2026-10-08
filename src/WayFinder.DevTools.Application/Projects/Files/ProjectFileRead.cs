namespace WayFinder.DevTools.Application.Projects.Files;

public sealed record ProjectFileRead(
    byte[] Content,
    long TotalBytes,
    long Offset,
    bool Truncated
);
