using System.CommandLine;
using WayFinder.DevTools.Application.Repositories.Reading;
using WayFinder.DevTools.Infrastructure.Composition;

namespace WayFinder.DevTools.Cli.Commands.Repositories;

public static class RepositoryReadCommand
{
    public static Command Create(
        WayFinderServices services
    )
    {
        var pathArgument =
            new Argument<string>("path")
            {
                Description =
                    "Project-relative path to the file to read",
                Arity = ArgumentArity.ExactlyOne,
            };

        var command =
            new Command(
                "read",
                "Read a text file from the current repository"
            );

        command.Arguments.Add(
            pathArgument
        );

        command.SetAction(
            parseResult =>
            {
                var path =
                    parseResult.GetValue(
                        pathArgument
                    );

                return Execute(
                    services,
                    path
                );
            }
        );

        return command;
    }

    private static int Execute(
    WayFinderServices services,
    string? path
)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine(
                "A file path is required."
            );

            return ExitCodes.Failure;
        }

        var project =
            services.ProjectLocator.Locate(
                Directory.GetCurrentDirectory()
            );

        if (project is null)
        {
            Console.Error.WriteLine(
                "No Git project found."
            );

            return ExitCodes.Failure;
        }

        try
        {
            var result =
                services.RepositoryFileReader.Read(
                    project,
                    path
                );

            Console.Write(
                result.Content
            );

            if (result.Truncated)
            {
                if (!result.Content.EndsWith('\n'))
                {
                    Console.WriteLine();
                }

                Console.Error.WriteLine(
                    $"[truncated: {result.TotalBytes} bytes total]"
                );
            }

            return ExitCodes.Success;
        }
        catch (BinaryFileNotSupportedException exception)
        {
            Console.Error.WriteLine(
                exception.Message
            );

            return ExitCodes.Failure;
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine(
                $"File not found: {path}"
            );

            return ExitCodes.Failure;
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.Error.WriteLine(
                exception.Message
            );

            return ExitCodes.Failure;
        }
    }
}
