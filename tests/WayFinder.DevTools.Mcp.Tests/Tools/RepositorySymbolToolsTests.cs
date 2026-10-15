using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registry;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Infrastructure.Projects.Registration;
using WayFinder.DevTools.Mcp.Tests.Fixtures;
using WayFinder.DevTools.Mcp.Tools;

namespace WayFinder.DevTools.Mcp.Tests.Tools;

public sealed class RepositorySymbolToolsTests
{
    [Fact]
    public void GetSymbols_ReturnsClassAndMethod()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-symbols-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, "CustomerService.cs"),
                """
                namespace CustomerA.Services;

                public class CustomerService
                {
                    public void GetCustomer()
                    {
                    }
                }
                """
            );

            var registry = new InMemoryProjectRegistry();

            registry.Add(
                new ProjectContext(
                    "CustomerA",
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

            var tool = new RepositorySymbolTools(services);

            // Act
            var result = tool.GetSymbols(
                "CustomerA",
                "CustomerService.cs"
            );

            // Assert
            Assert.False(result.Truncated);

            Assert.Contains(
                result.Symbols,
                symbol =>
                    symbol.Name == "CustomerService"
                    && symbol.Kind == "Class"
            );

            Assert.Contains(
                result.Symbols,
                symbol =>
                    symbol.Name == "GetCustomer"
                    && symbol.Kind == "Method"
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
}
