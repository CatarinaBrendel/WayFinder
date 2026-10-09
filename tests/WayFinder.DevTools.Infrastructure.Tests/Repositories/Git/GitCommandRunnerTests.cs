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

    [Fact]
    public void GetStatus_IgnoresInheritedGitConfigEnvironment()
    {
        File.WriteAllText(
            Path.Combine(_root, "untracked.txt"),
            "content"
        );

        var environment = new Dictionary<string, string>
        {
            ["GIT_CONFIG_COUNT"] = "1",
            ["GIT_CONFIG_KEY_0"] = "status.showUntrackedFiles",
            ["GIT_CONFIG_VALUE_0"] = "no"
        };

        var runner = new GitCommandRunner(environment);

        var result = runner.GetStatus(_root);

        Assert.Equal("?? untracked.txt\0", result);
    }

    [Fact]
    public void GetBranch_IgnoresInvalidInheritedGitConfiguration()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        var environment = new Dictionary<string, string>
        {
            ["GIT_CONFIG_COUNT"] = "1",
            ["GIT_CONFIG_KEY_0"] = "invalid configuration key",
            ["GIT_CONFIG_VALUE_0"] = "value"
        };

        var runner = new GitCommandRunner(environment);

        var result = runner.GetBranch(_root);

        Assert.Equal("main", result);
    }

    [Fact]
    public void GetBranch_IgnoresInjectedGlobalGitConfiguration()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        var configPath = Path.Combine(
            _root,
            "malicious-global.gitconfig"
        );

        File.WriteAllText(
            configPath,
            "[invalid section]\nvalue = true\n"
        );

        var environment = new Dictionary<string, string>
        {
            ["GIT_CONFIG_GLOBAL"] = configPath
        };

        var runner = new GitCommandRunner(environment);

        var result = runner.GetBranch(_root);

        Assert.Equal("main", result);
    }

    [Fact]
    public void GetBranch_IgnoresInjectedSystemGitConfiguration()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        var configPath = Path.Combine(
            _root,
            "malicious-system.gitconfig"
        );

        File.WriteAllText(
            configPath,
            "[invalid section]\nvalue = true\n"
        );

        var environment = new Dictionary<string, string>
        {
            ["GIT_CONFIG_SYSTEM"] = configPath
        };

        var runner = new GitCommandRunner(environment);

        var result = runner.GetBranch(_root);

        Assert.Equal("main", result);
    }

    [Fact]
    public void GetBranch_IgnoresInheritedGitDir()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        var otherRoot = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-other-repo-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(otherRoot);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = otherRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            startInfo.ArgumentList.Add("init");
            startInfo.ArgumentList.Add("-q");
            startInfo.ArgumentList.Add("-b");
            startInfo.ArgumentList.Add("other-branch");

            using (var process = Process.Start(startInfo)!)
            {
                process.WaitForExit();

                Assert.Equal(0, process.ExitCode);
            }

            var environment = new Dictionary<string, string>
            {
                ["GIT_DIR"] = Path.Combine(otherRoot, ".git")
            };

            var runner = new GitCommandRunner(environment);

            var result = runner.GetBranch(_root);

            Assert.Equal("main", result);
        }
        finally
        {
            Directory.Delete(otherRoot, recursive: true);
        }
    }

    [Fact]
    public void GetStatus_IgnoresInheritedGitWorkTree()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        File.WriteAllText(
            Path.Combine(_root, "registered-file.txt"),
            "registered content"
        );

        var otherRoot = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-other-worktree-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(otherRoot);

        try
        {
            File.WriteAllText(
                Path.Combine(otherRoot, "external-file.txt"),
                "external content"
            );

            var environment = new Dictionary<string, string>
            {
                ["GIT_WORK_TREE"] = otherRoot
            };

            var runner = new GitCommandRunner(environment);

            var result = runner.GetStatus(_root);

            Assert.Contains(
                "registered-file.txt",
                result
            );

            Assert.DoesNotContain(
                "external-file.txt",
                result
            );
        }
        finally
        {
            Directory.Delete(otherRoot, recursive: true);
        }
    }

    [Fact]
    public void GetStatus_IgnoresInheritedGitIndexFile()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        File.WriteAllText(
            Path.Combine(_root, "tracked.txt"),
            "original content"
        );

        Git("add", "tracked.txt");
        Git("commit", "-qm", "Add tracked file");

        // Store the alternative index inside Git's metadata directory,
        // where it will not appear as an untracked working-tree file.
        var alternateIndex = Path.Combine(
            _root,
            ".git",
            "alternate-index"
        );

        // Copy the real index so the alternative starts from the same state.
        File.Copy(
            Path.Combine(_root, ".git", "index"),
            alternateIndex
        );

        // Modify the working file.
        File.WriteAllText(
            Path.Combine(_root, "tracked.txt"),
            "modified content"
        );

        // Stage the modification exclusively in the alternative index.
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.Environment["GIT_INDEX_FILE"] = alternateIndex;

        startInfo.ArgumentList.Add("add");
        startInfo.ArgumentList.Add("tracked.txt");

        using (var process = Process.Start(startInfo)!)
        {
            process.WaitForExit();

            Assert.Equal(0, process.ExitCode);
        }

        // Restore the working file without modifying the real index.
        File.WriteAllText(
            Path.Combine(_root, "tracked.txt"),
            "original content"
        );

        var environment = new Dictionary<string, string>
        {
            ["GIT_INDEX_FILE"] = alternateIndex
        };

        var runner = new GitCommandRunner(environment);

        var result = runner.GetStatus(_root);

        Assert.Empty(result);
    }

    [Fact]
    public void HasCommits_IgnoresInheritedGitCommonDir()
    {
        // Ensure the registered repository contains a commit.
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        var otherRoot = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-other-repo-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(otherRoot);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = otherRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            startInfo.ArgumentList.Add("init");
            startInfo.ArgumentList.Add("-q");

            using (var process = Process.Start(startInfo)!)
            {
                process.WaitForExit();
                Assert.Equal(0, process.ExitCode);
            }

            // The alternative repository deliberately has no commits.
            var environment = new Dictionary<string, string>
            {
                ["GIT_COMMON_DIR"] = Path.Combine(otherRoot, ".git")
            };

            var runner = new GitCommandRunner(environment);

            Assert.True(runner.HasCommits(_root));
        }
        finally
        {
            Directory.Delete(otherRoot, recursive: true);
        }
    }

    [Fact]
    public void HasCommits_IgnoresInheritedGitObjectDirectory()
    {
        // Ensure the registered repository contains a commit.
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        var otherObjects = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-other-objects-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(otherObjects);

        try
        {
            var environment = new Dictionary<string, string>
            {
                ["GIT_OBJECT_DIRECTORY"] = otherObjects
            };

            var runner = new GitCommandRunner(environment);

            Assert.True(runner.HasCommits(_root));
        }
        finally
        {
            Directory.Delete(otherObjects, recursive: true);
        }
    }

    [Fact]
    public void HasCommits_DoesNotResolveObjectsFromInheritedAlternateDirectory()
    {
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        var commitId = GetHeadCommitId();

        var objectDirectory = Path.Combine(_root, ".git", "objects");

        var objectPath = Path.Combine(
            objectDirectory,
            commitId[..2],
            commitId[2..]
        );

        var alternateDirectory = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-alternate-objects-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(alternateDirectory);

        try
        {
            // Copy the commit object into an external object database.
            var alternateObjectPath = Path.Combine(
                alternateDirectory,
                commitId[..2],
                commitId[2..]
            );

            Directory.CreateDirectory(
                Path.GetDirectoryName(alternateObjectPath)!
            );

            File.Copy(objectPath, alternateObjectPath);

            // Remove the original object from the registered repository.
            File.Delete(objectPath);

            var environment = new Dictionary<string, string>
            {
                ["GIT_ALTERNATE_OBJECT_DIRECTORIES"] = alternateDirectory
            };

            var runner = new GitCommandRunner(environment);

            // The registered repository must not depend on an inherited
            // external object database.
            Assert.False(runner.HasCommits(_root));
        }
        finally
        {
            Directory.Delete(alternateDirectory, recursive: true);
        }
    }

    [Fact]
    public void GetBranch_IgnoresInheritedGitConfigParameters()
    {
        var environment = new Dictionary<string, string>
        {
            ["GIT_CONFIG_PARAMETERS"] =
                "'invalid configuration key=value'"
        };

        var runner = new GitCommandRunner(environment);

        var branch = runner.GetBranch(_root);

        Assert.Equal("main", branch);
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

    private string GetHeadCommitId()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("rev-parse");
        startInfo.ArgumentList.Add("HEAD");

        using var process = Process.Start(startInfo)!;

        var output = process.StandardOutput.ReadToEnd();

        process.WaitForExit();

        Assert.Equal(0, process.ExitCode);

        return output.Trim();
    }
}
