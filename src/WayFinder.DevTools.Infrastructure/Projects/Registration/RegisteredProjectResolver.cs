using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Registration;
using WayFinder.DevTools.Application.Projects.Registry;

namespace WayFinder.DevTools.Infrastructure.Projects.Registration;

public sealed class RegisteredProjectResolver
    : IRegisteredProjectResolver
{
    private readonly IProjectRegistry _registry;

    public RegisteredProjectResolver(
        IProjectRegistry registry
    )
    {
        _registry =
            registry;
    }

    public ProjectContext Resolve(
        Guid projectId
    )
    {
        var registeredProject =
            _registry.FindById(
                projectId
            );

        if (registeredProject is null)
        {
            throw new RegisteredProjectNotFoundException(
                projectId
            );
        }

        return new ProjectContext(
            registeredProject.Name,
            registeredProject.RootPath,
            IsGitRepository: true
        );
    }
}
