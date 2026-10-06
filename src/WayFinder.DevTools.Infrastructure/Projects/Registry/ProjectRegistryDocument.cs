using WayFinder.DevTools.Application.Projects.Registry;

namespace WayFinder.DevTools.Infrastructure.Projects.Registry;

internal sealed record ProjectRegistryDocument(
    int Version,
    IReadOnlyCollection<RegisteredProject> Projects
);
