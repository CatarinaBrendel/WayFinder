using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitStatusParserTests
{
    private readonly GitStatusParser _parser = new();

    [Fact]
    public void Parse_EmptyOutput_ReturnsCleanStatus()
    {
        var result = Parse("");

        Assert.Empty(result.Staged);
        Assert.Empty(result.Unstaged);
        Assert.Empty(result.Untracked);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void Parse_StagedAddition_ReturnsStagedChange()
    {
        var result = Parse("A  added.cs\0");

        var change = Assert.Single(result.Staged);

        Assert.Equal("added.cs", change.Path);
        Assert.Equal(GitChangeKind.Added, change.Kind);
        Assert.Empty(result.Unstaged);
    }

    [Fact]
    public void Parse_UnstagedModification_ReturnsUnstagedChange()
    {
        var result = Parse(" M modified.cs\0");

        var change = Assert.Single(result.Unstaged);

        Assert.Equal("modified.cs", change.Path);
        Assert.Equal(GitChangeKind.Modified, change.Kind);
        Assert.Empty(result.Staged);
    }

    [Fact]
    public void Parse_StagedAndUnstagedChanges_ReturnsBoth()
    {
        var result = Parse("MM file.cs\0");

        Assert.Single(result.Staged);
        Assert.Single(result.Unstaged);
    }

    [Fact]
    public void Parse_UntrackedFile_PreservesSpaces()
    {
        var result = Parse("?? file with spaces.cs\0");

        Assert.Equal(
            "file with spaces.cs",
            Assert.Single(result.Untracked)
        );
    }

    [Fact]
    public void Parse_Rename_PreservesOriginalPath()
    {
        var result = Parse(
            "R  new-name.cs\0old-name.cs\0"
        );

        var change = Assert.Single(result.Staged);

        Assert.Equal(GitChangeKind.Renamed, change.Kind);
        Assert.Equal("new-name.cs", change.Path);
        Assert.Equal("old-name.cs", change.OriginalPath);
    }

    [Fact]
    public void Parse_Conflict_IsReportedSeparately()
    {
        var result = Parse("UU conflicted.cs\0");

        var change = Assert.Single(result.Conflicts);

        Assert.Equal(GitChangeKind.Unmerged, change.Kind);
        Assert.Empty(result.Staged);
        Assert.Empty(result.Unstaged);
    }

    [Fact]
    public void Parse_IgnoredFile_IsSkipped()
    {
        var result = Parse("!! ignored.cs\0");

        Assert.Empty(result.Staged);
        Assert.Empty(result.Unstaged);
        Assert.Empty(result.Untracked);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void Parse_Results_AreSortedOrdinally()
    {
        var result = Parse(
            "?? z.cs\0?? a.cs\0?? B.cs\0"
        );

        Assert.Equal(
            ["B.cs", "a.cs", "z.cs"],
            result.Untracked
        );
    }

    [Fact]
    public void Parse_InvalidRecord_Throws()
    {
        Assert.Throws<FormatException>(
            () => Parse("invalid\0")
        );
    }

    [Fact]
    public void Parse_RenameWithoutOriginalPath_Throws()
    {
        Assert.Throws<FormatException>(
            () => Parse("R  new-name.cs\0")
        );
    }

    private GitStatus Parse(string output)
    {
        return _parser.Parse(
            "TestRepository",
            "main",
            output
        );
    }
}
