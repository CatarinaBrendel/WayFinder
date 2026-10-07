namespace WayFinder.DevTools.Application.Context;

public sealed record ContextPackage(
    string Task,
    string ProjectName,
    ContextFile? Guidance,
    IReadOnlyCollection<ContextFile> Files,
    IReadOnlyCollection<ContextSelection> Selections,
    ContextStatistics Statistics
);
