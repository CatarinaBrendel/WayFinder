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

    [Fact]
    public void ReadRange_ReturnsRequestedLines()
    {
        File.WriteAllText(
            Path.Combine(_root, "lines.txt"),
            "one\ntwo\nthree\nfour\nfive",
            new UTF8Encoding(false)
        );

        var result = _reader.Read(
            _project,
            "lines.txt",
            startLine: 2,
            lineCount: 2
        );

        Assert.Equal(
            "two\nthree\n",
            result.Content
        );

        Assert.Equal(2, result.StartLine);
        Assert.Equal(3, result.EndLine);
        Assert.Equal(23, result.TotalBytes);
    }

    [Fact]
    public void ReadRange_ReturnsRemainingLinesAtEndOfFile()
    {
        File.WriteAllText(
            Path.Combine(_root, "lines.txt"),
            "one\ntwo\nthree",
            new UTF8Encoding(false)
        );

        var result = _reader.Read(
            _project,
            "lines.txt",
            startLine: 2,
            lineCount: 10
        );

        Assert.Equal(
            "two\nthree",
            result.Content
        );

        Assert.Equal(2, result.StartLine);
        Assert.Equal(3, result.EndLine);
    }

    [Fact]
    public void ReadRange_BeyondEndOfFile_ReturnsEmptyContent()
    {
        File.WriteAllText(
            Path.Combine(_root, "lines.txt"),
            "one\ntwo\nthree",
            new UTF8Encoding(false)
        );

        var result = _reader.Read(
            _project,
            "lines.txt",
            startLine: 10,
            lineCount: 2
        );

        Assert.Equal(
            string.Empty,
            result.Content
        );

        Assert.Equal(10, result.StartLine);
        Assert.Null(result.EndLine);
    }

    [Fact]
    public void ReadRange_IncludesEmptyLines()
    {
        File.WriteAllText(
            Path.Combine(_root, "lines.txt"),
            "one\n\ntwo\nthree",
            new UTF8Encoding(false)
        );

        var result = _reader.Read(
            _project,
            "lines.txt",
            startLine: 2,
            lineCount: 2
        );

        Assert.Equal(
            "\ntwo\n",
            result.Content
        );

        Assert.Equal(2, result.StartLine);
        Assert.Equal(3, result.EndLine);
    }

    [Fact]
    public void ReadRange_PreservesCrLf()
    {
        File.WriteAllText(
            Path.Combine(_root, "lines.txt"),
            "one\r\ntwo\r\nthree\r\nfour",
            new UTF8Encoding(false)
        );

        var result = _reader.Read(
            _project,
            "lines.txt",
            startLine: 2,
            lineCount: 2
        );

        Assert.Equal(
            "two\r\nthree\r\n",
            result.Content
        );

        Assert.Equal(2, result.StartLine);
        Assert.Equal(3, result.EndLine);
    }

    [Fact]
    public void ReadRange_FinalLineWithoutNewlineCountsAsLine()
    {
        File.WriteAllText(
            Path.Combine(_root, "lines.txt"),
            "one\ntwo\nthree",
            new UTF8Encoding(false)
        );

        var result = _reader.Read(
            _project,
            "lines.txt",
            startLine: 3,
            lineCount: 1
        );

        Assert.Equal(
            "three",
            result.Content
        );

        Assert.Equal(3, result.EndLine);
    }

    [Fact]
    public void ReadRange_RejectsZeroStartLine()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _reader.Read(
                _project,
                "anything.txt",
                startLine: 0,
                lineCount: 1
            )
        );
    }

    [Fact]
    public void ReadRange_RejectsZeroLineCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _reader.Read(
                _project,
                "anything.txt",
                startLine: 1,
                lineCount: 0
            )
        );
    }

    [Fact]
    public void ReadRange_Utf8CharacterAcrossBufferBoundary_IsPreserved()
    {
        const int bufferBytes = 16 * 1024;

        var prefix = new string(
            'a',
            bufferBytes - 1
        );

        var content =
            prefix + "€\nsecond";

        File.WriteAllText(
            Path.Combine(_root, "unicode.txt"),
            content,
            new UTF8Encoding(false)
        );

        var result = _reader.Read(
            _project,
            "unicode.txt",
            startLine: 1,
            lineCount: 1
        );

        Assert.Equal(
            prefix + "€\n",
            result.Content
        );

        Assert.Equal(1, result.EndLine);
    }

    [Fact]
    public void ReadRange_NullByteInLaterBuffer_ThrowsBinaryFileNotSupportedException()
    {
        const int bufferBytes = 16 * 1024;

        var content =
            Enumerable
                .Repeat((byte)'a', bufferBytes)
                .Concat([(byte)0])
                .Concat([(byte)'\n'])
                .ToArray();

        File.WriteAllBytes(
            Path.Combine(_root, "binary.dat"),
            content
        );

        Assert.Throws<BinaryFileNotSupportedException>(
            () => _reader.Read(
                _project,
                "binary.dat",
                startLine: 1,
                lineCount: 2
            )
        );
    }

    [Fact]
    public void ReadRange_RejectsLineCountAboveMaximum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _reader.Read(
                _project,
                "anything.txt",
                startLine: 1,
                lineCount: 501
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
