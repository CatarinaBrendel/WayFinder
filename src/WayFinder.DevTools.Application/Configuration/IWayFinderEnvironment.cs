namespace WayFinder.DevTools.Application.Configuration;

public interface IWayFinderEnvironment
{
    string HomePath { get; }
    string RegistryPath { get; }
}
