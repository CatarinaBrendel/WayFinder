using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registration;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Projects.Registration;
using WayFinder.DevTools.Infrastructure.Repositories.Discovery;
using WayFinder.DevTools.Mcp.Tests.Fixtures;
using WayFinder.DevTools.Mcp.Tools;
using Xunit;

namespace WayFinder.DevTools.Mcp.Tests.Tools;

public sealed class RepositoryDiscoveryToolsTests
{
    [Fact]
    public void Discover_UnregisteredProject_Throws()
    {
        // Arrange
        var services = WayFinderComposition.Create();

        var tool = new RepositoryDiscoveryTools(services);

        var unknownProject =
            $"unregistered-{Guid.NewGuid():N}";

        // Act
        var exception = Record.Exception(
            () => tool.Discover(unknownProject)
        );

        // Assert
        Assert.IsType<
            RegisteredProjectReferenceNotFoundException
        >(exception);
    }

    [Fact]
    public void Discover_RegisteredProject_ReturnsRelativePaths()
    {
        // Arrange
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-mcp-test-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        try
        {
            var projectDirectory = Path.Combine(root, "src", "TestProject");

            Directory.CreateDirectory(projectDirectory);

            File.WriteAllText(
                Path.Combine(root, "TestRepository.sln"),
                string.Empty
            );

            File.WriteAllText(
                Path.Combine(projectDirectory, "TestProject.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\" />"
            );

            var registry = new InMemoryProjectRegistry();

            registry.Add(
                new ProjectContext(
                    "TestRepository",
                    root,
                    IsGitRepository: false
                )
            );

            var fileSystem = new ProjectFileSystem();

            var discovery = new RepositoryDiscoveryService(fileSystem);

            var services = WayFinderComposition.Create() with
            {
                ProjectRegistry = registry,
                RegisteredProjectResolver =
                    new RegisteredProjectResolver(registry),
                RepositoryDiscoveryService = discovery
            };

            var tool = new RepositoryDiscoveryTools(services);

            // Act
            var result = tool.Discover("TestRepository");

            // Assert
            Assert.Contains(
                result.Solutions,
                solution =>
                    solution.Name == "TestRepository"
                    && solution.RelativePath == "TestRepository.sln"
            );

            Assert.Contains(
                result.Projects,
                project =>
                    project.Name == "TestProject"
                    && project.RelativePath == "src/TestProject/TestProject.csproj"
            );

            Assert.All(
                result.Solutions,
                solution => Assert.False(
                    Path.IsPathRooted(solution.RelativePath))
            );

            Assert.All(
                result.Projects,
                project => Assert.False(
                    Path.IsPathRooted(project.RelativePath))
            );
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Discover_RegisteredProjectId_Succeeds()
    {
        // Arrange: create an isolated repository.
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-mcp-test-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, "TestProject.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\" />"
            );

            var registry = new InMemoryProjectRegistry();

            var registered = registry.Add(
                new ProjectContext(
                    "TestRepository",
                    root,
                    IsGitRepository: false
                )
            );

            var services = WayFinderComposition.Create() with
            {
                ProjectRegistry = registry,
                RegisteredProjectResolver =
                    new RegisteredProjectResolver(registry)
            };

            var tool = new RepositoryDiscoveryTools(services);

            // Act: resolve using the registered GUID.
            var result = tool.Discover(
                registered.Id.ToString()
            );

            // Assert
            Assert.Single(result.Projects);

            Assert.Equal(
                "TestProject.csproj",
                result.Projects[0].RelativePath
            );
        }
        finally
        {
            // Always clean up the temporary repository.
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Discover_UnregisteredAbsolutePath_Throws()
    {
        var registry = new InMemoryProjectRegistry();

        var services = WayFinderComposition.Create() with
        {
            ProjectRegistry = registry,
            RegisteredProjectResolver =
                new RegisteredProjectResolver(registry)
        };

        var tool = new RepositoryDiscoveryTools(services);

        var unregisteredPath = Path.Combine(
            Path.GetTempPath(),
            $"unregistered-{Guid.NewGuid():N}"
        );

        Assert.Throws<
            RegisteredProjectReferenceNotFoundException
        >(() => tool.Discover(unregisteredPath));
    }
}
