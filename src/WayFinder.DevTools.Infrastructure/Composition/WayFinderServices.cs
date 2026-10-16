using WayFinder.DevTools.Application.Configuration;
using WayFinder.DevTools.Application.Context;
using WayFinder.DevTools.Application.Diagnostics;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Projects.Initialization;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Manifest;
using WayFinder.DevTools.Application.Projects.Registration;
using WayFinder.DevTools.Application.Projects.Registry;
using WayFinder.DevTools.Application.Repositories.Reading;
using WayFinder.DevTools.Application.Repositories.Searching;
using WayFinder.DevTools.Application.Projects.Tree;
using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Application.Repositories.Discovery;
using WayFinder.DevTools.Application.Repositories.Relationships;
using WayFinder.DevTools.Application.Repositories.Symbols;
using WayFinder.DevTools.Application.Repositories.Navigation;
using WayFinder.DevTools.Application.Repositories.TypeRelationships;
using WayFinder.DevTools.Application.Repositories.References;

namespace WayFinder.DevTools.Infrastructure.Composition;

public sealed record WayFinderServices(
    IWayFinderEnvironment Environment,
    IProjectLocator ProjectLocator,
    IProjectFileSystem ProjectFileSystem,
    IProjectTreeReader ProjectTreeReader,
    IProjectInspector ProjectInspector,
    IProjectManifestReader ProjectManifestReader,
    IProjectInitializer ProjectInitializer,
    IProjectRegistry ProjectRegistry,
    IRepositoryFileReader RepositoryFileReader,
    IRepositorySearcher RepositorySearcher,
    IRepositoryDiscoveryService RepositoryDiscoveryService,
    IRepositoryRelationshipReader RepositoryRelationshipReader,
    IRepositorySymbolReader RepositorySymbolReader,
    IRepositoryTypeRelationshipReader RepositoryTypeRelationshipReader,
    IRepositoryReferenceReader RepositoryReferenceReader,
    IRepositorySourceNavigator RepositorySourceNavigator,
    IGitStatusReader GitStatusReader,
    IGitDiffReader GitDiffReader,
    IGitLogReader GitLogReader,
    IDoctor Doctor,
    IContextCompiler ContextCompiler,
    IRegisteredProjectResolver RegisteredProjectResolver

);
