namespace WayFinder.DevTools.Application.Projects.Tree;

public interface IProjectTreeReader
{
    ProjectTreeEntry Read(
        ProjectContext project,
        int? maxDepth = null
    );
}
