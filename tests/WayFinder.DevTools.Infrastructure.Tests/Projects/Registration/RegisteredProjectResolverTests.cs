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
