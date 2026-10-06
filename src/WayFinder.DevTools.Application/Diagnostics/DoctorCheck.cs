namespace WayFinder.DevTools.Application.Diagnostics;

public sealed record DoctorCheck(
    string Name,
    DoctorCheckStatus Status,
    string Message
);
