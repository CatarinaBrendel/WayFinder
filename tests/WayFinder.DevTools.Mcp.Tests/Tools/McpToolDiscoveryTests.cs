using ModelContextProtocol.Client;

namespace WayFinder.DevTools.Mcp.Tests.Tools;

public sealed class McpToolDiscoveryTests
{
    [Fact]
    public async Task Server_AdvertisesRepositoryIntelligenceTools()
    {
        var repositoryRoot = FindRepositoryRoot();

        var transport = new StdioClientTransport(
            new StdioClientTransportOptions
            {
                Name = "WayFinder Test Server",
                Command = "dotnet",
                Arguments =
                [
                    "run",
                    "--no-build",
                    "--project",
                    Path.Combine(
                        repositoryRoot,
                        "src",
                        "WayFinder.DevTools.Mcp",
                        "WayFinder.DevTools.Mcp.csproj"
                    )
                ],
                WorkingDirectory = repositoryRoot
            }
        );

        await using var client =
            await McpClient.CreateAsync(transport);

        var tools = await client.ListToolsAsync();

        var toolNames = tools
            .Select(tool => tool.Name)
            .ToHashSet(StringComparer.Ordinal);

        string[] expectedTools =
        [
            "repo_discover",
            "repo_relationships",
            "repo_symbols",
            "repo_source_lines",
            "repo_source_declaration"
        ];

        foreach (var expectedTool in expectedTools)
        {
            Assert.Contains(expectedTool, toolNames);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(
            AppContext.BaseDirectory
        );

        while (directory is not null)
        {
            if (Directory.Exists(
                Path.Combine(
                    directory.FullName,
                    "src",
                    "WayFinder.DevTools.Mcp"
                )))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the WayFinder repository root."
        );
    }
}
