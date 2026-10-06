namespace WayFinder.DevTools.Application.Projects.Registry;

public sealed record RegisteredProject(
    Guid Id,
    string Name,
    string RootPath
);
