namespace WayFinder.DevTools.Application.Projects;

public interface IProjectLocator
{
    ProjectContext? Locate(string startPath);
}
