using System.Text;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Symbols;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.Symbols;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Symbols;

public sealed class RepositorySymbolReaderTests
{
    [Fact]
    public void Read_DiscoversCSharpDeclarations()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(
                Path.Combine(root, "Services")
            );

            File.WriteAllText(
                Path.Combine(root, "Services", "CustomerService.cs"),
                """
                namespace SageIntegration.Services;

                public interface ICustomerService
                {
                    void Save();
                }

                public sealed class CustomerService : ICustomerService
                {
                    public void Save() { }

                    public Customer? GetCustomer(int id) => null;

                    public record Customer(int Id);

                    public record struct CustomerId(int Value);

                    public enum CustomerStatus
                    {
                        Active,
                        Inactive
                    }
                }
                """
            );

            var reader = new RepositorySymbolReader(
                new ProjectFileSystem()
            );

            var response = reader.Read(
                new ProjectContext("Example", root, false),
                Path.Combine("Services", "CustomerService.cs")
            );

            Assert.False(response.Truncated);

            var result = response.Symbols;

            Assert.Contains(
                result,
                symbol =>
                    symbol.Name == "SageIntegration.Services"
                    && symbol.Kind == RepositorySymbolKind.Namespace
                    && symbol.Line == 1
            );

            Assert.Contains(
                result,
                symbol =>
                    symbol.Name == "ICustomerService"
                    && symbol.QualifiedName ==
                        "SageIntegration.Services.ICustomerService"
                    && symbol.Kind == RepositorySymbolKind.Interface
                    && symbol.Line == 3
            );

            Assert.Contains(
                result,
                symbol =>
                    symbol.Name == "CustomerService"
                    && symbol.QualifiedName ==
                        "SageIntegration.Services.CustomerService"
                    && symbol.Kind == RepositorySymbolKind.Class
                    && symbol.Line == 8
            );

            Assert.Contains(
                result,
                symbol =>
                    symbol.Name == "GetCustomer"
                    && symbol.QualifiedName ==
                        "SageIntegration.Services.CustomerService.GetCustomer"
                    && symbol.Kind == RepositorySymbolKind.Method
                    && symbol.Line == 12
            );

            Assert.Contains(
                result,
                symbol =>
                    symbol.Name == "Customer"
                    && symbol.Kind == RepositorySymbolKind.Record
            );

            Assert.Contains(
                result,
                symbol =>
                    symbol.Name == "CustomerId"
                    && symbol.Kind == RepositorySymbolKind.Struct
            );

            Assert.Contains(
                result,
                symbol =>
                    symbol.Name == "CustomerStatus"
                    && symbol.Kind == RepositorySymbolKind.Enum
            );

            Assert.All(
                result,
                symbol => Assert.Equal(
                    Path.Combine("Services", "CustomerService.cs"),
                    symbol.RelativePath
                )
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_RejectsOversizedSourceFiles()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Large.cs"),
                new string(' ', 1024 * 1024 + 1)
            );

            var reader = new RepositorySymbolReader(
                new ProjectFileSystem()
            );

            Assert.Throws<InvalidOperationException>(() =>
                reader.Read(
                    new ProjectContext("Example", root, false),
                    "Large.cs"
                )
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(500, false)]
    [InlineData(501, true)]
    public void Read_ReportsSymbolTruncation(
        int symbolCount,
        bool expectedTruncated
    )
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var source = string.Join(
                Environment.NewLine,
                Enumerable.Range(1, symbolCount)
                    .Select(index => $"public class Class{index} {{ }}")
            );

            File.WriteAllText(
                Path.Combine(root, "ManySymbols.cs"),
                source
            );

            var reader = new RepositorySymbolReader(
                new ProjectFileSystem()
            );

            var result = reader.Read(
                new ProjectContext("Example", root, false),
                "ManySymbols.cs"
            );

            Assert.Equal(500, result.Symbols.Count);
            Assert.Equal(expectedTruncated, result.Truncated);

            Assert.Equal(
                "Class1",
                result.Symbols[0].Name
            );

            Assert.Equal(
                "Class500",
                result.Symbols[^1].Name
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("utf8")]
    [InlineData("utf8-bom")]
    [InlineData("utf16-le")]
    [InlineData("utf16-be")]
    [InlineData("windows-1252")]
    public void Read_DiscoversSymbolsWithDifferentEncodings(string encodingName)
    {
        var root = CreateTemporaryDirectory();

        try
        {
            const string source = """
            namespace Geschäftslogik;

            public class Kundenverwaltung
            {
                public void Überprüfen() { }
            }
            """;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var encoding = encodingName switch
            {
                "utf8" => new UTF8Encoding(false),
                "utf8-bom" => new UTF8Encoding(true),
                "utf16-le" => new UnicodeEncoding(false, true),
                "utf16-be" => new UnicodeEncoding(true, true),
                "windows-1252" => Encoding.GetEncoding(1252),
                _ => throw new ArgumentOutOfRangeException(nameof(encodingName))
            };

            var path = Path.Combine(root, "Kundenverwaltung.cs");

            var preamble = encoding.GetPreamble();
            var content = encoding.GetBytes(source);

            File.WriteAllBytes(
                path,
                [.. preamble, .. content]
            );

            var reader = new RepositorySymbolReader(
                new ProjectFileSystem()
            );

            var result = reader.Read(
                new ProjectContext("Example", root, false),
                "Kundenverwaltung.cs"
            );

            Assert.False(result.Truncated);

            Assert.Contains(
                result.Symbols,
                symbol =>
                    symbol.Kind == RepositorySymbolKind.Class
                    && symbol.QualifiedName ==
                        "Geschäftslogik.Kundenverwaltung"
            );

            Assert.Contains(
                result.Symbols,
                symbol =>
                    symbol.Kind == RepositorySymbolKind.Method
                    && symbol.QualifiedName ==
                        "Geschäftslogik.Kundenverwaltung.Überprüfen"
            );
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
            $"wayfinder-symbols-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        return root;
    }
}
