using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.Discovery;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Discovery;

public sealed class RepositoryDiscoveryServiceTests
{
    [Fact]
    public void Discover_FindsSolutionsAndProjects()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-discovery-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        try
        {
            Directory.CreateDirectory(
                Path.Combine(root, "src")
            );

            Directory.CreateDirectory(
                Path.Combine(root, "bin")
            );

            File.WriteAllText(
                Path.Combine(root, "Example.slnx"),
                ""
            );

            File.WriteAllText(
                Path.Combine(root, "Legacy.sln"),
                ""
            );

            File.WriteAllText(
                Path.Combine(root, "src", "App.csproj"),
                ""
            );

            File.WriteAllText(
                Path.Combine(root, "src", "Standalone.csproj"),
                ""
            );

            File.WriteAllText(
                Path.Combine(root, "bin", "Ignored.csproj"),
                ""
            );

            var project = new ProjectContext(
                Name: "Example",
                RootPath: root,
                IsGitRepository: false
            );

            var service = new RepositoryDiscoveryService(
                new ProjectFileSystem()
            );

            var result = service.Discover(project);

            Assert.Equal(
                ["Example.slnx", "Legacy.sln"],
                result.Solutions.Select(
                    solution => solution.RelativePath
                )
            );

            Assert.Equal(
                [
                    Path.Combine("src", "App.csproj"),
                    Path.Combine("src", "Standalone.csproj")
                ],
                result.Projects.Select(
                    project => project.RelativePath
                )
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
