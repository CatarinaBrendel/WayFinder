using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using WayFinder.DevTools.Application.Projects.Registration;

namespace WayFinder.DevTools.Mcp.Tools;

internal static class McpToolExecutor
{
    public static CallToolResult Execute<T>(
        Func<T> operation,
        ILogger logger)
    {
        try
        {
            var result = operation();

            return new CallToolResult
            {
                IsError = false,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = JsonSerializer.Serialize(
                            result,
                            JsonSerializerOptions.Web)
                    }
                ]
            };
        }
        catch (RegisteredProjectNotFoundException)
        {
            return Error("Registered project was not found.");
        }
        catch (RegisteredProjectReferenceNotFoundException)
        {
            return Error("Registered project was not found.");
        }
        catch (RegisteredProjectAmbiguousException)
        {
            return Error(
                "Project name is ambiguous. Use the registered project ID.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Error($"Invalid argument: {ex.Message}");
        }
        catch (ArgumentException ex) when (ex.ParamName == "path")
        {
            return Error(
                "Invalid Git path. Provide a repository-relative literal path " +
                "that remains inside the repository. " +
                "Absolute paths, wildcards, and Git pathspec magic are not allowed."
            );
        }
        catch (ArgumentException)
        {
            return Error("Invalid argument supplied to the tool.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error while executing an MCP tool.");

            return Error(
                "An internal error occurred while executing the tool.");
        }
    }

    public static CallToolResult Error(string message)
    {
        return new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = message
                }
            ]
        };
    }
}
