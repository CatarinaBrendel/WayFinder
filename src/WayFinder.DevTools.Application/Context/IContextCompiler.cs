using WayFinder.DevTools.Application.Projects;

namespace WayFinder.DevTools.Application.Context;

public interface IContextCompiler
{
    ContextPackage Compile(
        ProjectContext project,
        ContextRequest request
    );
}
