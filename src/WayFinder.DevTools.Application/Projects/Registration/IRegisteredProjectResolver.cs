namespace WayFinder.DevTools.Application.Projects.Registration;

public interface IRegisteredProjectResolver
{
    ProjectContext Resolve(
        Guid projectId
    );
}
