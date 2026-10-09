using System.Diagnostics;
using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitLogCommandRunnerTests : IDisposable
{
    private readonly string _root;
    private readonly GitCommandRunner _runner = new();

    public GitLogCommandRunnerTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-git-log-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);

        Git("init", "-q");
        Git("config", "user.name", "WayFinder Tests");
        Git("config", "user.email", "wayfinder@example.invalid");
        Git("symbolic-ref", "HEAD", "refs/heads/main");
    }

    [Fact]
    public void HasCommits_EmptyRepository_ReturnsFalse()
    {
        Assert.False(_runner.HasCommits(_root));
    }

    [Fact]
    public void GetLog_EmptyRepository_ReturnsEmpty()
    {
        var result = _runner.GetLog(
            _root,
            count: 10,
            path: null
        );

        Assert.Empty(result);
    }

    [Fact]
    public void GetLog_SingleCommit_ReturnsMetadata()
    {
        CommitFile(
            "example.txt",
            "content",
            "Initial commit"
        );

        var result = _runner.GetLog(
            _root,
            count: 10,
            path: null
        );

        Assert.True(_runner.HasCommits(_root));

        Assert.Contains("WayFinder Tests", result);
        Assert.Contains("wayfinder@example.invalid", result);
        Assert.Contains("Initial commit", result);
        Assert.Contains('\0', result);
    }

    [Fact]
    public void GetLog_CountLimitsReturnedCommits()
    {
        CommitFile("first.txt", "first", "First commit");
        CommitFile("second.txt", "second", "Second commit");
        CommitFile("third.txt", "third", "Third commit");

        var result = _runner.GetLog(
            _root,
            count: 2,
            path: null
        );

        Assert.Contains("Third commit", result);
        Assert.Contains("Second commit", result);
        Assert.DoesNotContain("First commit", result);
    }

    [Fact]
    public void GetLog_PathFilter_ReturnsMatchingHistory()
    {
        CommitFile("first.txt", "first", "First file");
        CommitFile("second.txt", "second", "Second file");

        var result = _runner.GetLog(
            _root,
            count: 10,
            path: "first.txt"
        );

        Assert.Contains("First file", result);
        Assert.DoesNotContain("Second file", result);
    }

    [Fact]
    public void GetLog_UnicodeMetadata_IsPreserved()
    {
        Git("config", "user.name", "Catarina Müller");

        CommitFile(
            "unicode.txt",
            "content",
            "Änderung für café 🚀"
        );

        var result = _runner.GetLog(
            _root,
            count: 10,
            path: null
        );

        Assert.Contains("Catarina Müller", result);
        Assert.Contains("Änderung für café 🚀", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void GetLog_InvalidCount_Throws(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _runner.GetLog(
                _root,
                count,
                path: null
            )
        );
    }

    [Fact]
    public void GetLog_InvalidRepository_Throws()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-not-a-repository-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(directory);

        try
        {
            Assert.Throws<InvalidOperationException>(
                () => _runner.GetLog(
                    directory,
                    count: 10,
                    path: null
                )
            );
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void GetLog_OutputExceedsLimit_Throws()
    {
        var largeSubject = new string('X', 70_000);

        CommitFile(
            "large.txt",
            "content",
            largeSubject
        );

        var exception = Assert.Throws<InvalidOperationException>(
            () => _runner.GetLog(
                _root,
                count: 10,
                path: null
            )
        );

        Assert.Contains(
            "output exceeds",
            exception.Message,
            StringComparison.OrdinalIgnoreCase
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
