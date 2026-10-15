using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.Discovery;
using WayFinder.DevTools.Infrastructure.Repositories.Relationships;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Relationships;

public sealed class RepositoryRelationshipReaderTests
{
    [Fact]
    public void Read_ExtractsFrameworksAndDirectProjectReferences()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src", "Cli"));
            Directory.CreateDirectory(Path.Combine(root, "src", "Application"));

            File.WriteAllText(
                Path.Combine(root, "src", "Cli", "Cli.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <PropertyGroup>
                        <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
                    </PropertyGroup>
                    <ItemGroup>
                        <ProjectReference Include="..\Application\Application.csproj" />
                        <ProjectReference Include="..\..\..\External\Shared.csproj" />
                        <ProjectReference Include="$(SharedProjects)\Common.csproj" />
                    </ItemGroup>
                </Project>
                """
            );

            File.WriteAllText(
                Path.Combine(root, "src", "Application", "Application.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                    </PropertyGroup>
                </Project>
                """
            );

            var reader = CreateReader();

            var result = reader.Read(
                new ProjectContext("Example", root, false)
            );

            Assert.Equal(2, result.Count);

            var cli = Assert.Single(
                result,
                relationship => relationship.ProjectPath ==
                    Path.Combine("src", "Cli", "Cli.csproj")
            );

            Assert.Equal(
                ["net10.0", "net8.0"],
                cli.TargetFrameworks
            );

            Assert.Contains(
                cli.References,
                reference =>
                    reference.Path == "src/Application/Application.csproj"
                    && !reference.IsExternal
            );

            Assert.Contains(
                cli.References,
                reference =>
                    reference.Path == @"..\..\..\External\Shared.csproj"
                    && reference.IsExternal
            );

            Assert.Contains(
                cli.References,
                reference =>
                    reference.Path == @"$(SharedProjects)\Common.csproj"
                    && reference.IsExternal
            );

            var application = Assert.Single(
                result,
                relationship => relationship.ProjectPath ==
                    Path.Combine("src", "Application", "Application.csproj")
            );

            Assert.Equal(["net10.0"], application.TargetFrameworks);
            Assert.Empty(application.References);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_SkipsMalformedAndOversizedProjects()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Valid.csproj"),
                """
                <Project>
                    <PropertyGroup>
                        <TargetFramework>net48</TargetFramework>
                    </PropertyGroup>
                </Project>
                """
            );

            File.WriteAllText(
                Path.Combine(root, "Malformed.csproj"),
                "<Project><PropertyGroup>"
            );

            File.WriteAllText(
                Path.Combine(root, "Oversized.csproj"),
                "<Project>"
                + new string(' ', 1024 * 1024)
                + "</Project>"
            );

            var reader = CreateReader();

            var result = reader.Read(
                new ProjectContext("Example", root, false)
            );

            var relationship = Assert.Single(result);

            Assert.Equal("Valid.csproj", relationship.ProjectPath);
            Assert.Equal(["net48"], relationship.TargetFrameworks);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_ExtractsAssemblyReferences()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "SageIntegration.csproj"),
                """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net48</TargetFramework>
                </PropertyGroup>

                <ItemGroup>
                    <Reference Include="Sagede.OfficeLine.Engine">
                        <HintPath>C:\Sage\Assemblies\Sagede.OfficeLine.Engine.dll</HintPath>
                        <Private>False</Private>
                    </Reference>

                    <Reference Include="Sagede.OfficeLine.Data">
                        <HintPath>..\Libraries\Sagede.OfficeLine.Data.dll</HintPath>
                    </Reference>

                    <Reference Include="System.Data" />
                </ItemGroup>
            </Project>
            """
            );

            var reader = CreateReader();

            var result = reader.Read(
                new ProjectContext("SageIntegration", root, false)
            );

            var relationship = Assert.Single(result);

            Assert.Equal(
                "SageIntegration.csproj",
                relationship.ProjectPath
            );

            Assert.Equal(
                ["net48"],
                relationship.TargetFrameworks
            );

            Assert.Empty(relationship.References);

            Assert.Equal(
                [
                    "Sagede.OfficeLine.Data",
                "Sagede.OfficeLine.Engine",
                "System.Data"
                ],
                relationship.AssemblyReferences.Select(
                    reference => reference.Name
                )
            );

            var engine = Assert.Single(
                relationship.AssemblyReferences,
                reference => reference.Name == "Sagede.OfficeLine.Engine"
            );

            Assert.Equal(
                @"C:\Sage\Assemblies\Sagede.OfficeLine.Engine.dll",
                engine.HintPath
            );

            Assert.False(engine.CopyLocal);

            var data = Assert.Single(
                relationship.AssemblyReferences,
                reference => reference.Name == "Sagede.OfficeLine.Data"
            );

            Assert.Equal(
                @"..\Libraries\Sagede.OfficeLine.Data.dll",
                data.HintPath
            );

            Assert.Null(data.CopyLocal);

            var systemData = Assert.Single(
                relationship.AssemblyReferences,
                reference => reference.Name == "System.Data"
            );

            Assert.Null(systemData.HintPath);
            Assert.Null(systemData.CopyLocal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(
    @"C:\Program Files (x86)\Sage\Shared\Shared.csproj"
)]
    [InlineData(
    @"D:\Development\Shared\Shared.csproj"
)]
    [InlineData(
    @"\\fileserver\projects\Shared\Shared.csproj"
)]
    public void Read_ClassifiesWindowsAbsolutePathsAsExternal(
    string referencePath
)
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var projectFile = Path.Combine(
                root,
                "SageIntegration.csproj"
            );

            File.WriteAllText(
                projectFile,
                $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net48</TargetFramework>
                </PropertyGroup>
                <ItemGroup>
                    <ProjectReference Include="{referencePath}" />
                </ItemGroup>
            </Project>
            """
            );

            var reader = CreateReader();

            var result = reader.Read(
                new ProjectContext(
                    "SageIntegration",
                    root,
                    false
                )
            );

            var relationship = Assert.Single(result);
            var reference = Assert.Single(relationship.References);

            Assert.True(reference.IsExternal);

            Assert.Equal(
                referencePath,
                reference.Path
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static RepositoryRelationshipReader CreateReader()
    {
        var fileSystem = new ProjectFileSystem();

        return new RepositoryRelationshipReader(
            new RepositoryDiscoveryService(fileSystem),
            fileSystem
        );
    }

    private static string CreateTemporaryDirectory()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-relationships-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        return root;
    }
}
