using WayFinder.DevTools.Application.Configuration;
using WayFinder.DevTools.Application.Diagnostics;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registry;
using WayFinder.DevTools.Infrastructure.Diagnostics;

namespace WayFinder.DevTools.Infrastructure.Tests.Diagnostics;

public sealed class DoctorTests : IDisposable
{
    private readonly string _root;

    public DoctorTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-doctor-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(_root);
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
    public void Examine_ReportsEnvironmentHomeAsHealthy()
    {
        var doctor = CreateDoctor();

        var report = doctor.Examine();

        var check =
            Assert.Single(
                report.Checks,
                check =>
                    check.Name == "environment.home"
            );

        Assert.Equal(
            DoctorCheckStatus.Ok,
            check.Status
        );

        Assert.Equal(
            _root,
            check.Message
        );
    }

    [Fact]
    public void Examine_ReportsValidRegistryAsHealthy()
    {
        var doctor = CreateDoctor();

        var report = doctor.Examine();

        var check =
            Assert.Single(
                report.Checks,
                check =>
                    check.Name == "registry"
            );

        Assert.Equal(
            DoctorCheckStatus.Ok,
            check.Status
        );
    }

    [Fact]
    public void Examine_ReportsExistingRegisteredProjectAsHealthy()
    {
        var projectRoot =
            Path.Combine(
                _root,
                "ExistingProject"
            );

        Directory.CreateDirectory(projectRoot);

        var registeredProject =
            new RegisteredProject(
                Guid.NewGuid(),
                "ExistingProject",
                projectRoot
            );

        var doctor =
            CreateDoctor(
                projects: [registeredProject]
            );

        var report = doctor.Examine();

        var check =
            Assert.Single(
                report.Checks,
                check =>
                    check.Name ==
                    $"project.{registeredProject.Id}"
            );

        Assert.Equal(
            DoctorCheckStatus.Ok,
            check.Status
        );
    }

    [Fact]
    public void Examine_ReportsMissingRegisteredProjectAsWarning()
    {
        var registeredProject =
            new RegisteredProject(
                Guid.NewGuid(),
                "MissingProject",
                Path.Combine(
                    _root,
                    "does-not-exist"
                )
            );

        var doctor =
            CreateDoctor(
                projects: [registeredProject]
            );

        var report = doctor.Examine();

        var check =
            Assert.Single(
                report.Checks,
                check =>
                    check.Name ==
                    $"project.{registeredProject.Id}"
            );

        Assert.Equal(
            DoctorCheckStatus.Warning,
            check.Status
        );

        Assert.False(report.HasErrors);
        Assert.Equal(1, report.WarningCount);
    }

    [Fact]
    public void Examine_ReportsCurrentProjectAsHealthy()
    {
        var project =
            new ProjectContext(
                "CurrentProject",
                _root,
                true
            );

        var doctor =
            CreateDoctor(
                currentProject: project
            );

        var report = doctor.Examine();

        var check =
            Assert.Single(
                report.Checks,
                check =>
                    check.Name == "project.current"
            );

        Assert.Equal(
            DoctorCheckStatus.Ok,
            check.Status
        );

        Assert.Contains(
            "CurrentProject",
            check.Message
        );
    }

    [Fact]
    public void Examine_ReportsNoCurrentProjectAsHealthy()
    {
        var doctor =
            CreateDoctor(
                currentProject: null
            );

        var report = doctor.Examine();

        var check =
            Assert.Single(
                report.Checks,
                check =>
                    check.Name == "project.current"
            );

        Assert.Equal(
            DoctorCheckStatus.Ok,
            check.Status
        );

        Assert.False(report.HasErrors);
    }

    [Fact]
    public void Examine_ReportsInvalidRegistryAsError()
    {
        var doctor =
            CreateDoctor(
                registryException:
                    new InvalidDataException(
                        "Registry is broken."
                    )
            );

        var report = doctor.Examine();

        var check =
            Assert.Single(
                report.Checks,
                check =>
                    check.Name == "registry"
            );

        Assert.Equal(
            DoctorCheckStatus.Error,
            check.Status
        );

        Assert.True(report.HasErrors);
        Assert.Equal(1, report.ErrorCount);
    }

    private Doctor CreateDoctor(
        IReadOnlyCollection<RegisteredProject>? projects = null,
        ProjectContext? currentProject = null,
        Exception? registryException = null
    )
    {
        var environment =
            new TestEnvironment(_root);

        var registry =
            new TestProjectRegistry(
                projects ?? [],
                registryException
            );

        var locator =
            new TestProjectLocator(
                currentProject
            );

        return new Doctor(
            environment,
            registry,
            locator
        );
    }

    private sealed class TestEnvironment(
        string homePath
    ) : IWayFinderEnvironment
    {
        public string HomePath { get; } =
            homePath;

        public string RegistryPath =>
            Path.Combine(
                HomePath,
                "registry.json"
            );
    }

    private sealed class TestProjectLocator(
        ProjectContext? project
    ) : IProjectLocator
    {
        public ProjectContext? Locate(
            string startPath
        )
        {
            return project;
        }
    }

    private sealed class TestProjectRegistry(
        IReadOnlyCollection<RegisteredProject> projects,
        Exception? exception = null
    ) : IProjectRegistry
    {
        public IReadOnlyCollection<RegisteredProject> GetAll()
        {
            if (exception is not null)
            {
                throw exception;
            }

            return projects;
        }

        public RegisteredProject? FindById(
            Guid id
        )
        {
            return projects.FirstOrDefault(
                project => project.Id == id
            );
        }

        public RegisteredProject? FindByRootPath(
            string rootPath
        )
        {
            return projects.FirstOrDefault(
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

        public bool Remove(Guid id)
        {
            throw new NotSupportedException();
        }
    }
}
