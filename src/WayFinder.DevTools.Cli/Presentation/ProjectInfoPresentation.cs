using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Inspection;
using WayFinder.DevTools.Application.Projects.Manifest;

namespace WayFinder.DevTools.Cli.Presentation;

public static class ProjectInfoPresentation
{
    public static void Write(
        TextWriter output,
        ProjectContext project,
        ProjectManifest? manifest,
        ProjectInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(inspection);

        output.WriteLine(
            $"{ConsoleTheme.Waypoint} {ConsoleTheme.Bold(project.Name)}"
        );

        output.WriteLine(ConsoleTheme.Rule);
        output.WriteLine();

        output.WriteLine(ConsoleTheme.Bold("Project"));

        output.WriteLine($"  Root       {project.RootPath}");

        output.WriteLine(
            $"  Git        {FormatState(project.IsGitRepository, "yes", "no")}"
        );

        output.WriteLine(
            $"  Manifest   {FormatState(manifest is not null, "configured", "not configured")}"
        );

        if (manifest is not null)
        {
            WriteManifest(output, manifest);
        }

        WriteInspection(output, inspection);

        if (manifest is null)
        {
            WriteManifestHint(output);
        }
    }

    private static string FormatState(
        bool value,
        string trueText,
        string falseText)
    {
        return value
            ? $"{ConsoleTheme.SuccessMark} {ConsoleTheme.Success(trueText)}"
            : $"{ConsoleTheme.WarningMark} {ConsoleTheme.Warning(falseText)}";
    }

    private static void WriteManifest(
        TextWriter output,
        ProjectManifest manifest
    )
    {
        output.WriteLine();

        output.WriteLine(
            ConsoleTheme.Bold("Declared")
        );

        if (manifest.Technologies.Count == 0)
        {
            output.WriteLine("  none");
            return;
        }

        foreach (var technology in manifest.Technologies)
        {
            output.WriteLine(
                $"  {ConsoleTheme.Waypoint} {technology}"
            );
        }
    }

    private static void WriteInspection(
        TextWriter output,
        ProjectInspection inspection
    )
    {
        var detected =
            inspection.Artifacts
                .Where(
                    entry =>
                        entry.Value.Count > 0
                )
                .ToArray();

        if (detected.Length == 0)
        {
            return;
        }

        output.WriteLine();

        output.WriteLine(
            ConsoleTheme.Bold("Detected")
        );

        foreach (var (detector, artifacts) in detected)
        {
            output.WriteLine(
                $"  {ConsoleTheme.SuccessMark} {ConsoleTheme.Bold(detector)}"
            );

            foreach (var artifact in artifacts)
            {
                output.WriteLine(
                    $"      {artifact.Type,-12} {artifact.Path}"
                );
            }
        }
    }

    private static void WriteManifestHint(TextWriter output)
    {
        output.WriteLine();

        output.WriteLine(
            $"{ConsoleTheme.WarningMark} WayFinder metadata is not configured."
        );

        output.WriteLine(
            $"  Run {ConsoleTheme.Bold("/project init")} to set this waypoint up."
        );
    }
}
