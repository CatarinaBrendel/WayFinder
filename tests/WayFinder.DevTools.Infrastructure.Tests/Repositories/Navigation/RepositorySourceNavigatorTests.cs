using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Symbols;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.Navigation;
using WayFinder.DevTools.Infrastructure.Repositories.Symbols;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Navigation;

public sealed class RepositorySourceNavigatorTests
{
    [Fact]
    public void ReadLines_ReturnsRequestedRange()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Example.cs"),
                "line 1\r\nline 2\r\nline 3\r\nline 4\r\nline 5"
            );

            var navigator = new RepositorySourceNavigator(
                new ProjectFileSystem()
            );

            var result = navigator.ReadLines(
                new ProjectContext("Example", root, false),
                "Example.cs",
                startLine: 2,
                lineCount: 3
            );

            Assert.Equal(2, result.StartLine);
            Assert.Equal(4, result.EndLine);
            Assert.Equal("line 2\nline 3\nline 4", result.Content);
            Assert.False(result.Truncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(150, 200, true)]
    [InlineData(150, 20, false)]
    [InlineData(100, 200, false)]
    public void ReadLines_ReportsTruncationCorrectly(
        int requestedLines,
        int fileLines,
        bool expectedTruncated
    )
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var source = string.Join(
                "\n",
                Enumerable.Range(1, fileLines)
                    .Select(index => $"line {index}")
            );

            File.WriteAllText(
                Path.Combine(root, "Example.cs"),
                source
            );

            var navigator = new RepositorySourceNavigator(
                new ProjectFileSystem()
            );

            var result = navigator.ReadLines(
                new ProjectContext("Example", root, false),
                "Example.cs",
                startLine: 1,
                lineCount: requestedLines
            );

            Assert.Equal(
                Math.Min(Math.Min(requestedLines, fileLines), 100),
                result.Content.Split('\n').Length
            );

            Assert.Equal(expectedTruncated, result.Truncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ReadDeclaration_ReturnsRequestedMethod()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "CustomerService.cs"),
                """
            namespace SageIntegration.Services;

            public class CustomerService
            {
                public void Save()
                {
                    Console.WriteLine("Saved");
                }

                public void Delete()
                {
                    Console.WriteLine("Deleted");
                }
            }
            """
            );

            var navigator = new RepositorySourceNavigator(
                new ProjectFileSystem()
            );

            var result = navigator.ReadDeclaration(
                new ProjectContext("Example", root, false),
                "CustomerService.cs",
                "SageIntegration.Services.CustomerService.Save"
            );

            Assert.Equal(5, result.StartLine);
            Assert.Equal(8, result.EndLine);

            Assert.Contains("void Save()", result.Content);
            Assert.Contains("Console.WriteLine(\"Saved\")", result.Content);

            Assert.DoesNotContain("void Delete()", result.Content);

            Assert.False(result.Truncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ReadDeclaration_RejectsAmbiguousMethods()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "CustomerService.cs"),
                """
            namespace SageIntegration.Services;

            public class CustomerService
            {
                public void Save(int id) { }

                public void Save(string name) { }
            }
            """
            );

            var navigator = new RepositorySourceNavigator(
                new ProjectFileSystem()
            );

            var exception = Assert.Throws<InvalidOperationException>(() =>
                navigator.ReadDeclaration(
                    new ProjectContext("Example", root, false),
                    "CustomerService.cs",
                    "SageIntegration.Services.CustomerService.Save"
                )
            );

            Assert.Contains("ambiguous", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ReadDeclaration_TruncatesLargeMethods()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var statements = string.Join(
                "\n",
                Enumerable.Range(1, 150)
                    .Select(index => $"        Console.WriteLine({index});")
            );

            var source = $$"""
            namespace SageIntegration.Services;

            public class CustomerService
            {
                public void Process()
                {
            {{statements}}
                }
            }
            """;

            File.WriteAllText(
                Path.Combine(root, "CustomerService.cs"),
                source
            );

            var navigator = new RepositorySourceNavigator(
                new ProjectFileSystem()
            );

            var result = navigator.ReadDeclaration(
                new ProjectContext("Example", root, false),
                "CustomerService.cs",
                "SageIntegration.Services.CustomerService.Process"
            );

            Assert.Equal(100, result.Content.Split('\n').Length);
            Assert.True(result.Truncated);

            Assert.Contains("Console.WriteLine(1)", result.Content);
            Assert.DoesNotContain("Console.WriteLine(150)", result.Content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ReadDeclaration_ResolvesSymbolDiscoveredBySymbolReader()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "CustomerService.cs"),
                """
            namespace SageIntegration.Services;

            public class CustomerService
            {
                public void Save()
                {
                    Console.WriteLine("Saved");
                }
            }
            """
            );

            var project = new ProjectContext(
                "Example",
                root,
                false
            );

            var fileSystem = new ProjectFileSystem();

            var symbolReader = new RepositorySymbolReader(fileSystem);
            var navigator = new RepositorySourceNavigator(fileSystem);

            var symbols = symbolReader.Read(
                project,
                "CustomerService.cs"
            );

            var method = Assert.Single(
                symbols.Symbols,
                symbol =>
                    symbol.Kind == RepositorySymbolKind.Method
                    && symbol.Name == "Save"
            );

            var excerpt = navigator.ReadDeclaration(
                project,
                method.RelativePath,
                method.QualifiedName
            );

            Assert.Equal(method.Line, excerpt.StartLine);

            Assert.Contains(
                "Console.WriteLine(\"Saved\")",
                excerpt.Content
            );

            Assert.False(excerpt.Truncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-navigation-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        return root;
    }
}
