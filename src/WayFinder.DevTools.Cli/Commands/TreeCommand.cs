using System.CommandLine;
using WayFinder.DevTools.Application.Projects.Tree;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands;

internal static class TreeCommand
{
    private const int DefaultDepth = 3;

    public static Command Create(
        WayFinderServices services
    )
    {
        var depthOption =
            new Option<int>(
                "--depth"
            )
            {
                Description =
                    "Maximum tree depth. Use 0 for unlimited depth.",
                DefaultValueFactory =
                    _ => DefaultDepth,
            };

        var command =
            new Command(
                "tree",
                "Show the project structure as seen by WayFinder."
            )
            {
                depthOption,
            };

        command.SetAction(
            parseResult =>
            {
                var depth =
                    parseResult.GetValue(
                        depthOption
                    );

                return Execute(
                    services,
                    depth
                );
            }
        );

        return command;
    }

    private static int Execute(
        WayFinderServices services,
        int depth
    )
    {
        if (depth < 0)
        {
            Console.Error.WriteLine(
                ConsoleTheme.Error(
                    "✗ Depth must be zero or greater."
                )
            );

            return ExitCodes.Failure;
        }

        var project =
            services.ProjectLocator.Locate(
                Environment.CurrentDirectory
            );

        if (project is null)
        {
            Console.Error.WriteLine(
                ConsoleTheme.Error(
                    "✗ No project found."
                )
            );

            return ExitCodes.Failure;
        }

        var tree =
            services.ProjectTreeReader.Read(
                project,
                depth == 0
                    ? null
                    : depth
            );

        WriteTree(tree);

        return ExitCodes.Success;
    }

    private static void WriteTree(
        ProjectTreeEntry root
    )
    {
        Console.WriteLine(
            $"{root.Name}/"
        );

        WriteChildren(
            root.Children,
            prefix: ""
        );
    }

    private static void WriteChildren(
        IReadOnlyList<ProjectTreeEntry> children,
        string prefix
    )
    {
        for (var index = 0;
             index < children.Count;
             index++)
        {
            var child = children[index];
            var isLast = index == children.Count - 1;

            var branch =
                isLast
                    ? "└── "
                    : "├── ";

            var suffix =
                child.Type == ProjectTreeEntryType.Directory
                    ? "/"
                    : "";

            Console.WriteLine(
                $"{prefix}{branch}{child.Name}{suffix}"
            );

            if (child.Type != ProjectTreeEntryType.Directory
                || child.Children.Count == 0)
            {
                continue;
            }

            var childPrefix =
                prefix
                + (isLast
                    ? "    "
                    : "│   ");

            WriteChildren(
                child.Children,
                childPrefix
            );
        }
    }
}
