
namespace WayFinder.DevTools.Application.Projects.Registration;

public sealed class RegisteredProjectAmbiguousException
    : Exception
{
    public RegisteredProjectAmbiguousException(string project)
        : base($"Registered project name '{project}' is ambiguous. Use the project ID.")
    {
        Project = project;
    }

    public string Project { get; }
}
