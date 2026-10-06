using WayFinder.DevTools.Application.Projects.Detection;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Infrastructure.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Detection;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Projects.Initialization;
using WayFinder.DevTools.Infrastructure.Projects.Manifest;

namespace WayFinder.DevTools.Infrastructure.Composition;

public static class WayFinderComposition
{
    public static WayFinderServices Create()
    {
        var projectLocator =
            new FileSystemProjectLocator();

        var projectFileSystem =
            new ProjectFileSystem();

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

        return new WayFinderServices(
            ProjectLocator: projectLocator,
            ProjectFileSystem: projectFileSystem,
            ProjectInspector: projectInspector,
            ProjectManifestReader: projectManifestReader,
            ProjectInitializer: projectInitializer
        );
    }
}
