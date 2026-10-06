namespace WayFinder.DevTools.Application.Diagnostics;

public sealed record DoctorReport(
    IReadOnlyCollection<DoctorCheck> Checks
)
{
    public bool HasErrors =>
        Checks.Any(
            check =>
                check.Status == DoctorCheckStatus.Error
        );

    public int WarningCount =>
        Checks.Count(
            check =>
                check.Status == DoctorCheckStatus.Warning
        );

    public int ErrorCount =>
        Checks.Count(
            check =>
                check.Status == DoctorCheckStatus.Error
        );
}
