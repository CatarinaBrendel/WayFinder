using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registry;
using WayFinder.DevTools.Infrastructure.Composition;
using WayFinder.DevTools.Infrastructure.Projects.Registration;
using WayFinder.DevTools.Mcp.Tests.Fixtures;
using WayFinder.DevTools.Mcp.Tools;

namespace WayFinder.DevTools.Mcp.Tests.Tools;

public sealed class RepositoryRelationshipToolsTests
{
    [Fact]
    public void GetRelationships_ReturnsExternalAssemblyReferences()
    {
        // Arrange
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-relationships-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, "CustomerA.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net48</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Reference Include="Sagede.OfficeLine">
                      <HintPath>..\Sage\Sagede.OfficeLine.dll</HintPath>
                      <Private>False</Private>
                    </Reference>
                  </ItemGroup>
                </Project>
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

            var tool = new RepositoryRelationshipTools(services);

            // Act
            var result = tool.GetRelationships("CustomerA");

            // Assert
            var project = Assert.Single(result.Projects);

            Assert.Equal("CustomerA.csproj", project.ProjectPath);
            Assert.Contains("net48", project.TargetFrameworks);

            var assembly = Assert.Single(project.AssemblyReferences);

            Assert.Equal("Sagede.OfficeLine", assembly.Name);
            Assert.Equal(
                @"..\Sage\Sagede.OfficeLine.dll",
                assembly.HintPath
            );
            Assert.False(assembly.CopyLocal);
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
