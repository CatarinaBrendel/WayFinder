using WayFinder.DevTools.Application.Repositories.Searching;
using WayFinder.DevTools.Mcp.Contracts.Repositories.Searching;

namespace WayFinder.DevTools.Mcp.Mapping;

internal static class RepositorySearchResponseMapper
{
    public static RepositorySearchResponse Map(
        RepositorySearchResult result
    )
    {
        ArgumentNullException.ThrowIfNull(
            result
        );

        return new RepositorySearchResponse(
            Matches:
                result.Matches
                    .Select(
                        match =>
                            new RepositorySearchMatchResponse(
                                match.Path,
                                match.LineNumber,
                                match.Line
                            )
                    )
                    .ToArray(),
            Truncated: result.Truncated
        );
    }
}
