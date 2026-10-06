using System.CommandLine;
using WayFinder.DevTools.Cli.Presentation;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands;

internal static class CompletionCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var command =
            new Command(
                "completion",
                "Manage shell completion"
            );

        command.Subcommands.Add(
            CreateZshCommand()
        );

        command.Subcommands.Add(
            CreateInstallCommand(services)
        );

        return command;
    }

    private static Command CreateZshCommand()
    {
        var command =
            new Command(
                "zsh",
                "Generate zsh completion"
            );

        command.SetAction(
            _ =>
            {
                Console.WriteLine(
                    ZshCompletion
                );

                return ExitCodes.Success;
            }
        );

        return command;
    }

    private static Command CreateInstallCommand(
        WayFinderServices services
    )
    {
        var command =
            new Command(
                "install",
                "Install shell completion"
            );

        command.SetAction(
            _ => Install(
                services
            )
        );

        return command;
    }

    private static int Install(
        WayFinderServices services
    )
    {
        var completionDirectory =
            Path.Combine(
                services.Environment.HomePath,
                "completions"
            );

        var completionPath =
            Path.Combine(
                completionDirectory,
                "_wayfinder"
            );

        try
        {
            Directory.CreateDirectory(
                completionDirectory
            );

            File.WriteAllText(
                completionPath,
                ZshCompletion + Environment.NewLine
            );
        }
        catch (
            Exception exception
        ) when (
            exception is IOException
            or UnauthorizedAccessException
        )
        {
            Console.Error.WriteLine(
                $"{ConsoleTheme.ErrorMark} Could not install completion: {exception.Message}"
            );

            return ExitCodes.Failure;
        }

        Console.WriteLine(
            $"{ConsoleTheme.Waypoint} {ConsoleTheme.Bold("Shell completion")}"
        );

        Console.WriteLine(
            ConsoleTheme.Rule
        );

        Console.WriteLine();

        Console.WriteLine(
            $"{ConsoleTheme.SuccessMark} Installed zsh completion"
        );

        Console.WriteLine(
            $"  {completionPath}"
        );

        Console.WriteLine();

        Console.WriteLine(
            "Add this to ~/.zshrc before compinit:"
        );

        Console.WriteLine();

        Console.WriteLine(
            "  fpath=(~/.wayfinder/completions $fpath)"
        );

        Console.WriteLine();

        Console.WriteLine(
            "Then restart your shell."
        );

        return ExitCodes.Success;
    }

    private const string ZshCompletion =
        """
        #compdef wayfinder

        _wayfinder()
        {
            local -a commands

            commands=(
                'project:Manage WayFinder projects'
                'repo:Inspect repository content'
                'doctor:Check the health of your WayFinder setup'
                'completion:Manage shell completion'
            )

            if (( CURRENT == 2 )); then
                _describe 'command' commands
                return
            fi

            case $words[2] in
                project)
                    _wayfinder_project
                    ;;

                repo)
                    _wayfinder_repo
                    ;;

                completion)
                    _wayfinder_completion
                    ;;
            esac
        }

        _wayfinder_project()
        {
            local -a commands

            commands=(
                'add:Register a project with WayFinder'
                'info:Show information about the current project'
                'init:Create WayFinder metadata for the current project'
                'list:List projects registered with WayFinder'
                'remove:Remove a project from the WayFinder registry'
            )

            if (( CURRENT == 3 )); then
                _describe 'project command' commands
                return
            fi

            case $words[3] in
                add)
                    _files -/
                    ;;

                remove)
                    _message 'project ID'
                    ;;
            esac
        }

        _wayfinder_repo()
        {
            local -a commands

            commands=(
                'read:Read a text file from the current repository'
                'search:Search text files in the current repository'
            )

            if (( CURRENT == 3 )); then
                _describe 'repository command' commands
                return
            fi

            case $words[3] in
                read)
                    _files
                    ;;

                search)
                    _message 'search query'
                    ;;
            esac
        }

        _wayfinder_completion()
        {
            local -a commands

            commands=(
                'zsh:Generate zsh completion'
                'install:Install shell completion'
            )

            if (( CURRENT == 3 )); then
                _describe 'completion command' commands
            fi
        }

        compdef _wayfinder wayfinder
        """;
}
