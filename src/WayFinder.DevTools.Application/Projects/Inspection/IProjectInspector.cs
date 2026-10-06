namespace WayFinder.DevTools.Application.Projects.Inspection;

public interface IProjectInspector
{
    ProjectInspection Inspect(ProjectContext project);
}
