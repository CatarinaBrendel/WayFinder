using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class DoctorInteractiveCommand : IInteractiveCommand
{
    private readonly WayFinderServices _services;

    public DoctorInteractiveCommand(
        WayFinderServices services)
    {
        _services = services;
    }

    public string Name => "/doctor";

    public string Description =>
        "Check the WayFinder environment";

    public Task<InteractiveCommandResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var report = _services.Doctor.Examine();

        InteractivePresentation.WriteDoctorReport(
            output,
            report
        );

        return Task.FromResult(
            InteractiveCommandResult.Continue
        );
    }
}
