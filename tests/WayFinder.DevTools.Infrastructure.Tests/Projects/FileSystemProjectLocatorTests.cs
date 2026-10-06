using WayFinder.DevTools.Infrastructure.Projects;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects;

public sealed class FileSystemProjectLocatorTests : IDisposable
{
    private readonly string _root;

    public FileSystemProjectLocatorTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-tests-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Locate_FromRepositoryRoot_ReturnsProject()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".git"));

        var locator = new FileSystemProjectLocator();

        var project = locator.Locate(_root);

        Assert.NotNull(project);
        Assert.Equal(new DirectoryInfo(_root).Name, project.Name);
        Assert.Equal(Path.GetFullPath(_root), project.RootPath);
        Assert.True(project.IsGitRepository);
    }

    [Fact]
    public void Locate_FromNestedDirectory_ReturnsRepositoryRoot()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".git"));

        var nested = Path.Combine(
            _root,
            "src",
            "WayFinder",
            "Something"
        );

        Directory.CreateDirectory(nested);

        var locator = new FileSystemProjectLocator();

        var project = locator.Locate(nested);

        Assert.NotNull(project);
        Assert.Equal(Path.GetFullPath(_root), project.RootPath);
    }

    [Fact]
    public void Locate_WhenNoRepositoryExists_ReturnsNull()
    {
        var nested = Path.Combine(_root, "somewhere");

        Directory.CreateDirectory(nested);

        var locator = new FileSystemProjectLocator();

        var project = locator.Locate(nested);

        Assert.Null(project);
    }

    [Fact]
    public void Locate_WhenPathDoesNotExist_Throws()
    {
        var missing = Path.Combine(_root, "does-not-exist");

        var locator = new FileSystemProjectLocator();

        Assert.Throws<DirectoryNotFoundException>(
            () => locator.Locate(missing)
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
