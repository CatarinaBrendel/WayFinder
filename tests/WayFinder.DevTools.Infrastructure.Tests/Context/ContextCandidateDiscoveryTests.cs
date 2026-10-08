using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Context;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.Searching;
using Xunit;

namespace WayFinder.DevTools.Infrastructure.Tests.Context;

public sealed class ContextCandidateDiscoveryTests
    : IDisposable
{
    private readonly string _rootPath;
    private readonly ProjectContext _project;
    private readonly ContextCandidateDiscovery _discovery;

    public ContextCandidateDiscoveryTests()
    {
        _rootPath =
            Path.Combine(
                Path.GetTempPath(),
                $"wayfinder-context-{Guid.NewGuid():N}"
            );

        Directory.CreateDirectory(
            _rootPath
        );

        _project =
            new ProjectContext(
                "TestProject",
                _rootPath,
                true
            );

        var fileSystem =
            new ProjectFileSystem();

        var searcher =
            new RepositorySearcher(
                fileSystem
            );

        _discovery =
            new ContextCandidateDiscovery(
                fileSystem,
                searcher
            );
    }

    [Fact]
    public void Discover_FindsFileNameMatch()
    {
        WriteFile(
            "src/Doctor.cs",
            "public sealed class HealthCheck { }"
        );

        var result =
            _discovery.Discover(
                _project,
                ["doctor"]
            );

        var candidate =
            Assert.Single(result);

        Assert.Equal(
            "src/Doctor.cs",
            candidate.Path
        );

        Assert.Equal(
            ["doctor"],
            candidate.PathMatchedTerms
        );

        Assert.Empty(
            candidate.ContentMatchedTerms
        );
    }

    [Fact]
    public void Discover_FindsContentMatch()
    {
        WriteFile(
            "src/HealthCheck.cs",
            """
            public sealed class HealthCheck
            {
                private readonly Doctor _doctor;
            }
            """
        );

        var result =
            _discovery.Discover(
                _project,
                ["doctor"]
            );

        var candidate =
            Assert.Single(result);

        Assert.Equal(
            "src/HealthCheck.cs",
            candidate.Path
        );

        Assert.Empty(
            candidate.PathMatchedTerms
        );

        Assert.Equal(
            ["doctor"],
            candidate.ContentMatchedTerms
        );
    }

    [Fact]
    public void Discover_MergesPathAndContentMatchesForSameFile()
    {
        WriteFile(
            "src/Doctor.cs",
            """
            public sealed class Doctor
            {
                private readonly ProjectRegistry _registry;
            }
            """
        );

        var result =
            _discovery.Discover(
                _project,
                ["doctor", "project"]
            );

        var candidate =
            Assert.Single(result);

        Assert.Equal(
            "src/Doctor.cs",
            candidate.Path
        );

        Assert.Equal(
            ["doctor"],
            candidate.PathMatchedTerms
        );

        Assert.Equal(
            ["doctor", "project"],
            candidate.ContentMatchedTerms
        );
    }

    [Fact]
    public void Discover_ReturnsCandidatesInDeterministicPathOrder()
    {
        WriteFile(
            "src/Zeta.cs",
            "Doctor doctor;"
        );

        WriteFile(
            "src/Alpha.cs",
            "Doctor doctor;"
        );

        WriteFile(
            "tests/DoctorTests.cs",
            "Doctor doctor;"
        );

        var result =
            _discovery.Discover(
                _project,
                ["doctor"]
            );

        Assert.Equal(
            [
                "src/Alpha.cs",
                "src/Zeta.cs",
                "tests/DoctorTests.cs",
            ],
            result.Select(
                candidate => candidate.Path
            )
        );
    }

    [Fact]
    public void Discover_DoesNotTreatDirectoryNameAsPathMatch()
    {
        WriteFile(
            "src/Projects/HealthCheck.cs",
            "public sealed class HealthCheck { }"
        );

        var result =
            _discovery.Discover(
                _project,
                ["project"]
            );

        Assert.Empty(
            result
        );
    }

    [Fact]
    public void Discover_PreservesContentMatchLineNumbers()
    {
        WriteFile(
            "src/Doctor.cs",
            """
        public sealed class Doctor
        {
            public void Run()
            {
                Doctor();
            }
        }
        """
        );

        var result =
            _discovery.Discover(
                _project,
                ["doctor"]
            );

        var candidate =
            Assert.Single(
                result
            );

        Assert.Equal(
            [
                new ContextContentMatch(
                "doctor",
                1
            ),
            new ContextContentMatch(
                "doctor",
                5
            ),
        ],
            candidate.ContentMatches
        );
    }

    [Fact]
    public void Discover_OrdersContentMatchesByLineThenTerm()
    {
        WriteFile(
            "src/HealthCheck.cs",
            """
        project doctor
        doctor
        project
        """
        );

        var result =
            _discovery.Discover(
                _project,
                [
                    "project",
                "doctor",
                ]
            );

        var candidate =
            Assert.Single(
                result
            );

        Assert.Equal(
            [
                new ContextContentMatch(
                "doctor",
                1
            ),
            new ContextContentMatch(
                "project",
                1
            ),
            new ContextContentMatch(
                "doctor",
                2
            ),
            new ContextContentMatch(
                "project",
                3
            ),
        ],
            candidate.ContentMatches
        );
    }

    [Fact]
    public void Discover_MatchesCompleteFilenameComponent()
    {
        WriteFile(
            "src/TimedNetworkScanOperation.cs",
            "public sealed class Example { }"
        );

        var result =
            _discovery.Discover(
                _project,
                ["timed"]
            );

        var candidate =
            Assert.Single(result);

        Assert.Equal(
            ["timed"],
            candidate.PathMatchedTerms
        );
    }

    [Fact]
    public void Discover_DoesNotMatchTermInsideFilenameComponent()
    {
        WriteFile(
            "src/FileReadDiscoveryHandler.cs",
            "public sealed class Example { }"
        );

        var result =
            _discovery.Discover(
                _project,
                ["over"]
            );

        Assert.Empty(result);
    }

    [Fact]
    public void Discover_DoesNotPrefixMatchFilenameComponent()
    {
        WriteFile(
            "src/TimedNetworkScanOperation.cs",
            "public sealed class Example { }"
        );

        var result =
            _discovery.Discover(
                _project,
                ["time"]
            );

        Assert.Empty(result);
    }

    [Fact]
    public void Discover_MatchesCamelCaseFilenameComponent()
    {
        WriteFile(
            "src/DefenderAwarenessHandler.cs",
            "public sealed class Example { }"
        );

        var result =
            _discovery.Discover(
                _project,
                ["awareness"]
            );

        var candidate =
            Assert.Single(result);

        Assert.Equal(
            ["awareness"],
            candidate.PathMatchedTerms
        );
    }

    [Fact]
    public void Discover_MatchesAcronymFilenameComponent()
    {
        WriteFile(
            "src/HTTPClientFactory.cs",
            "public sealed class Example { }"
        );

        var result =
            _discovery.Discover(
                _project,
                ["http", "client"]
            );

        var candidate =
            Assert.Single(result);

        Assert.Equal(
            ["client", "http"],
            candidate.PathMatchedTerms
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(
                _rootPath))
        {
            Directory.Delete(
                _rootPath,
                recursive: true
            );
        }
    }

    private void WriteFile(
        string relativePath,
        string content
    )
    {
        var path =
            Path.Combine(
                _rootPath,
                relativePath
            );

        var directory =
            Path.GetDirectoryName(
                path
            );

        if (directory is not null)
        {
            Directory.CreateDirectory(
                directory
            );
        }

        File.WriteAllText(
            path,
            content
        );
    }

}
