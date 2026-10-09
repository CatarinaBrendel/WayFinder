using System.Diagnostics;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitLogReaderTests : IDisposable
{
    private readonly string _root;
    private readonly GitLogReader _reader = new();

    public GitLogReaderTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-git-log-reader-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);

        Git("init", "-q");
        Git("config", "user.name", "WayFinder Tests");
        Git("config", "user.email", "wayfinder@example.invalid");
        Git("symbolic-ref", "HEAD", "refs/heads/main");
    }

    [Fact]
    public void Read_EmptyRepository_ReturnsEmptyHistory()
    {
        var result = _reader.Read(
            Project(),
            new GitLogRequest()
        );

        Assert.Equal(
            Path.GetFileName(_root),
            result.RepositoryName
        );

        Assert.Empty(result.Commits);
    }

    [Fact]
    public void Read_SingleCommit_ReturnsTypedMetadata()
    {
        CommitFile(
            "example.txt",
            "content",
            "Initial commit"
        );

        var result = _reader.Read(
            Project(),
            new GitLogRequest()
        );

        var commit = Assert.Single(result.Commits);

        Assert.Equal("Initial commit", commit.Subject);
        Assert.Equal("WayFinder Tests", commit.AuthorName);
        Assert.Equal(
            "wayfinder@example.invalid",
            commit.AuthorEmail
        );

        Assert.Equal(40, commit.Hash.Length);
        Assert.Equal("main", result.Branch);
    }

    [Fact]
    public void Read_DefaultCount_ReturnsTenCommits()
    {
        for (var index = 1; index <= 12; index++)
        {
            CommitFile(
                "history.txt",
                $"Version {index}",
                $"Commit {index}"
            );
        }

        var result = _reader.Read(
            Project(),
            new GitLogRequest()
        );

        Assert.Equal(10, result.Commits.Count);
        Assert.Equal("Commit 12", result.Commits[0].Subject);
        Assert.Equal("Commit 3", result.Commits[^1].Subject);
    }

    [Fact]
    public void Read_CustomCount_LimitsHistory()
    {
        CommitFile("file.txt", "one", "First");
        CommitFile("file.txt", "two", "Second");
        CommitFile("file.txt", "three", "Third");

        var result = _reader.Read(
            Project(),
            new GitLogRequest(Count: 2)
        );

        Assert.Equal(2, result.Commits.Count);
        Assert.Equal("Third", result.Commits[0].Subject);
        Assert.Equal("Second", result.Commits[1].Subject);
    }

    [Fact]
    public void Read_PathFilter_ReturnsMatchingHistory()
    {
        CommitFile("first.txt", "first", "First file");
        CommitFile("second.txt", "second", "Second file");

        var result = _reader.Read(
            Project(),
            new GitLogRequest(Path: "first.txt")
        );

        var commit = Assert.Single(result.Commits);

        Assert.Equal("First file", commit.Subject);
    }

    [Fact]
    public void Read_DetachedHead_ReturnsNullBranch()
    {
        CommitFile("file.txt", "content", "Initial commit");

        Git("checkout", "--detach", "-q", "HEAD");

        var result = _reader.Read(
            Project(),
            new GitLogRequest()
        );

        Assert.Null(result.Branch);
        Assert.Single(result.Commits);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void Read_InvalidCount_Throws(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _reader.Read(
                Project(),
                new GitLogRequest(Count: count)
            )
        );
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\system.ini")]
    [InlineData("folder/../file.txt")]
    [InlineData("folder//file.txt")]
    [InlineData(":(glob)*.cs")]
    [InlineData("*.cs")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("..\\outside.txt")]
    [InlineData("folder\\..\\file.txt")]
    [InlineData("folder/./file.txt")]
    [InlineData("C:relative.txt")]
    [InlineData("\\Windows\\system.ini")]
    public void Read_InvalidPath_Throws(string path)
    {
        Assert.Throws<ArgumentException>(
            () => _reader.Read(
                Project(),
                new GitLogRequest(Path: path)
            )
        );
    }

    [Fact]
    public void Read_NonGitProject_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => _reader.Read(
                Project(isGitRepository: false),
                new GitLogRequest()
            )
        );
    }

    [Fact]
    public void Read_NullProject_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => _reader.Read(
                null!,
                new GitLogRequest()
            )
        );
    }

    [Fact]
    public void Read_NullRequest_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => _reader.Read(
                Project(),
                null!
            )
        );
    }

    [Fact]
    public void Read_WindowsStylePath_ReturnsMatchingHistory()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, "src")
        );

        CommitFile(
            "src/example.txt",
            "content",
            "Add example file"
        );

        var result = _reader.Read(
            Project(),
            new GitLogRequest(
                Path: "src\\example.txt"
            )
        );

        var commit = Assert.Single(result.Commits);

        Assert.Equal(
            "Add example file",
            commit.Subject
        );
    }

    private ProjectContext Project(bool isGitRepository = true)
    {
        return new ProjectContext(
            Name: Path.GetFileName(_root),
            RootPath: _root,
            IsGitRepository: isGitRepository
        );
    }

    private void CommitFile(
        string path,
        string content,
        string message
    )
    {
        File.WriteAllText(
            Path.Combine(_root, path),
            content
        );

        Git("add", "--", path);
        Git("commit", "-qm", message);
    }

    private void Git(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
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
