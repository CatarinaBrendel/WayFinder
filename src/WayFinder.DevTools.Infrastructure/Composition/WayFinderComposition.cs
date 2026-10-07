using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Infrastructure.Configuration;
using WayFinder.DevTools.Infrastructure.Context;
using WayFinder.DevTools.Infrastructure.Context.Estimation;
using WayFinder.DevTools.Infrastructure.Diagnostics;
using WayFinder.DevTools.Infrastructure.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Detection;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Projects.Initialization;
using WayFinder.DevTools.Infrastructure.Projects.Manifest;
using WayFinder.DevTools.Infrastructure.Projects.Registration;
using WayFinder.DevTools.Infrastructure.Projects.Registry;
using WayFinder.DevTools.Infrastructure.Repositories.Reading;
using WayFinder.DevTools.Infrastructure.Repositories.Searching;

namespace WayFinder.DevTools.Infrastructure.Composition;

public static class WayFinderComposition
{
    public static WayFinderServices Create()
    {
        var projectLocator =
            new FileSystemProjectLocator();

        var projectFileSystem =
            new ProjectFileSystem();

        var repositorySearcher =
            new RepositorySearcher(
                projectFileSystem
            );

        var repositoryFileReader =
            new RepositoryFileReader(
                projectFileSystem
            );

        var signatureProvider =
            new JsonTechnologySignatureProvider();

        var technologyDetectors = signatureProvider
            .GetSignatures()
            .Select(
                signature => (IProjectDetector)
                    new TechnologySignatureDetector(
                        projectFileSystem,
                        signature
                    )
            );

        var projectInspector =
            new ProjectInspector(
                technologyDetectors.Append(
                    new GuidanceProjectDetector(
                        projectFileSystem
                    )
                )
            );

        var projectManifestReader =
            new JsonProjectManifestReader(
                projectFileSystem
            );

        var projectInitializer =
            new ProjectInitializer(
                projectInspector,
                projectFileSystem
            );

        var environment =
            new WayFinderEnvironment();

        var projectRegistry =
            new JsonProjectRegistry(environment);

        var registeredProjectResolver =
            new RegisteredProjectResolver(
                projectRegistry
            );

        var doctor =
            new Doctor(
                environment,
                projectRegistry,
                projectLocator
            );

        var tokenEstimator =
            new ApproximateTokenEstimator();

        var contextCompiler =
            new ContextCompiler(
                projectFileSystem,
                repositorySearcher,
                repositoryFileReader,
                tokenEstimator
            );

        return new WayFinderServices(
            ProjectLocator: projectLocator,
            ProjectFileSystem: projectFileSystem,
            ProjectInspector: projectInspector,
            ProjectManifestReader: projectManifestReader,
            ProjectInitializer: projectInitializer,
            ProjectRegistry: projectRegistry,
            Environment: environment,
            RepositoryFileReader: repositoryFileReader,
            RepositorySearcher: repositorySearcher,
            Doctor: doctor,
            ContextCompiler: contextCompiler,
            RegisteredProjectResolver: registeredProjectResolver
        );
    }
}
