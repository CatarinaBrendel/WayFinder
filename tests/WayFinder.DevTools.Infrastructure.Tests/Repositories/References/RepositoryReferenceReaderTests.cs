using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.References;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.References;

public sealed class RepositoryReferenceReaderTests
{
    [Fact]
    public void Read_FindsInvocationsAndMemberAccess()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Services.cs"),
                """
                public class CustomerService
                {
                    public void CalculatePrice() { }

                    public void Execute()
                    {
                        CalculatePrice();
                        this.CalculatePrice();
                    }
                }
                """
            );

            var reader = CreateReader();

            var result = reader.Read(
                new ProjectContext("Example", root, false),
                "CalculatePrice"
            );

            Assert.Equal(2, result.References.Count);
            Assert.All(
                result.References,
                reference => Assert.Equal(
                    "Invocation",
                    reference.Kind
                )
            );

            Assert.False(result.Truncated);
            Assert.Equal("Syntactic", result.Resolution);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_FindsObjectCreation()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Services.cs"),
                """
                public class CustomerService { }

                public class Consumer
                {
                    public void Execute()
                    {
                        var service = new CustomerService();
                    }
                }
                """
            );

            var result = CreateReader().Read(
                new ProjectContext("Example", root, false),
                "CustomerService"
            );

            Assert.Single(result.References);
            Assert.Equal(
                "ObjectCreation",
                result.References[0].Kind
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_FindsGenericTypeReferences()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Services.cs"),
                """
                public interface IRepository<T> { }

                public class Customer { }

                public class Consumer
                {
                    private IRepository<Customer>? repository;
                }
                """
            );

            var result = CreateReader().Read(
                new ProjectContext("Example", root, false),
                "IRepository"
            );

            Assert.Single(result.References);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_DoesNotIncludeDeclarations()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Services.cs"),
                """
                public class CustomerService
                {
                    public void Execute() { }
                }
                """
            );

            var result = CreateReader().Read(
                new ProjectContext("Example", root, false),
                "Execute"
            );

            Assert.Empty(result.References);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_FindsQualifiedGenericTypeReferences()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Services.cs"),
                """
            namespace Services
            {
                public interface IRepository<T> { }
            }

            public class Consumer
            {
                private Services.IRepository<string>? repository;
            }
            """
            );

            var result = CreateReader().Read(
                new ProjectContext("Example", root, false),
                "IRepository"
            );

            var reference = Assert.Single(result.References);

            Assert.Equal("Identifier", reference.Kind);
            Assert.Equal("Services.cs", reference.RelativePath);
            Assert.False(result.Truncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_FindsGenericMethodInvocations()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Services.cs"),
                """
            public class Service
            {
                public T Get<T>() => default!;

                public void Execute()
                {
                    var result = Get<string>();
                    var other = this.Get<int>();
                }
            }
            """
            );

            var result = CreateReader().Read(
                new ProjectContext("Example", root, false),
                "Get"
            );

            Assert.Equal(2, result.References.Count);

            Assert.All(
                result.References,
                reference => Assert.Equal(
                    "Invocation",
                    reference.Kind
                )
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(200, false)]
    [InlineData(201, true)]
    public void Read_ReportsReferenceTruncation(
        int referenceCount,
        bool expectedTruncated
)
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var invocations = string.Join(
                Environment.NewLine,
                Enumerable.Repeat(
                    "Target();",
                    referenceCount
                )
            );

            File.WriteAllText(
                Path.Combine(root, "Services.cs"),
                $$"""
            public class Service
            {
                public void Target() { }

                public void Execute()
                {
                    {{invocations}}
                }
            }
            """
            );

            var result = CreateReader().Read(
                new ProjectContext("Example", root, false),
                "Target"
            );

            Assert.Equal(200, result.References.Count);
            Assert.Equal(expectedTruncated, result.Truncated);
            Assert.Equal(1, result.FilesExamined);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_ReportsTruncationForOversizedFiles()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "Large.cs"),
                new string(' ', 1024 * 1024 + 1)
            );

            var result = CreateReader().Read(
                new ProjectContext("Example", root, false),
                "Target"
            );

            Assert.Empty(result.References);
            Assert.Equal(1, result.FilesExamined);
            Assert.True(result.Truncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_ReportsTruncationWhenFileLimitIsExceeded()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            for (var index = 0; index < 1001; index++)
            {
                File.WriteAllText(
                    Path.Combine(root, $"File{index:D4}.cs"),
                    "public class Example { }"
                );
            }

            var result = CreateReader().Read(
                new ProjectContext("Example", root, false),
                "Target"
            );

            Assert.Empty(result.References);
            Assert.Equal(1000, result.FilesExamined);
            Assert.True(result.Truncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_ContinuesAfterMalformedUtf16File()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            // UTF-16 LE BOM followed by an unmatched high surrogate.
            File.WriteAllBytes(
                Path.Combine(root, "A_Invalid.cs"),
                [0xFF, 0xFE, 0x00, 0xD8]
            );

            File.WriteAllText(
                Path.Combine(root, "B_Valid.cs"),
                """
            public class Service
            {
                public void Target() { }

                public void Execute()
                {
                    Target();
                }
            }
            """
            );

            var result = CreateReader().Read(
                new ProjectContext("Example", root, false),
                "Target"
            );

            var reference = Assert.Single(result.References);

            Assert.Equal("B_Valid.cs", reference.RelativePath);
            Assert.Equal("Invocation", reference.Kind);

            Assert.Equal(2, result.FilesExamined);
            Assert.True(result.Truncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static RepositoryReferenceReader CreateReader()
    {
        return new RepositoryReferenceReader(
            new ProjectFileSystem()
        );
    }

    private static string CreateTemporaryDirectory()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-references-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(root);

        return root;
    }
}
