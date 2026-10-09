using System.Diagnostics;
using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitCommandRunnerTests : IDisposable
{
    private readonly string _root;
    private readonly GitCommandRunner _runner = new();

    public GitCommandRunnerTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-git-tests-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);

        Git("init", "-q");
        Git("config", "user.name", "WayFinder Tests");
        Git("config", "user.email", "wayfinder@example.invalid");
        Git("symbolic-ref", "HEAD", "refs/heads/main");
    }

    [Fact]
    public void GetStatus_ForCleanRepository_ReturnsEmpty()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        var result = _runner.GetStatus(_root);

        Assert.Empty(result);
    }

    [Fact]
    public void GetStatus_ForUntrackedFile_PreservesSpaces()
    {
        File.WriteAllText(
            Path.Combine(_root, "file with spaces.txt"),
            "content"
        );

        var result = _runner.GetStatus(_root);

        Assert.Equal(
            "?? file with spaces.txt\0",
            result
        );
    }

    [Fact]
    public void GetStatus_ForStagedFile_ReturnsIndexStatus()
    {
        File.WriteAllText(
            Path.Combine(_root, "added.txt"),
            "content"
        );

        Git("add", "added.txt");

        var result = _runner.GetStatus(_root);

        Assert.Equal(
            "A  added.txt\0",
            result
        );
    }

    [Fact]
    public void GetBranch_ForAttachedHead_ReturnsBranchName()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        var result = _runner.GetBranch(_root);

        Assert.Equal("main", result);
    }

    [Fact]
    public void GetBranch_ForDetachedHead_ReturnsNull()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");
        Git("checkout", "--detach", "-q");

        var result = _runner.GetBranch(_root);

        Assert.Null(result);
    }

    [Fact]
    public void GetStatus_ForInvalidRepository_Throws()
    {
        var unrelatedDirectory = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-nonrepo-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(unrelatedDirectory);

        try
        {
            Assert.Throws<InvalidOperationException>(
                () => _runner.GetStatus(unrelatedDirectory)
            );
        }
        finally
        {
            Directory.Delete(unrelatedDirectory, recursive: true);
        }
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

        var output = outputTask.GetAwaiter().GetResult();
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
