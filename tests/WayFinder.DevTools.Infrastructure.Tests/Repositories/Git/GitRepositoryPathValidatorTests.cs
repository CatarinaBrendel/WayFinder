using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitRepositoryPathValidatorTests
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "wayfinder-path-validation"
    );

    [Fact]
    public void Validate_NullPath_ReturnsNull()
    {
        var result = GitRepositoryPathValidator.Validate(
            _root,
            null
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData("README.md", "README.md")]
    [InlineData("src/Program.cs", "src/Program.cs")]
    [InlineData("src\\Program.cs", "src/Program.cs")]
    [InlineData("src/nested/File.cs", "src/nested/File.cs")]
    public void Validate_ValidPath_ReturnsNormalizedPath(
        string path,
        string expected
    )
    {
        var result = GitRepositoryPathValidator.Validate(
            _root,
            path
        );

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("../outside.txt")]
    [InlineData("../../outside.txt")]
    [InlineData("..\\outside.txt")]
    [InlineData("src/../outside.txt")]
    [InlineData("src\\..\\outside.txt")]
    [InlineData("src/./Program.cs")]
    [InlineData("src//Program.cs")]
    [InlineData("src\\\\Program.cs")]
    [InlineData("/etc/passwd")]
    [InlineData("\\Windows\\system.ini")]
    [InlineData("C:\\Windows\\system.ini")]
    [InlineData("C:relative.txt")]
    [InlineData(":(glob)**/*.cs")]
    [InlineData(":(top)README.md")]
    [InlineData("*.cs")]
    [InlineData("example?.txt")]
    [InlineData("[abc].txt")]
    public void Validate_InvalidPath_ThrowsArgumentException(
        string path
    )
    {
        var exception = Assert.Throws<ArgumentException>(
            () => GitRepositoryPathValidator.Validate(
                _root,
                path
            )
        );

        Assert.Equal("path", exception.ParamName);
    }

}
