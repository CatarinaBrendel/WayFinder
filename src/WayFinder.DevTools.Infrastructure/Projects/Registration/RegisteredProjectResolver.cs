using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registration;
using WayFinder.DevTools.Application.Projects.Registry;

namespace WayFinder.DevTools.Infrastructure.Projects.Registration;

public sealed class RegisteredProjectResolver
    : IRegisteredProjectResolver
{
    private readonly IProjectRegistry _registry;

    public RegisteredProjectResolver(IProjectRegistry registry)
    {
        _registry = registry;
    }

    public ProjectContext Resolve(Guid projectId)
    {
        var registeredProject = _registry.FindById(projectId);

        if (registeredProject is null)
        {
            throw new RegisteredProjectNotFoundException(projectId);
        }

        return CreateProjectContext(registeredProject);
    }

    public ProjectContext Resolve(string project)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(project);

        if (Guid.TryParse(project, out var projectId))
        {
            return Resolve(projectId);
        }

        var matches = _registry
            .GetAll()
            .Where(candidate =>
                string.Equals(
                    candidate.Name,
                    project,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Take(2)
            .ToArray();

        return matches.Length switch
        {
            0 => throw new RegisteredProjectReferenceNotFoundException(project),
            1 => CreateProjectContext(matches[0]),
            _ => throw new RegisteredProjectAmbiguousException(project)
        };
    }

    private static ProjectContext CreateProjectContext(
        RegisteredProject registeredProject
    )
    {
        return new ProjectContext(
            registeredProject.Name,
            registeredProject.RootPath,
            IsGitRepository: true
        );
    }
}
