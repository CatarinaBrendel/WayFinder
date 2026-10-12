using WayFinder.DevTools.Application.Projects.Tree;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Interactive.Commands;

public sealed class TreeInteractiveCommand : IInteractiveCommand, IInteractiveCommandCompletion
{
    private const int DefaultDepth = 3;

    private readonly WayFinderServices _services;

    public string Name => "/tree";

    public string Description => "Show the current project structure";

    public TreeInteractiveCommand(WayFinderServices services)
    {
        _services = services;
    }

    public IReadOnlyList<string> Subcommands { get; } = [];

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Options { get; } =
        new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase)
        {
            [""] = ["--depth"]
        };

    public Task<InteractiveCommandResult> ExecuteAsync(
    IReadOnlyList<string> arguments,
    TextWriter output,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var depth = DefaultDepth;

        if (arguments.Count > 0)
        {
            if (arguments.Count != 2 ||
                arguments[0] != "--depth" ||
                !int.TryParse(arguments[1], out depth))
            {
                ConsolePresenter.Info(
                    output,
                    "Usage: /tree [--depth <number>]"
                );

                return Task.FromResult(InteractiveCommandResult.Continue);
            }
        }

        if (depth < 0)
        {
            ConsolePresenter.Error(
                output,
                "Depth must be zero or greater."
            );

            return Task.FromResult(InteractiveCommandResult.Continue);
        }

        var project = _services.ProjectLocator.Locate(
            Environment.CurrentDirectory
        );

        if (project is null)
        {
            ConsolePresenter.Error(
                output,
                "No project found."
            );

            return Task.FromResult(InteractiveCommandResult.Continue);
        }

        var tree = _services.ProjectTreeReader.Read(
            project,
            depth == 0 ? null : depth
        );

        ConsolePresenter.Header(output, "Project structure");

        ConsolePresenter.Property(
            output,
            "Project",
            project.Name
        );

        ConsolePresenter.Property(
            output,
            "Depth",
            depth == 0 ? "Unlimited" : depth.ToString()
        );

        ConsolePresenter.Subsection(output, "Files and directories");

        WriteTree(tree, output);

        return Task.FromResult(InteractiveCommandResult.Continue);
    }

    private static void WriteTree(
        ProjectTreeEntry root,
        TextWriter output)
    {
        ConsolePresenter.TreeLine(
            output,
            $"{root.Name}/"
        );

        WriteChildren(
            root.Children,
            prefix: "",
            output
        );
    }

    private static void WriteChildren(
        IReadOnlyList<ProjectTreeEntry> children,
        string prefix,
        TextWriter output)
    {
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            var isLast = index == children.Count - 1;

            var branch = isLast ? "└── " : "├── ";

            var suffix =
                child.Type == ProjectTreeEntryType.Directory
                    ? "/"
                    : "";

            ConsolePresenter.TreeLine(
                output,
                $"{prefix}{branch}{child.Name}{suffix}"
            );

            if (child.Type != ProjectTreeEntryType.Directory ||
                child.Children.Count == 0)
            {
                continue;
            }

            var childPrefix =
                prefix + (isLast ? "    " : "│   ");

            WriteChildren(
                child.Children,
                childPrefix,
                output
            );
        }
    }
}
