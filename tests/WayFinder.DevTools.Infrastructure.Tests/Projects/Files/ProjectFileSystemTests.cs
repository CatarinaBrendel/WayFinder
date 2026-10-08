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

    [Fact]
    public void FindFiles_ReturnsFilesInDeterministicOrder()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, "zeta")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "alpha")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "middle")
        );

        File.WriteAllText(
            Path.Combine(_root, "zeta", "Z.cs"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "alpha", "A.cs"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "middle", "M.cs"),
            string.Empty
        );

        var files = _fileSystem.FindFiles(
            _project,
            "*.cs"
        );

        Assert.Equal(
            [
                Path.Combine("alpha", "A.cs"),
            Path.Combine("middle", "M.cs"),
            Path.Combine("zeta", "Z.cs"),
        ],
            files.Select(file => file.RelativePath)
        );
    }

    [Fact]
    public void Read_TruncatesFileAtMaximumBytes()
    {
        File.WriteAllText(
            Path.Combine(
                _project.RootPath,
                "large.txt"
            ),
            "abcdefghij"
        );

        var result = _fileSystem.Read(
            _project,
            "large.txt",
            maxBytes: 4
        );

        Assert.Equal(
            "abcd"u8.ToArray(),
            result.Content
        );

        Assert.Equal(
            10,
            result.TotalBytes
        );

        Assert.True(
            result.Truncated
        );
    }

    [Fact]
    public void Read_ReturnsEntireFileWhenBelowLimit()
    {
        File.WriteAllText(
            Path.Combine(
                _project.RootPath,
                "small.txt"
            ),
            "hello"
        );

        var result = _fileSystem.Read(
            _project,
            "small.txt",
            maxBytes: 64
        );

        Assert.Equal(
            "hello"u8.ToArray(),
            result.Content
        );

        Assert.Equal(
            5,
            result.TotalBytes
        );

        Assert.False(
            result.Truncated
        );
    }

    [Fact]
    public void Read_RejectsZeroMaximumBytes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _fileSystem.Read(
                _project,
                "anything.txt",
                maxBytes: 0
            )
        );
    }

    [Fact]
    public void Read_RejectsNegativeMaximumBytes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _fileSystem.Read(
                _project,
                "anything.txt",
                maxBytes: -1
            )
        );
    }

    [Fact]
    public void Read_ForParentTraversal_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(
            () => _fileSystem.Read(
                _project,
                "../outside.txt",
                maxBytes: 64
            )
        );
    }

    [Fact]
    public void Read_ForAbsolutePath_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(
            () => _fileSystem.Read(
                _project,
                "/etc/passwd",
                maxBytes: 64
            )
        );
    }

    [Fact]
    public void Read_ForSymlinkOutsideProject_Throws()
    {
        var outside = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-secret-{Guid.NewGuid():N}.txt"
        );

        File.WriteAllText(
            outside,
            "secret"
        );

        try
        {
            var link = Path.Combine(
                _root,
                "secret.txt"
            );

            File.CreateSymbolicLink(
                link,
                outside
            );

            Assert.Throws<UnauthorizedAccessException>(
                () => _fileSystem.Read(
                    _project,
                    "secret.txt",
                    maxBytes: 64
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

    [Fact]
    public void GetFiles_ReturnsAllFiles()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, "src")
        );

        File.WriteAllText(
            Path.Combine(_root, "README.md"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "src", "Program.cs"),
            string.Empty
        );

        var files =
            _fileSystem.GetFiles(
                _project
            );

        Assert.Equal(
            [
                "README.md",
            Path.Combine("src", "Program.cs"),
        ],
            files.Select(
                file => file.RelativePath
            )
        );
    }

    [Fact]
    public void GetFiles_IgnoresIgnoredDirectories()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, "src")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "bin")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, ".git")
        );

        File.WriteAllText(
            Path.Combine(_root, "src", "Real.cs"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "bin", "Generated.dll"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, ".git", "config"),
            string.Empty
        );

        var files =
            _fileSystem.GetFiles(
                _project
            );

        var file =
            Assert.Single(files);

        Assert.Equal(
            Path.Combine("src", "Real.cs"),
            file.RelativePath
        );
    }

    [Fact]
    public void GetFiles_DoesNotTraverseDirectorySymlink()
    {
        var outside = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-outside-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(outside);

        try
        {
            File.WriteAllText(
                Path.Combine(outside, "Secret.txt"),
                "secret"
            );

            Directory.CreateSymbolicLink(
                Path.Combine(_root, "external"),
                outside
            );

            var files =
                _fileSystem.GetFiles(
                    _project
                );

            Assert.Empty(files);
        }
        finally
        {
            if (Directory.Exists(outside))
            {
                Directory.Delete(
                    outside,
                    recursive: true
                );
            }
        }
    }

    [Fact]
    public void Read_WithOffset_ReturnsBytesFromOffset()
    {
        File.WriteAllText(
            Path.Combine(
                _project.RootPath,
                "offset.txt"
            ),
            "abcdefghij"
        );

        var result = _fileSystem.Read(
            _project,
            "offset.txt",
            offset: 3,
            maxBytes: 4
        );

        Assert.Equal(
            "defg"u8.ToArray(),
            result.Content
        );

        Assert.Equal(
            10,
            result.TotalBytes
        );

        Assert.Equal(
            3,
            result.Offset
        );

        Assert.True(
            result.Truncated
        );
    }

    [Fact]
    public void Read_WithOffset_ReturnsFinalChunkWithoutTruncation()
    {
        File.WriteAllText(
            Path.Combine(
                _project.RootPath,
                "offset.txt"
            ),
            "abcdefghij"
        );

        var result = _fileSystem.Read(
            _project,
            "offset.txt",
            offset: 7,
            maxBytes: 4
        );

        Assert.Equal(
            "hij"u8.ToArray(),
            result.Content
        );

        Assert.Equal(
            10,
            result.TotalBytes
        );

        Assert.Equal(
            7,
            result.Offset
        );

        Assert.False(
            result.Truncated
        );
    }

    [Fact]
    public void Read_WithOffsetAtEndOfFile_ReturnsEmptyContent()
    {
        File.WriteAllText(
            Path.Combine(
                _project.RootPath,
                "offset.txt"
            ),
            "hello"
        );

        var result = _fileSystem.Read(
            _project,
            "offset.txt",
            offset: 5,
            maxBytes: 4
        );

        Assert.Empty(
            result.Content
        );

        Assert.Equal(
            5,
            result.TotalBytes
        );

        Assert.Equal(
            5,
            result.Offset
        );

        Assert.False(
            result.Truncated
        );
    }

    [Fact]
    public void Read_WithOffsetBeyondEndOfFile_ReturnsEmptyContent()
    {
        File.WriteAllText(
            Path.Combine(
                _project.RootPath,
                "offset.txt"
            ),
            "hello"
        );

        var result = _fileSystem.Read(
            _project,
            "offset.txt",
            offset: 10,
            maxBytes: 4
        );

        Assert.Empty(
            result.Content
        );

        Assert.Equal(
            5,
            result.TotalBytes
        );

        Assert.Equal(
            10,
            result.Offset
        );

        Assert.False(
            result.Truncated
        );
    }

    [Fact]
    public void Read_RejectsNegativeOffset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _fileSystem.Read(
                _project,
                "anything.txt",
                offset: -1,
                maxBytes: 64
            )
        );
    }

    [Fact]
    public void Read_WithoutOffset_UsesZeroOffset()
    {
        File.WriteAllText(
            Path.Combine(
                _project.RootPath,
                "hello.txt"
            ),
            "hello"
        );

        var result = _fileSystem.Read(
            _project,
            "hello.txt",
            maxBytes: 64
        );

        Assert.Equal(
            0,
            result.Offset
        );

        Assert.Equal(
            "hello"u8.ToArray(),
            result.Content
        );
    }

    [Fact]
    public void GetEntries_ReturnsRootEntries()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, "src")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "tests")
        );

        File.WriteAllText(
            Path.Combine(_root, "README.md"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "wayfinder.json"),
            string.Empty
        );

        var entries = _fileSystem.GetEntries(
            _project,
            ""
        );

        Assert.Collection(
            entries,
            entry =>
            {
                Assert.Equal("src", entry.Name);
                Assert.Equal("src", entry.Path);
                Assert.True(entry.IsDirectory);
            },
            entry =>
            {
                Assert.Equal("tests", entry.Name);
                Assert.Equal("tests", entry.Path);
                Assert.True(entry.IsDirectory);
            },
            entry =>
            {
                Assert.Equal("README.md", entry.Name);
                Assert.Equal("README.md", entry.Path);
                Assert.False(entry.IsDirectory);
            },
            entry =>
            {
                Assert.Equal("wayfinder.json", entry.Name);
                Assert.Equal("wayfinder.json", entry.Path);
                Assert.False(entry.IsDirectory);
            }
        );
    }

    [Fact]
    public void GetEntries_ReturnsOnlyImmediateChildren()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, "src", "Application")
        );

        File.WriteAllText(
            Path.Combine(_root, "src", "Application", "Foo.cs"),
            string.Empty
        );

        File.WriteAllText(
            Path.Combine(_root, "src", "Program.cs"),
            string.Empty
        );

        var entries = _fileSystem.GetEntries(
            _project,
            "src"
        );

        Assert.Collection(
            entries,
            entry =>
            {
                Assert.Equal("Application", entry.Name);
                Assert.Equal(
                    Path.Combine("src", "Application"),
                    entry.Path
                );
                Assert.True(entry.IsDirectory);
            },
            entry =>
            {
                Assert.Equal("Program.cs", entry.Name);
                Assert.Equal(
                    Path.Combine("src", "Program.cs"),
                    entry.Path
                );
                Assert.False(entry.IsDirectory);
            }
        );
    }

    [Fact]
    public void GetEntries_ExcludesIgnoredDirectories()
    {
        Directory.CreateDirectory(
            Path.Combine(_root, ".git")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "bin")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "obj")
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "src")
        );

        var entries = _fileSystem.GetEntries(
            _project,
            ""
        );

        var entry = Assert.Single(entries);

        Assert.Equal("src", entry.Name);
        Assert.True(entry.IsDirectory);
    }

    [Fact]
    public void GetEntries_ReturnsDirectoriesBeforeFilesAndSortsOrdinally()
    {
        File.WriteAllText(
            Path.Combine(_root, "z.txt"),
            string.Empty
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "Zoo")
        );

        File.WriteAllText(
            Path.Combine(_root, "a.txt"),
            string.Empty
        );

        Directory.CreateDirectory(
            Path.Combine(_root, "Alpha")
        );

        var entries = _fileSystem.GetEntries(
            _project,
            ""
        );

        Assert.Equal(
            ["Alpha", "Zoo", "a.txt", "z.txt"],
            entries.Select(entry => entry.Name)
        );
    }

    [Fact]
    public void GetEntries_ForParentTraversal_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(
            () => _fileSystem.GetEntries(
                _project,
                "../outside"
            )
        );
    }

    [Fact]
    public void GetEntries_ForAbsolutePath_Throws()
    {
        var absolutePath = Path.GetFullPath(
            Path.Combine(_root, "..")
        );

        Assert.Throws<UnauthorizedAccessException>(
            () => _fileSystem.GetEntries(
                _project,
                absolutePath
            )
        );
    }

    [Fact]
    public void GetEntries_ForNonexistentDirectory_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => _fileSystem.GetEntries(
                _project,
                "does-not-exist"
            )
        );
    }

    [Fact]
    public void GetEntries_DoesNotExposeDirectorySymlink()
    {
        var realDirectory = Path.Combine(
            _root,
            "real"
        );

        Directory.CreateDirectory(realDirectory);

        Directory.CreateSymbolicLink(
            Path.Combine(_root, "linked"),
            realDirectory
        );

        var entries = _fileSystem.GetEntries(
            _project,
            ""
        );

        Assert.Contains(
            entries,
            entry => entry.Name == "real"
        );

        Assert.DoesNotContain(
            entries,
            entry => entry.Name == "linked"
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
