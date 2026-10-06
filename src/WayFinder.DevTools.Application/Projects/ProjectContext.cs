namespace WayFinder.DevTools.Application.Projects;

public sealed record ProjectContext(
    string Name,
    string RootPath,
    bool IsGitRepository
);
