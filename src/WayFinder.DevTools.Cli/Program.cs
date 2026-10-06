using System.CommandLine;
using WayFinder.DevTools.Cli.Commands.Projects;
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

return rootCommand
    .Parse(args)
    .Invoke();
