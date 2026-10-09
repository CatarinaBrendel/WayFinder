using System.Diagnostics;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitDiffReaderTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly GitDiffReader _reader = new();

    public GitDiffReaderTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-diff-reader-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);

        Git("init", "-q");
        Git("config", "user.name", "WayFinder Tests");
        Git("config", "user.email", "wayfinder@example.invalid");
        Git("symbolic-ref", "HEAD", "refs/heads/main");

        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "original\n"
        );

        File.WriteAllText(
            Path.Combine(_root, "other.txt"),
            "unchanged\n"
        );

        Git("add", ".");
        Git("commit", "-qm", "Initial commit");

        _project = new ProjectContext(
            Name: "TestRepository",
            RootPath: _root,
            IsGitRepository: true
        );
    }

    [Fact]
    public void Read_ValidRequest_ReturnsRepositoryMetadata()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "modified\n"
        );

        var result = _reader.Read(
            _project,
            new GitDiffRequest()
        );

        Assert.Equal("TestRepository", result.RepositoryName);
        Assert.Equal("main", result.Branch);
        Assert.False(result.Staged);
        Assert.False(result.StatOnly);
        Assert.Null(result.Path);
        Assert.False(result.Truncated);
        Assert.Contains("+modified", result.Content);
    }

    [Fact]
    public void Read_StagedRequest_ReturnsStagedChanges()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "staged\n"
        );

        Git("add", "example.txt");

        var result = _reader.Read(
            _project,
            new GitDiffRequest(Staged: true)
        );

        Assert.True(result.Staged);
        Assert.Contains("+staged", result.Content);
    }

    [Fact]
    public void Read_StatOnlyRequest_ReturnsSummary()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "modified\n"
        );

        var result = _reader.Read(
            _project,
            new GitDiffRequest(StatOnly: true)
        );

        Assert.True(result.StatOnly);
        Assert.Contains("1 file changed", result.Content);
        Assert.DoesNotContain("diff --git", result.Content);
    }

    [Fact]
    public void Read_PathFilter_ReturnsOnlyRequestedFile()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "modified\n"
        );

        File.WriteAllText(
            Path.Combine(_root, "other.txt"),
            "also modified\n"
        );

        var result = _reader.Read(
            _project,
            new GitDiffRequest(Path: "example.txt")
        );

        Assert.Equal("example.txt", result.Path);
        Assert.Contains("example.txt", result.Content);
        Assert.DoesNotContain("other.txt", result.Content);
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("../../outside.txt")]
    [InlineData("src/../../../outside.txt")]
    public void Read_PathTraversal_Throws(string path)
    {
        Assert.Throws<ArgumentException>(
            () => _reader.Read(
                _project,
                new GitDiffRequest(Path: path)
            )
        );
    }

    [Theory]
    [InlineData(":(top)**")]
    [InlineData(":(exclude)example.txt")]
    [InlineData("*.cs")]
    [InlineData("example?.txt")]
    [InlineData("[abc].txt")]
    public void Read_GitPathspecSyntax_Throws(string path)
    {
        Assert.Throws<ArgumentException>(
            () => _reader.Read(
                _project,
                new GitDiffRequest(Path: path)
            )
        );
    }

    [Fact]
    public void Read_AbsolutePath_Throws()
    {
        var path = Path.Combine(
            _root,
            "example.txt"
        );

        Assert.Throws<ArgumentException>(
            () => _reader.Read(
                _project,
                new GitDiffRequest(Path: path)
            )
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Read_EmptyPath_Throws(string path)
    {
        Assert.Throws<ArgumentException>(
            () => _reader.Read(
                _project,
                new GitDiffRequest(Path: path)
            )
        );
    }

    [Fact]
    public void Read_NonGitProject_Throws()
    {
        var project = _project with
        {
            IsGitRepository = false
        };

        Assert.Throws<InvalidOperationException>(
            () => _reader.Read(
                project,
                new GitDiffRequest()
            )
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Read_InvalidOutputLimit_Throws(int limit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _reader.Read(
                _project,
                new GitDiffRequest(MaxOutputBytes: limit)
            )
        );
    }

    [Fact]
    public void Read_TruncatedOutput_ReportsTruncation()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            string.Join(
                '\n',
                Enumerable.Range(0, 1000)
                    .Select(index => $"line {index}")
            )
        );

        var result = _reader.Read(
            _project,
            new GitDiffRequest(MaxOutputBytes: 128)
        );

        Assert.True(result.Truncated);

        Assert.True(
            System.Text.Encoding.UTF8.GetByteCount(result.Content)
            <= 128
        );
    }

    private void Git(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Could not start Git."
            );

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        process.WaitForExit();

        _ = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Git test setup failed: {error}"
            );
        }
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }
}
