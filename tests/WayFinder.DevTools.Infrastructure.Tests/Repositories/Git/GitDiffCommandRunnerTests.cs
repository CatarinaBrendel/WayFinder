using System.Diagnostics;
using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitDiffCommandRunnerTests : IDisposable
{
    private readonly string _root;
    private readonly GitCommandRunner _runner = new();

    public GitDiffCommandRunnerTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-diff-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);

        Git("init", "-q");
        Git("config", "user.name", "WayFinder Tests");
        Git("config", "user.email", "wayfinder@example.invalid");

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
    }

    [Fact]
    public void GetDiff_UnstagedModification_ReturnsPatch()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "modified\n"
        );

        var result = ReadDiff();

        Assert.False(result.Truncated);
        Assert.Contains("-original", result.Content);
        Assert.Contains("+modified", result.Content);
    }

    [Fact]
    public void GetDiff_StagedModification_ReturnsPatch()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "staged\n"
        );

        Git("add", "example.txt");

        var unstaged = ReadDiff();
        var staged = ReadDiff(staged: true);

        Assert.Empty(unstaged.Content);
        Assert.Contains("+staged", staged.Content);
        Assert.False(staged.Truncated);
    }

    [Fact]
    public void GetDiff_StatOnly_ReturnsSummary()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "modified\n"
        );

        var result = ReadDiff(statOnly: true);

        Assert.Contains("example.txt", result.Content);
        Assert.Contains("1 file changed", result.Content);
        Assert.DoesNotContain("diff --git", result.Content);
    }

    [Fact]
    public void GetDiff_PathFilter_ExcludesOtherFiles()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "modified\n"
        );

        File.WriteAllText(
            Path.Combine(_root, "other.txt"),
            "also modified\n"
        );

        var result = ReadDiff(path: "example.txt");

        Assert.Contains("example.txt", result.Content);
        Assert.DoesNotContain("other.txt", result.Content);
    }

    [Fact]
    public void GetDiff_OutputExceedsLimit_ReportsTruncation()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            string.Join(
                '\n',
                Enumerable.Range(0, 1000)
                    .Select(index => $"line {index}")
            )
        );

        var result = ReadDiff(maxOutputBytes: 128);

        Assert.True(result.Truncated);
        Assert.True(
            System.Text.Encoding.UTF8.GetByteCount(result.Content)
            <= 128
        );
    }

    [Fact]
    public void GetDiff_UnicodeContent_PreservesUtf8()
    {
        File.WriteAllText(
            Path.Combine(_root, "example.txt"),
            "Änderung: café 🚀\n"
        );

        var result = ReadDiff();

        Assert.False(result.Truncated);
        Assert.Contains("Änderung: café 🚀", result.Content);
    }

    private GitDiffCommandResult ReadDiff(
        bool staged = false,
        bool statOnly = false,
        string? path = null,
        int maxOutputBytes = 65_536
    )
    {
        return _runner.GetDiff(
            _root,
            staged,
            statOnly,
            path,
            maxOutputBytes
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
