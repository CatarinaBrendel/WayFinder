using System.Diagnostics;
using System.Text;
using WayFinder.DevTools.Application.Conversations;

namespace WayFinder.DevTools.Infrastructure.Conversations;

public sealed class CopilotProvider : IAiProvider
{
    public string Id => "copilot";
    private bool started;

    public async Task<ConversationResponse> CompleteAsync(
        ConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prompt = BuildPrompt(request);

        var usageFile = Path.Combine(
            Path.GetTempPath(),
            $"wayfinder-usage-{Guid.NewGuid():N}.json"
        );

        var startInfo = new ProcessStartInfo
        {
            FileName = "copilot",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in new[]
        {
            "-p", prompt,
            "-s",
            "--stream", "off",
            "--no-custom-instructions",
            "--disable-builtin-mcps",
            "--no-auto-update",
            "--available-tools",
            "--usage-output-file", usageFile
        })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "Could not start GitHub Copilot CLI.");
            }
            started = true;

            var outputTask = process.StandardOutput.ReadToEndAsync(
                cancellationToken);

            var errorTask = process.StandardError.ReadToEndAsync(
                cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Copilot CLI failed (exit code {process.ExitCode}): {error.Trim()}");
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                throw new InvalidOperationException(
                    "Copilot CLI returned an empty response.");
            }

            if (!File.Exists(usageFile))
            {
                return new ConversationResponse(output.Trim());
            }

            var json = await File.ReadAllTextAsync(
                usageFile,
                cancellationToken
            );

            return CopilotUsageParser.Parse(
                json,
                output.Trim()
            );
        }
        catch
        {
            if (started && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(
                    CancellationToken.None);
            }

            throw;
        }
        finally
        {
            if (File.Exists(usageFile))
            {
                File.Delete(usageFile);
            }
        }
    }

    private static string BuildPrompt(ConversationRequest request)
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            "Continue the following conversation. " +
            "Respond only to the latest user message.");

        builder.AppendLine();

        foreach (var message in request.Messages)
        {
            var role = message.Role switch
            {
                ConversationRole.System => "System",
                ConversationRole.User => "User",
                ConversationRole.Assistant => "Assistant",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(message.Role))
            };

            builder.AppendLine($"[{role}]");
            builder.AppendLine(message.Content);
            builder.AppendLine();
        }

        builder.AppendLine("[Assistant]");

        return builder.ToString();
    }
}
