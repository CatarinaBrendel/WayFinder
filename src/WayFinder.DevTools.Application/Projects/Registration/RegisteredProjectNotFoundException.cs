namespace WayFinder.DevTools.Application.Projects.Registration;

public sealed class RegisteredProjectNotFoundException
    : Exception
{
    public RegisteredProjectNotFoundException(
        Guid projectId
    )
        : base(
            $"Registered project '{projectId}' was not found."
        )
    {
        ProjectId =
            projectId;
    }

    public Guid ProjectId { get; }
}
