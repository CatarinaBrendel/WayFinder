using System.CommandLine;
using WayFinder.DevTools.Cli.Commands;
using WayFinder.DevTools.Cli.Commands.Projects;
using WayFinder.DevTools.Cli.Commands.Repositories;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

var services =
    WayFinderComposition.Create();

var rootCommand =
    new RootCommand(
        "WayFinder personal developer tools"
    );

rootCommand.Subcommands.Add(
    ProjectCommand.Create(services)
);

rootCommand.Subcommands.Add(
    RepositoryCommand.Create(services)
);

rootCommand.Subcommands.Add(
    DoctorCommand.Create(services)
);

if (args.Length == 0
    || (args.Length == 1
        && (args[0] == "--help"
            || args[0] == "-h"
            || args[0] == "-?")))
{
    ConsoleBranding.Write();
}

return rootCommand
    .Parse(args)
    .Invoke();
