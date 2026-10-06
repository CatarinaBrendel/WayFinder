using WayFinder.DevTools.Application.Configuration;

namespace WayFinder.DevTools.Infrastructure.Configuration;

public sealed class WayFinderEnvironment
    : IWayFinderEnvironment
{
    public string HomePath { get; }

    public string RegistryPath =>
        Path.Combine(
            HomePath,
            "registry.json"
        );

    public WayFinderEnvironment()
    {
        var userProfile = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile
        );

        if (string.IsNullOrWhiteSpace(userProfile))
        {
            throw new InvalidOperationException(
                "Could not determine the current user's home directory."
            );
        }

        HomePath = Path.Combine(
            userProfile,
            ".wayfinder"
        );
    }
}
