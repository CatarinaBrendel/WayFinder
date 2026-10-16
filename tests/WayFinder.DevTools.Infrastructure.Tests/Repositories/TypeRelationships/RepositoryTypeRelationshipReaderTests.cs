using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.TypeRelationships;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.TypeRelationships;

public sealed class RepositoryTypeRelationshipReaderTests
{
    [Fact]
    public void Read_DiscoversDeclaredBaseTypes()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Services.cs"),
                """
                namespace SageIntegration.Services;

                public abstract class BaseService { }

                public interface ICustomerService { }

                public interface IAdvancedService : ICustomerService { }

                public class CustomerService
                    : BaseService, ICustomerService
                {
                    public class NestedService : BaseService { }
                }

                public record CustomerRecord : ICustomerService;

                public struct CustomerId : IEquatable<CustomerId>
                {
                    public bool Equals(CustomerId other) => true;
                }
                """
            );

            var reader = new RepositoryTypeRelationshipReader(
                new ProjectFileSystem()
            );

            var result = reader.Read(
                new ProjectContext("Example", root, false),
                "Services.cs"
            );

            Assert.Contains(
                result.Relationships,
                relationship =>
                    relationship.QualifiedName ==
                        "SageIntegration.Services.CustomerService"
                    && relationship.DeclaredBaseTypes.SequenceEqual(
                        ["BaseService", "ICustomerService"]
                    )
            );

            Assert.Contains(
                result.Relationships,
                relationship =>
                    relationship.QualifiedName ==
                        "SageIntegration.Services.IAdvancedService"
                    && relationship.DeclaredBaseTypes.SequenceEqual(
                        ["ICustomerService"]
                    )
            );

            Assert.Contains(
                result.Relationships,
                relationship =>
                    relationship.QualifiedName ==
                        "SageIntegration.Services.CustomerService.NestedService"
                    && relationship.DeclaredBaseTypes.SequenceEqual(
                        ["BaseService"]
                    )
            );

            Assert.Contains(
                result.Relationships,
                relationship =>
                    relationship.QualifiedName ==
                        "SageIntegration.Services.CustomerRecord"
                    && relationship.DeclaredBaseTypes.SequenceEqual(
                        ["ICustomerService"]
                    )
            );

            Assert.Contains(
                result.Relationships,
                relationship =>
                    relationship.QualifiedName ==
                        "SageIntegration.Services.CustomerId"
                    && relationship.DeclaredBaseTypes.SequenceEqual(
                        ["IEquatable<CustomerId>"]
                    )
            );

            Assert.DoesNotContain(
                result.Relationships,
                relationship =>
                    relationship.Name == "BaseService"
                    || relationship.Name == "ICustomerService"
            );

            Assert.False(result.Truncated);
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

            var reader = new RepositoryTypeRelationshipReader(
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
    public void Read_ReportsRelationshipTruncation(
        int relationshipCount,
        bool expectedTruncated
)
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var source = string.Join(
                Environment.NewLine,
                Enumerable.Range(1, relationshipCount)
                    .Select(index =>
                        $"public class Class{index} : BaseClass {{ }}")
            );

            File.WriteAllText(
                Path.Combine(root, "ManyRelationships.cs"),
                source
            );

            var reader = new RepositoryTypeRelationshipReader(
                new ProjectFileSystem()
            );

            var result = reader.Read(
                new ProjectContext("Example", root, false),
                "ManyRelationships.cs"
            );

            Assert.Equal(500, result.Relationships.Count);
            Assert.Equal(expectedTruncated, result.Truncated);

            Assert.Equal(
                "Class1",
                result.Relationships[0].Name
            );

            Assert.Equal(
                "Class500",
                result.Relationships[^1].Name
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
            $"wayfinder-type-relationships-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        return root;
    }
}
