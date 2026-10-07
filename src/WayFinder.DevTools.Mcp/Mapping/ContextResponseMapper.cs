using WayFinder.DevTools.Application.Context;
using WayFinder.DevTools.Mcp.Contracts.Context;

namespace WayFinder.DevTools.Mcp.Mapping;

internal static class ContextResponseMapper
{
    public static ContextResponse Map(
        ContextPackage package
    )
    {
        ArgumentNullException.ThrowIfNull(
            package
        );

        return new ContextResponse(
            Project: package.ProjectName,
            Task: package.Task,
            Guidance: package.Guidance is null
                ? null
                : MapFile(package.Guidance),
            Files: package.Files
                .Select(MapFile)
                .ToArray(),
            Statistics:
                new ContextStatisticsResponse(
                    package.Statistics.FileCount,
                    package.Statistics.EstimatedTokens,
                    package.Statistics.TokenBudget
                )
        );
    }

    private static ContextFileResponse MapFile(
        ContextFile file
    )
    {
        return new ContextFileResponse(
            Path: file.Path,
            Kind: MapKind(file.Kind),
            Content: file.Content,
            EstimatedTokens: file.EstimatedTokens,
            StartLine: file.StartLine,
            EndLine: file.EndLine
        );
    }

    private static string MapKind(
        ContextFileKind kind
    )
    {
        return kind switch
        {
            ContextFileKind.Complete =>
                "complete",

            ContextFileKind.Excerpt =>
                "excerpt",

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "Unsupported context file kind."
                ),
        };
    }
}
