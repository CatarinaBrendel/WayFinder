using System.Text;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Reading;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.Reading;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Reading;

public sealed class RepositoryFileReaderTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly RepositoryFileReader _reader;

    public RepositoryFileReaderTests()
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

        _reader = new RepositoryFileReader(
            new ProjectFileSystem()
        );
    }

    [Fact]
    public void Read_ReturnsTextFile()
    {
        File.WriteAllText(
            Path.Combine(_root, "hello.txt"),
            "Hello, WayFinder!",
            Encoding.UTF8
        );

        var result = _reader.Read(
            _project,
            "hello.txt"
        );

        Assert.Equal(
            "Hello, WayFinder!",
            result.Content
        );

        Assert.False(result.Truncated);
    }

    [Fact]
    public void Read_TruncatesLargeTextFile()
    {
        var content = new string(
            'a',
            70 * 1024
        );

        File.WriteAllText(
            Path.Combine(_root, "large.txt"),
            content,
            new UTF8Encoding(false)
        );

        var result = _reader.Read(
            _project,
            "large.txt"
        );

        Assert.Equal(
            64 * 1024,
            result.Content.Length
        );

        Assert.Equal(
            70 * 1024,
            result.TotalBytes
        );

        Assert.True(result.Truncated);
    }

    [Fact]
    public void Read_TruncatedUtf8Character_DiscardsIncompleteCharacter()
    {
        const int maximumBytes = 64 * 1024;

        var prefix = new string(
            'a',
            maximumBytes - 2
        );

        var content =
            prefix + "€" + "after";

        File.WriteAllText(
            Path.Combine(_root, "unicode.txt"),
            content,
            new UTF8Encoding(false)
        );

        var result = _reader.Read(
            _project,
            "unicode.txt"
        );

        Assert.Equal(
            prefix,
            result.Content
        );

        Assert.True(result.Truncated);
    }

    [Fact]
    public void Read_InvalidUtf8_ThrowsBinaryFileNotSupportedException()
    {
        File.WriteAllBytes(
            Path.Combine(_root, "invalid.txt"),
            [0x61, 0x62, 0xFF, 0x63]
        );

        Assert.Throws<BinaryFileNotSupportedException>(
            () => _reader.Read(
                _project,
                "invalid.txt"
            )
        );
    }

    [Fact]
    public void Read_NullByte_ThrowsBinaryFileNotSupportedException()
    {
        File.WriteAllBytes(
            Path.Combine(_root, "binary.dat"),
            [0x61, 0x62, 0x00, 0x63]
        );

        Assert.Throws<BinaryFileNotSupportedException>(
            () => _reader.Read(
                _project,
                "binary.dat"
            )
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(
                _root,
                recursive: true
            );
        }
    }
}
