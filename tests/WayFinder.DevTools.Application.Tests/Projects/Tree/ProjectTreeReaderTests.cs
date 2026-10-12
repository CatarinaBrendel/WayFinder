using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Projects.Tree;

namespace WayFinder.DevTools.Application.Tests.Projects.Tree;

public sealed class ProjectTreeReaderTests
{
    private readonly ProjectContext _project = new(
        Name: "TestProject",
        RootPath: Path.Combine(
            Path.GetTempPath(),
            "TestProject"
        ),
        IsGitRepository: true
    );

    [Fact]
    public void Read_WithDepthOne_ReturnsOnlyImmediateChildren()
    {
        var fileSystem = CreateFileSystem();
        var reader = new ProjectTreeReader(fileSystem);

        var tree = reader.Read(
            _project,
            maxDepth: 1
        );

        Assert.Equal("TestProject", tree.Name);
        Assert.Equal("", tree.Path);
        Assert.Equal(
            ProjectTreeEntryType.Directory,
            tree.Type
        );

        Assert.Collection(
            tree.Children,
            src =>
            {
                Assert.Equal("src", src.Name);
                Assert.Equal("src", src.Path);
                Assert.Equal(
                    ProjectTreeEntryType.Directory,
                    src.Type
                );
                Assert.Empty(src.Children);
            },
            readme =>
            {
                Assert.Equal("README.md", readme.Name);
                Assert.Equal("README.md", readme.Path);
                Assert.Equal(
                    ProjectTreeEntryType.File,
                    readme.Type
                );
                Assert.Empty(readme.Children);
            }
        );
    }

    [Fact]
    public void Read_WithDepthTwo_ReturnsSecondLevel()
    {
        var fileSystem = CreateFileSystem();
        var reader = new ProjectTreeReader(fileSystem);

        var tree = reader.Read(
            _project,
            maxDepth: 2
        );

        var src = tree.Children[0];

        var application = Assert.Single(
            src.Children
        );

        Assert.Equal(
            "Application",
            application.Name
        );

        Assert.Equal(
            Path.Combine("src", "Application"),
            application.Path
        );

        Assert.Empty(
            application.Children
        );
    }

    [Fact]
    public void Read_WithUnlimitedDepth_ReturnsEntireTree()
    {
        var fileSystem = CreateFileSystem();
        var reader = new ProjectTreeReader(fileSystem);

        var tree = reader.Read(
            _project,
            maxDepth: null
        );

        var src = tree.Children[0];
        var application = Assert.Single(src.Children);
        var program = Assert.Single(application.Children);

        Assert.Equal("Program.cs", program.Name);

        Assert.Equal(
            Path.Combine(
                "src",
                "Application",
                "Program.cs"
            ),
            program.Path
        );

        Assert.Equal(
            ProjectTreeEntryType.File,
            program.Type
        );

        Assert.Empty(program.Children);
    }

    [Fact]
    public void Read_WithZeroDepth_Throws()
    {
        var fileSystem = CreateFileSystem();
        var reader = new ProjectTreeReader(fileSystem);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => reader.Read(
                _project,
                maxDepth: 0
            )
        );
    }

    [Fact]
    public void Read_WithNegativeDepth_Throws()
    {
        var fileSystem = CreateFileSystem();
        var reader = new ProjectTreeReader(fileSystem);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => reader.Read(
                _project,
                maxDepth: -1
            )
        );
    }

    private static IProjectFileSystem CreateFileSystem()
    {
        return new FakeProjectFileSystem(
            new Dictionary<
                string,
                IReadOnlyCollection<ProjectDirectoryEntry>
            >
            {
                [""] =
                [
                    new(
                        "src",
                        "src",
                        IsDirectory: true
                    ),
                    new(
                        "README.md",
                        "README.md",
                        IsDirectory: false
                    ),
                ],
                ["src"] =
                [
                    new(
                        "Application",
                        Path.Combine(
                            "src",
                            "Application"
                        ),
                        IsDirectory: true
                    ),
                ],
                [Path.Combine("src", "Application")] =
                [
                    new(
                        "Program.cs",
                        Path.Combine(
                            "src",
                            "Application",
                            "Program.cs"
                        ),
                        IsDirectory: false
                    ),
                ],
            }
        );
    }

    private sealed class FakeProjectFileSystem(
        IReadOnlyDictionary<
            string,
            IReadOnlyCollection<ProjectDirectoryEntry>
        > entries
    ) : IProjectFileSystem
    {
        public IReadOnlyCollection<ProjectDirectoryEntry> GetEntries(
            ProjectContext project,
            string relativePath
        )
        {
            return entries.TryGetValue(
                relativePath,
                out var result
            )
                ? result
                : [];
        }

        public bool FileExists(
            ProjectContext project,
            string relativePath
        )
        {
            throw new NotSupportedException();
        }

        public IReadOnlyCollection<ProjectFile> FindFiles(
            ProjectContext project,
            string searchPattern
        )
        {
            throw new NotSupportedException();
        }

        public string ReadAllText(
            ProjectContext project,
            string relativePath
        )
        {
            throw new NotSupportedException();
        }

        public void WriteAllText(
            ProjectContext project,
            string relativePath,
            string content
        )
        {
            throw new NotSupportedException();
        }

        public ProjectFileRead Read(
            ProjectContext project,
            string relativePath,
            int maxBytes
        )
        {
            throw new NotSupportedException();
        }

        public ProjectFileRead Read(
            ProjectContext project,
            string relativePath,
            long offset,
            int maxBytes
        )
        {
            throw new NotSupportedException();
        }

        public IReadOnlyCollection<ProjectFile> GetFiles(
            ProjectContext project
        )
        {
            throw new NotSupportedException();
        }
    }
}
