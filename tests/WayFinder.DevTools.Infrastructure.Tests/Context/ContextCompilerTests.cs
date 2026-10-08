using WayFinder.DevTools.Application.Context;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Context;
using WayFinder.DevTools.Infrastructure.Context.Estimation;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.Reading;
using WayFinder.DevTools.Infrastructure.Repositories.Searching;
using Xunit;

namespace WayFinder.DevTools.Infrastructure.Tests.Context;

public sealed class ContextCompilerTests
    : IDisposable
{
    private readonly string _rootPath;
    private readonly ProjectContext _project;
    private readonly ContextCompiler _compiler;

    public ContextCompilerTests()
    {
        _rootPath =
            Path.Combine(
                Path.GetTempPath(),
                $"wayfinder-context-compiler-{Guid.NewGuid():N}"
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

        var fileReader =
            new RepositoryFileReader(
                fileSystem
            );

        var tokenEstimator =
            new ApproximateTokenEstimator();

        _compiler =
            new ContextCompiler(
                fileSystem,
                fileReader,
                tokenEstimator
            );
    }

    [Fact]
    public void Compile_SelectsRelevantFiles()
    {
        WriteFile(
            "src/Doctor.cs",
            """
            public sealed class Doctor
            {
                public void ExamineProject()
                {
                }
            }
            """
        );

        WriteFile(
            "src/Unrelated.cs",
            """
            public sealed class Unrelated
            {
            }
            """
        );

        var result =
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "Fix doctor project handling"
                )
            );

        Assert.Contains(
            result.Files,
            file =>
                file.Path == "src/Doctor.cs"
        );

        Assert.DoesNotContain(
            result.Files,
            file =>
                file.Path == "src/Unrelated.cs"
        );
    }

    [Fact]
    public void Compile_IncludesAgentsAsGuidance()
    {
        WriteFile(
            "AGENTS.md",
            """
            # Repository instructions

            Keep infrastructure deterministic.
            """
        );

        WriteFile(
            "src/Doctor.cs",
            """
            public sealed class Doctor
            {
            }
            """
        );

        var result =
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "Fix doctor"
                )
            );

        Assert.NotNull(
            result.Guidance
        );

        Assert.Equal(
            "AGENTS.md",
            result.Guidance.Path
        );

        Assert.Contains(
            "Keep infrastructure deterministic.",
            result.Guidance.Content
        );
    }

    [Fact]
    public void Compile_DoesNotIncludeGuidanceAsNormalFile()
    {
        WriteFile(
            "AGENTS.md",
            """
            Doctor project instructions.
            """
        );

        WriteFile(
            "src/Doctor.cs",
            """
            public sealed class Doctor
            {
            }
            """
        );

        var result =
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "Fix doctor project"
                )
            );

        Assert.NotNull(
            result.Guidance
        );

        Assert.DoesNotContain(
            result.Files,
            file =>
                file.Path == "AGENTS.md"
        );
    }

    [Fact]
    public void Compile_RespectsTokenBudget()
    {
        WriteFile(
            "src/Doctor.cs",
            new string(
                'x',
                400
            ) + " doctor"
        );

        var result =
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "doctor",
                    TokenBudget: 50
                )
            );

        Assert.Empty(
            result.Files
        );

        Assert.True(
            result.Statistics.EstimatedTokens
            <= result.Statistics.TokenBudget
        );
    }

    [Fact]
    public void Compile_SkipsLargeCandidateAndIncludesLaterFileThatFits()
    {
        WriteFile(
            "src/DoctorLarge.cs",
            "doctor project "
            + new string(
                'x',
                1_000
            )
        );

        WriteFile(
            "src/DoctorSmall.cs",
            "doctor"
        );

        var result =
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "doctor project",
                    TokenBudget: 20
                )
            );

        Assert.DoesNotContain(
            result.Files,
            file =>
                file.Path == "src/DoctorLarge.cs"
        );

        Assert.Contains(
            result.Files,
            file =>
                file.Path == "src/DoctorSmall.cs"
        );
    }

    [Fact]
    public void Compile_ReportsStatistics()
    {
        WriteFile(
            "src/Doctor.cs",
            "doctor"
        );

        var result =
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "doctor",
                    TokenBudget: 8_000
                )
            );

        Assert.Equal(
            result.Files.Count,
            result.Statistics.FileCount
        );

        Assert.Equal(
            8_000,
            result.Statistics.TokenBudget
        );

        Assert.True(
            result.Statistics.EstimatedTokens > 0
        );

        Assert.True(
            result.Statistics.EstimatedTokens
            <= result.Statistics.TokenBudget
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

    [Fact]
    public void Compile_ReportsSelectionEvidence()
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
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "doctor project"
                )
            );

        var selection =
            Assert.Single(
                result.Selections
            );

        Assert.Equal(
            "src/Doctor.cs",
            selection.Path
        );

        Assert.Equal(
            90,
            selection.Score
        );

        Assert.Equal(
            ["doctor"],
            selection.PathMatchedTerms
        );

        Assert.Equal(
            ["doctor", "project"],
            selection.ContentMatchedTerms
        );

        Assert.True(
            selection.Included
        );
    }

    [Fact]
    public void Compile_PrefersCompleteFileWhenItFitsBudget()
    {
        WriteFile(
            "src/Doctor.cs",
            """
        public sealed class Doctor
        {
            public void ExamineDoctor()
            {
            }
        }
        """
        );

        var result =
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "doctor",
                    TokenBudget: 8_000
                )
            );

        var file =
            Assert.Single(
                result.Files
            );

        Assert.Equal(
            "src/Doctor.cs",
            file.Path
        );

        Assert.Equal(
            ContextFileKind.Complete,
            file.Kind
        );

        Assert.Null(
            file.StartLine
        );

        Assert.Null(
            file.EndLine
        );
    }

    [Fact]
    public void Compile_FallsBackToExcerptWhenCompleteFileDoesNotFit()
    {
        var lines =
            Enumerable.Range(
                    1,
                    100
                )
                .Select(
                    lineNumber =>
                        lineNumber == 50
                            ? "doctor"
                            : $"line {lineNumber} padding"
                );

        WriteFile(
            "src/Doctor.cs",
            string.Join(
                '\n',
                lines
            )
        );

        var result =
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "doctor",
                    TokenBudget: 200
                )
            );

        var file =
            Assert.Single(
                result.Files
            );

        Assert.Equal(
            ContextFileKind.Excerpt,
            file.Kind
        );

        Assert.Equal(
            30,
            file.StartLine
        );

        Assert.Equal(
            70,
            file.EndLine
        );

        Assert.Contains(
            "doctor",
            file.Content
        );

        Assert.True(
            file.EstimatedTokens <= 200
        );
    }

    [Fact]
    public void Compile_CountsSourceFilesRatherThanExcerpts()
    {
        var lines =
            Enumerable.Range(
                    1,
                    400
                )
                .Select(
                    lineNumber =>
                        lineNumber is 100 or 300
                            ? "doctor"
                            : "padding"
                );

        WriteFile(
            "src/Doctor.cs",
            string.Join(
                '\n',
                lines
            )
        );

        var result =
            _compiler.Compile(
                _project,
                new ContextRequest(
                    "doctor",
                    TokenBudget: 200
                )
            );

        Assert.Equal(
            2,
            result.Files.Count
        );

        Assert.All(
            result.Files,
            file =>
                Assert.Equal(
                    ContextFileKind.Excerpt,
                    file.Kind
                )
        );

        Assert.Equal(
            1,
            result.Statistics.FileCount
        );
    }

}
