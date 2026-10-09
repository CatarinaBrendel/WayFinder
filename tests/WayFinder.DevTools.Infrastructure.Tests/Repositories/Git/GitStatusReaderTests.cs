using System.Diagnostics;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitStatusReaderTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly GitStatusReader _reader = new();

    public GitStatusReaderTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-git-status-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);

        Git("init", "-q");
        Git("config", "user.name", "WayFinder Tests");
        Git("config", "user.email", "wayfinder@example.invalid");
        Git("symbolic-ref", "HEAD", "refs/heads/main");
        Git("commit", "--allow-empty", "-qm", "Initial commit");

        _project = new ProjectContext(
            Name: "TestRepository",
            RootPath: _root,
            IsGitRepository: true
        );
    }

    [Fact]
    public void Read_CleanRepository_ReturnsEmptyCollections()
    {
        var result = _reader.Read(_project);

        Assert.Equal("TestRepository", result.RepositoryName);
        Assert.Equal("main", result.Branch);

        Assert.Empty(result.Staged);
        Assert.Empty(result.Unstaged);
        Assert.Empty(result.Untracked);
        Assert.Empty(result.Conflicts);
    }

   [Fact]
    public void Read_RepositoryWithChanges_ClassifiesThemCorrectly()
    {
        File.WriteAllText(
            Path.Combine(_root, "modified.txt"),
            "original"
        );

        Git("add", "modified.txt");
        Git("commit", "-qm", "Add tracked file");

        File.WriteAllText(
            Path.Combine(_root, "staged.txt"),
            "staged"
        );

        Git("add", "staged.txt");

        File.WriteAllText(
            Path.Combine(_root, "modified.txt"),
            "changed"
        );

        File.WriteAllText(
            Path.Combine(_root, "untracked file.txt"),
            "untracked"
        );

        var result = _reader.Read(_project);

        Assert.Contains(
            result.Staged,
            change =>
                change.Path == "staged.txt"
                && change.Kind == GitChangeKind.Added
        );

        Assert.Contains(
            result.Unstaged,
            change =>
                change.Path == "modified.txt"
                && change.Kind == GitChangeKind.Modified
        );

        Assert.Contains(
            result.Untracked,
            path => path == "untracked file.txt"
        );

        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void Read_NonGitProject_Throws()
    {
        var project = _project with
        {
            IsGitRepository = false
        };

        Assert.Throws<InvalidOperationException>(
            () => _reader.Read(project)
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
