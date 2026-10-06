using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Projects.Initialization;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Manifest;

namespace WayFinder.DevTools.Infrastructure.Composition;

public sealed record WayFinderServices(
    IProjectLocator ProjectLocator,
    IProjectFileSystem ProjectFileSystem,
    IProjectInspector ProjectInspector,
    IProjectManifestReader ProjectManifestReader,
    IProjectInitializer ProjectInitializer
);
