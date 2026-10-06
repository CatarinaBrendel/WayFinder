using System.CommandLine;
using WayFinder.DevTools.Application.Diagnostics;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands;

internal static class DoctorCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var command =
            new Command(
                "doctor",
                "Check the health of your WayFinder setup"
            );

        command.SetAction(
            _ => Execute(services)
        );

        return command;
    }

    private static int Execute(
        WayFinderServices services
    )
    {
        var report =
            services.Doctor.Examine();

        WriteHeader();

        foreach (var check in report.Checks)
        {
            WriteCheck(check);
        }

        WriteSummary(report);

        return report.HasErrors
            ? ExitCodes.Failure
            : ExitCodes.Success;
    }

    private static void WriteHeader()
    {
        Console.WriteLine(
            $"  {ConsoleTheme.Accent("◇")}{ConsoleTheme.Secondary("─╮")}"
        );

        Console.WriteLine(
            $"    {ConsoleTheme.Secondary("│")}  {ConsoleTheme.Bold("WayFinder Doctor")}"
        );

        Console.WriteLine(
            $"  {ConsoleTheme.Secondary("╰─╯")}  Checking the route..."
        );

        Console.WriteLine();
    }

    private static void WriteCheck(
        DoctorCheck check
    )
    {
        var symbol =
            check.Status switch
            {
                DoctorCheckStatus.Ok =>
                    ConsoleTheme.Success("✓"),

                DoctorCheckStatus.Warning =>
                    ConsoleTheme.Warning("!"),

                DoctorCheckStatus.Error =>
                    ConsoleTheme.Error("✗"),

                _ => "?"
            };

        Console.WriteLine(
            $"  {symbol} {check.Message}"
        );
    }

    private static void WriteSummary(
        DoctorReport report
    )
    {
        var healthyCount =
            report.Checks.Count(
                check =>
                    check.Status ==
                    DoctorCheckStatus.Ok
            );

        Console.WriteLine();
        Console.WriteLine(
            ConsoleTheme.Secondary(
                "────────────────────────────────────────"
            )
        );

        Console.Write("  ");

        Console.Write(
            ConsoleTheme.Success(
                $"✓ {healthyCount} healthy"
            )
        );

        Console.Write("   ");

        Console.Write(
            ConsoleTheme.Warning(
                $"! {report.WarningCount} warnings"
            )
        );

        Console.Write("   ");

        Console.WriteLine(
            ConsoleTheme.Error(
                $"✗ {report.ErrorCount} errors"
            )
        );

        Console.WriteLine();

        if (report.HasErrors)
        {
            Console.WriteLine(
                "Something is blocking the route."
            );
        }
        else if (report.WarningCount > 0)
        {
            Console.WriteLine(
                "Nothing critical. A waypoint could use some attention."
            );
        }
        else
        {
            Console.WriteLine(
                "All clear. The route looks good."
            );
        }
    }
}
