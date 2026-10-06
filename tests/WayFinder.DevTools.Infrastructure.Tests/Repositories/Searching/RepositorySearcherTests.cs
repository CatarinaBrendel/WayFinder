using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Repositories.Searching;
using WayFinder.DevTools.Infrastructure.Projects.Files;
using WayFinder.DevTools.Infrastructure.Repositories.Searching;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Searching;

public sealed class RepositorySearcherTests : IDisposable
{
    private readonly string _root;
    private readonly ProjectContext _project;
    private readonly IRepositorySearcher _searcher;

    public RepositorySearcherTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-search-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);

        _project = new ProjectContext(
            Name: "TestProject",
            RootPath: _root,
            IsGitRepository: true
        );

        var fileSystem =
            new ProjectFileSystem();

        _searcher =
            new RepositorySearcher(fileSystem);
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

    [Fact]
    public void Search_StopsAtFiftyMatches()
    {
        for (var index = 0; index < 60; index++)
        {
            File.WriteAllText(
                Path.Combine(
                    _root,
                    $"File{index:D2}.txt"
                ),
                "needle"
            );
        }

        var result =
            _searcher.Search(
                _project,
                "needle"
            );

        Assert.Equal(
            50,
            result.Matches.Count
        );

        Assert.True(result.Truncated);
    }

    [Fact]
    public void Search_ReturnsMatchingLine()
    {
        File.WriteAllText(
            Path.Combine(_root, "Example.cs"),
            """
        first line
        RepositorySearcher searcher
        third line
        """
        );

        var result =
            _searcher.Search(
                _project,
                "RepositorySearcher"
            );

        var match =
            Assert.Single(result.Matches);

        Assert.Equal(
            "Example.cs",
            match.Path
        );

        Assert.Equal(
            2,
            match.LineNumber
        );

        Assert.Equal(
            "RepositorySearcher searcher",
            match.Line
        );

        Assert.False(result.Truncated);
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        File.WriteAllText(
            Path.Combine(_root, "Example.txt"),
            "Hello WayFinder"
        );

        var result =
            _searcher.Search(
                _project,
                "wayfinder"
            );

        Assert.Single(result.Matches);
    }

    [Fact]
    public void Search_ReturnsEmptyResultWhenNothingMatches()
    {
        File.WriteAllText(
            Path.Combine(_root, "Example.txt"),
            "nothing interesting"
        );

        var result =
            _searcher.Search(
                _project,
                "needle"
            );

        Assert.Empty(result.Matches);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void Search_ReturnsMatchesInDeterministicOrder()
    {
        File.WriteAllText(
            Path.Combine(_root, "B.txt"),
            """
        nothing
        needle B
        """
        );

        File.WriteAllText(
            Path.Combine(_root, "A.txt"),
            """
        needle A1
        nothing
        needle A3
        """
        );

        var result =
            _searcher.Search(
                _project,
                "needle"
            );

        Assert.Collection(
            result.Matches,
            match =>
            {
                Assert.Equal("A.txt", match.Path);
                Assert.Equal(1, match.LineNumber);
            },
            match =>
            {
                Assert.Equal("A.txt", match.Path);
                Assert.Equal(3, match.LineNumber);
            },
            match =>
            {
                Assert.Equal("B.txt", match.Path);
                Assert.Equal(2, match.LineNumber);
            }
        );
    }

    [Fact]
    public void Search_SkipsFilesContainingNullBytes()
    {
        File.WriteAllBytes(
            Path.Combine(_root, "Binary.bin"),
            [
                (byte)'n',
            (byte)'e',
            (byte)'e',
            (byte)'d',
            (byte)'l',
            (byte)'e',
            0,
            ]
        );

        var result =
            _searcher.Search(
                _project,
                "needle"
            );

        Assert.Empty(result.Matches);
    }

    [Fact]
    public void Search_SkipsInvalidUtf8()
    {
        File.WriteAllBytes(
            Path.Combine(_root, "Invalid.txt"),
            [
                0xFF,
            0xFE,
            (byte)'n',
            (byte)'e',
            (byte)'e',
            (byte)'d',
            (byte)'l',
            (byte)'e',
            ]
        );

        var result =
            _searcher.Search(
                _project,
                "needle"
            );

        Assert.Empty(result.Matches);
    }

    [Fact]
    public void Search_SkipsFilesLargerThanMaximumSize()
    {
        var content =
            "needle"
            + new string(
                'x',
                1024 * 1024
            );

        File.WriteAllText(
            Path.Combine(_root, "Large.txt"),
            content
        );

        var result =
            _searcher.Search(
                _project,
                "needle"
            );

        Assert.Empty(result.Matches);
    }

    [Fact]
    public void Search_TruncatesLongMatchingLines()
    {
        var line =
            "needle"
            + new string('x', 500);

        File.WriteAllText(
            Path.Combine(_root, "Long.txt"),
            line
        );

        var result =
            _searcher.Search(
                _project,
                "needle"
            );

        var match =
            Assert.Single(result.Matches);

        Assert.Equal(
            300,
            match.Line.Length
        );

        Assert.StartsWith(
            "needle",
            match.Line
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Search_RejectsEmptyQuery(
    string query
)
    {
        Assert.Throws<ArgumentException>(
            () => _searcher.Search(
                _project,
                query
            )
        );
    }
}
