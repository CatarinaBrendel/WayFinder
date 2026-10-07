namespace WayFinder.DevTools.Application.Projects.Registration;

public sealed class RegisteredProjectReferenceNotFoundException
    : Exception
{
    public RegisteredProjectReferenceNotFoundException(string project)
        : base($"Registered project '{project}' was not found.")
    {
        Project = project;
    }

    public string Project { get; }
}
