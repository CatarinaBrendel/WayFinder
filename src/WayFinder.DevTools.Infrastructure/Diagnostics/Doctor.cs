using WayFinder.DevTools.Application.Configuration;
using WayFinder.DevTools.Application.Diagnostics;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registry;

namespace WayFinder.DevTools.Infrastructure.Diagnostics;

public sealed class Doctor(
    IWayFinderEnvironment environment,
    IProjectRegistry projectRegistry,
    IProjectLocator projectLocator
) : IDoctor
{
    public DoctorReport Examine()
    {
        var checks =
            new List<DoctorCheck>();

        CheckEnvironment(checks);

        var projects =
            CheckRegistry(checks);

        if (projects is not null)
        {
            CheckRegisteredProjects(
                checks,
                projects
            );
        }

        CheckCurrentProject(checks);

        return new DoctorReport(checks);
    }

    private void CheckEnvironment(
        ICollection<DoctorCheck> checks
    )
    {
        checks.Add(
            new DoctorCheck(
                Name: "environment.home",
                Status: DoctorCheckStatus.Ok,
                Message: environment.HomePath
            )
        );
    }

    private static void CheckRegisteredProjects(
        ICollection<DoctorCheck> checks,
        IReadOnlyCollection<RegisteredProject> projects
    )
    {
        foreach (var project in projects)
        {
            checks.Add(
                Directory.Exists(project.RootPath)
                    ? new DoctorCheck(
                        Name: $"project.{project.Id}",
                        Status: DoctorCheckStatus.Ok,
                        Message: project.Name
                    )
                    : new DoctorCheck(
                        Name: $"project.{project.Id}",
                        Status: DoctorCheckStatus.Warning,
                        Message:
                            $"{project.Name}: registered root does not exist."
                    )
            );
        }
    }

    private IReadOnlyCollection<RegisteredProject>? CheckRegistry(
        ICollection<DoctorCheck> checks
    )
    {
        try
        {
            var projects =
                projectRegistry.GetAll();

            checks.Add(
                new DoctorCheck(
                    Name: "registry",
                    Status: DoctorCheckStatus.Ok,
                    Message: "Registry is valid."
                )
            );

            return projects;
        }
        catch (Exception exception)
            when (exception is InvalidDataException
                  or IOException
                  or UnauthorizedAccessException)
        {
            checks.Add(
                new DoctorCheck(
                    Name: "registry",
                    Status: DoctorCheckStatus.Error,
                    Message: exception.Message
                )
            );

            return null;
        }
    }

    private void CheckRegisteredProjects(
        ICollection<DoctorCheck> checks
    )
    {
        IReadOnlyCollection<RegisteredProject> projects;

        try
        {
            projects =
                projectRegistry.GetAll();
        }
        catch (Exception exception)
            when (exception is InvalidDataException
                  or IOException
                  or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var project in projects)
        {
            checks.Add(
                Directory.Exists(project.RootPath)
                    ? new DoctorCheck(
                        Name: $"project.{project.Id}",
                        Status: DoctorCheckStatus.Ok,
                        Message: project.Name
                    )
                    : new DoctorCheck(
                        Name: $"project.{project.Id}",
                        Status: DoctorCheckStatus.Warning,
                        Message:
                            $"{project.Name}: registered root does not exist."
                    )
            );
        }
    }

    private void CheckCurrentProject(
        ICollection<DoctorCheck> checks
    )
    {
        var project =
            projectLocator.Locate(
                Directory.GetCurrentDirectory()
            );

        if (project is null)
        {
            checks.Add(
                new DoctorCheck(
                    Name: "project.current",
                    Status: DoctorCheckStatus.Ok,
                    Message: "Not currently inside a Git project."
                )
            );

            return;
        }

        checks.Add(
            new DoctorCheck(
                Name: "project.current",
                Status: DoctorCheckStatus.Ok,
                Message:
                    $"{project.Name}: Git repository detected."
            )
        );
    }
}
