using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Infrastructure.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Files;

public sealed class ProjectFileSystemTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly ProjectFileSystem _fileSystem = new();

    public ProjectFileSystemTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-tests-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);

        _project = new ProjectContext(
            Name: "TestProject",
            RootPath: _root,
            IsGitRepository: true
        );
    }

    [Fact]
    public void FileExists_ForFileInsideProject_ReturnsTrue()
    {
        var path = Path.Combine(_root, "hello.txt");
        File.WriteAllText(path, "hello");

        var exists = _fileSystem.FileExists(
            _project,
            "hello.txt"
        );

        Assert.True(exists);
    }

    [Fact]
    public void FileExists_ForParentTraversal_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(
            () => _fileSystem.FileExists(
                _project,
                "../outside.txt"
            )
        );
    }

    [Fact]
    public void FileExists_ForAbsolutePath_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(
            () => _fileSystem.FileExists(
                _project,
                "/etc/passwd"
            )
        );
    }

    [Fact]
    public void FindFiles_IgnoresBinAndObj()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, "src")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "bin")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "obj")
        );

        File.WriteAllText(
            Path.Combine(_root, "src", "Real.cs"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "bin", "Generated.cs"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "obj", "Generated.cs"),
            string.Empty
        );

        var files = _fileSystem.FindFiles(
            _project,
            "*.cs"
        );

        var file = Assert.Single(files);

        Assert.Equal(
            Path.Combine("src", "Real.cs"),
            file.RelativePath
        );
    }

    [Fact]
    public void FindFiles_DoesNotTraverseDirectorySymlink()
    {
        var outside = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-outside-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(outside);

        try
        {
            File.WriteAllText(
                Path.Combine(outside, "Secret.cs"),
                "secret"
            );

            var link = Path.Combine(_root, "external");

            Directory.CreateSymbolicLink(link, outside);

            var files = _fileSystem.FindFiles(
                _project,
                "*.cs"
            );

            Assert.Empty(files);
        }
        finally
        {
            if (Directory.Exists(outside))
            {
                Directory.Delete(outside, recursive: true);
            }
        }
    }

    [Fact]
    public void FileExists_ForSymlinkOutsideProject_Throws()
    {
        var outside = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-secret-{Guid.NewGuid():N}.txt"
        );

        File.WriteAllText(outside, "secret");

        try
        {
            var link = Path.Combine(_root, "secret.txt");

            File.CreateSymbolicLink(link, outside);

            Assert.Throws<UnauthorizedAccessException>(
                () => _fileSystem.FileExists(
                    _project,
                    "secret.txt"
                )
            );
        }
        finally
        {
            if (File.Exists(outside))
            {
                File.Delete(outside);
            }
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
