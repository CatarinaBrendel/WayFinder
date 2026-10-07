using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registration;
using WayFinder.DevTools.Application.Projects.Registry;
using WayFinder.DevTools.Infrastructure.Projects.Registration;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Registration;

public sealed class RegisteredProjectResolverTests
{
    [Fact]
    public void Resolve_RegisteredProject_ReturnsProjectContext()
    {
        var projectId =
            Guid.NewGuid();

        var registeredProject =
            new RegisteredProject(
                projectId,
                "TestProject",
                "/projects/test"
            );

        var registry =
            new TestProjectRegistry(
                registeredProject
            );

        var resolver =
            new RegisteredProjectResolver(
                registry
            );

        var result =
            resolver.Resolve(
                projectId
            );

        Assert.Equal(
            "TestProject",
            result.Name
        );

        Assert.Equal(
            "/projects/test",
            result.RootPath
        );

        Assert.True(
            result.IsGitRepository
        );
    }

    [Fact]
    public void Resolve_UnknownProject_ThrowsRegisteredProjectNotFoundException()
    {
        var projectId =
            Guid.NewGuid();

        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry()
            );

        var exception =
            Assert.Throws<RegisteredProjectNotFoundException>(
                () =>
                    resolver.Resolve(
                        projectId
                    )
            );

        Assert.Equal(
            projectId,
            exception.ProjectId
        );
    }

    [Fact]
    public void Resolve_GuidString_ReturnsProjectContext()
    {
        var projectId =
            Guid.NewGuid();

        var registeredProject =
            new RegisteredProject(
                projectId,
                "TestProject",
                "/projects/test"
            );

        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry(
                    registeredProject
                )
            );

        var result =
            resolver.Resolve(
                projectId.ToString()
            );

        Assert.Equal(
            "TestProject",
            result.Name
        );

        Assert.Equal(
            "/projects/test",
            result.RootPath
        );
    }

    [Fact]
    public void Resolve_UniqueProjectName_ReturnsProjectContext()
    {
        var registeredProject =
            new RegisteredProject(
                Guid.NewGuid(),
                "TestProject",
                "/projects/test"
            );

        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry(
                    registeredProject
                )
            );

        var result =
            resolver.Resolve(
                "TestProject"
            );

        Assert.Equal(
            "TestProject",
            result.Name
        );

        Assert.Equal(
            "/projects/test",
            result.RootPath
        );
    }

    [Fact]
    public void Resolve_ProjectName_IsCaseInsensitive()
    {
        var registeredProject =
            new RegisteredProject(
                Guid.NewGuid(),
                "TestProject",
                "/projects/test"
            );

        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry(
                    registeredProject
                )
            );

        var result =
            resolver.Resolve(
                "testproject"
            );

        Assert.Equal(
            "TestProject",
            result.Name
        );
    }

    [Fact]
    public void Resolve_UnknownProjectName_ThrowsRegisteredProjectReferenceNotFoundException()
    {
        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry()
            );

        var exception =
            Assert.Throws<RegisteredProjectReferenceNotFoundException>(
                () =>
                    resolver.Resolve(
                        "UnknownProject"
                    )
            );

        Assert.Equal(
            "UnknownProject",
            exception.Project
        );
    }

    [Fact]
    public void Resolve_EmptyProjectReference_ThrowsArgumentException()
    {
        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry()
            );

        Assert.Throws<ArgumentException>(
            () =>
                resolver.Resolve(
                    ""
                )
        );
    }

    [Fact]
    public void Resolve_WhitespaceProjectReference_ThrowsArgumentException()
    {
        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry()
            );

        Assert.Throws<ArgumentException>(
            () =>
                resolver.Resolve(
                    "   "
                )
        );
    }

    [Fact]
    public void Resolve_PartialProjectName_ThrowsRegisteredProjectReferenceNotFoundException()
    {
        var registeredProject =
            new RegisteredProject(
                Guid.NewGuid(),
                "TestProject",
                "/projects/test"
            );

        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry(
                    registeredProject
                )
            );

        Assert.Throws<RegisteredProjectReferenceNotFoundException>(
            () =>
                resolver.Resolve(
                    "Test"
                )
        );
    }

    [Fact]
    public void Resolve_DuplicateProjectName_ThrowsRegisteredProjectAmbiguousException()
    {
        var firstProject =
            new RegisteredProject(
                Guid.NewGuid(),
                "TestProject",
                "/projects/first"
            );

        var secondProject =
            new RegisteredProject(
                Guid.NewGuid(),
                "TestProject",
                "/projects/second"
            );

        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry(
                    firstProject,
                    secondProject
                )
            );

        var exception =
            Assert.Throws<RegisteredProjectAmbiguousException>(
                () =>
                    resolver.Resolve(
                        "TestProject"
                    )
            );

        Assert.Equal(
            "TestProject",
            exception.Project
        );
    }

    [Fact]
    public void Resolve_DuplicateProjectNamesWithDifferentCasing_ThrowsRegisteredProjectAmbiguousException()
    {
        var firstProject =
            new RegisteredProject(
                Guid.NewGuid(),
                "TestProject",
                "/projects/first"
            );

        var secondProject =
            new RegisteredProject(
                Guid.NewGuid(),
                "testproject",
                "/projects/second"
            );

        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry(
                    firstProject,
                    secondProject
                )
            );

        Assert.Throws<RegisteredProjectAmbiguousException>(
            () =>
                resolver.Resolve(
                    "TESTPROJECT"
                )
        );
    }

    [Fact]
    public void Resolve_UnknownGuidString_DoesNotFallBackToProjectName()
    {
        var projectId =
            Guid.NewGuid();

        var registeredProject =
            new RegisteredProject(
                Guid.NewGuid(),
                projectId.ToString(),
                "/projects/test"
            );

        var resolver =
            new RegisteredProjectResolver(
                new TestProjectRegistry(
                    registeredProject
                )
            );

        var exception =
            Assert.Throws<RegisteredProjectNotFoundException>(
                () =>
                    resolver.Resolve(
                        projectId.ToString()
                    )
            );

        Assert.Equal(
            projectId,
            exception.ProjectId
        );
    }

    private sealed class TestProjectRegistry
        : IProjectRegistry
    {
        private readonly IReadOnlyCollection<RegisteredProject> _projects;

        public TestProjectRegistry(
            params RegisteredProject[] projects
        )
        {
            _projects =
                projects;
        }

        public IReadOnlyCollection<RegisteredProject> GetAll()
        {
            return _projects;
        }

        public RegisteredProject? FindById(
            Guid id
        )
        {
            return _projects.FirstOrDefault(
                project =>
                    project.Id == id
            );
        }

        public RegisteredProject? FindByRootPath(
            string rootPath
        )
        {
            return _projects.FirstOrDefault(
                project =>
                    project.RootPath == rootPath
            );
        }

        public RegisteredProject Add(
            ProjectContext project
        )
        {
            throw new NotSupportedException();
        }

        public bool Remove(
            Guid id
        )
        {
            throw new NotSupportedException();
        }
    }
}
