using WayFinder.DevTools.Application.Repositories.Reading;
using WayFinder.DevTools.Mcp.Contracts.Repositories.Reading;

namespace WayFinder.DevTools.Mcp.Mapping;

internal static class RepositoryFileResponseMapper
{
    public static RepositoryFileResponse Map(
        RepositoryFileContent content
    )
    {
        ArgumentNullException.ThrowIfNull(
            content
        );

        return new RepositoryFileResponse(
            Path: content.Path,
            Content: content.Content,
            TotalBytes: content.TotalBytes,
            Mode: "file",
            Truncated: content.Truncated,
            StartLine: null,
            EndLine: null
        );
    }

    public static RepositoryFileResponse Map(
        RepositoryFileRangeContent content
    )
    {
        ArgumentNullException.ThrowIfNull(
            content
        );

        return new RepositoryFileResponse(
            Path: content.Path,
            Content: content.Content,
            TotalBytes: content.TotalBytes,
            Mode: "range",
            Truncated: null,
            StartLine: content.StartLine,
            EndLine: content.EndLine
        );
    }
}
