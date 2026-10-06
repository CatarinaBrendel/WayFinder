using WayFinder.DevTools.Application.Configuration;
using WayFinder.DevTools.Application.Diagnostics;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Projects.Initialization;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Manifest;
using WayFinder.DevTools.Application.Projects.Registry;
using WayFinder.DevTools.Application.Repositories.Reading;
using WayFinder.DevTools.Application.Repositories.Searching;

namespace WayFinder.DevTools.Infrastructure.Composition;

public sealed record WayFinderServices(
    IWayFinderEnvironment Environment,
    IProjectLocator ProjectLocator,
    IProjectFileSystem ProjectFileSystem,
    IProjectInspector ProjectInspector,
    IProjectManifestReader ProjectManifestReader,
    IProjectInitializer ProjectInitializer,
    IProjectRegistry ProjectRegistry,
    IRepositoryFileReader RepositoryFileReader,
    IRepositorySearcher RepositorySearcher,
    IDoctor Doctor
);
