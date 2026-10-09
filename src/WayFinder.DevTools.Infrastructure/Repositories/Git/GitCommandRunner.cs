using System.Diagnostics;
using System.Text;

namespace WayFinder.DevTools.Infrastructure.Repositories.Git;

internal sealed class GitCommandRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private const int MaxLogOutputBytes = 65_536; // 64 KB

    public string GetStatus(string repositoryPath)
    {
        return Run(
            repositoryPath,
            [
                "--no-optional-locks",
                "status",
                "--porcelain=v1",
                "-z",
                "--untracked-files=all",
            ]
        ).Output;
    }

    public string? GetBranch(string repositoryPath)
    {
        var result = Run(
            repositoryPath,
            [
                "--no-optional-locks",
                "symbolic-ref",
                "--quiet",
                "--short",
                "HEAD",
            ],
            permittedExitCodes: new HashSet<int> { 0, 1 }
        );

        return result.ExitCode == 0
            ? result.Output.TrimEnd('\r', '\n')
            : null;
    }

    public GitDiffCommandResult GetDiff(
        string repositoryPath,
        bool staged,
        bool statOnly,
        string? path,
        int maxOutputBytes
    )
    {
        if (maxOutputBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxOutputBytes)
            );
        }

        var arguments = new List<string>
    {
        "--no-optional-locks",
        "-c", "color.ui=false",
        "-c", "diff.external=",
        "diff",
        "--no-ext-diff",
        "--no-textconv",
        "--no-color"
    };

        if (staged)
        {
            arguments.Add("--cached");
        }

        if (statOnly)
        {
            arguments.Add("--stat");
        }

        if (path is not null)
        {
            arguments.Add("--");
            arguments.Add($":(literal){path}");
        }

        return RunBoundedDiff(
            repositoryPath,
            arguments,
            maxOutputBytes
        );
    }
    public string GetLog(
        string repositoryPath,
        int count,
        string? path
    )
    {
        if (count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                "Commit count must be between 1 and 100."
            );
        }

        if (!HasCommits(repositoryPath))
        {
            return string.Empty;
        }

        var arguments = new List<string>
        {
            "--no-optional-locks",
            "-c", "log.showSignature=false",
            "log",
            "--no-show-signature",
            "-n", count.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            ),
            "--format=%H%x00%an%x00%ae%x00%aI%x00%s%x00"
        };

        if (path is not null)
        {
            arguments.Add("--");
            arguments.Add($":(literal){path}");
        }

        return RunBoundedLog(
            repositoryPath,
            arguments,
            MaxLogOutputBytes
        );
    }

    public bool HasCommits(string repositoryPath)
    {
        var result = Run(
            repositoryPath,
            [
                "--no-optional-locks",
            "rev-parse",
            "--verify",
            "--quiet",
            "HEAD^{commit}"
            ],
            permittedExitCodes: new HashSet<int> { 0, 1 }
        );

        return result.ExitCode == 0;
    }

    private static GitCommandResult Run(
        string repositoryPath,
        IReadOnlyList<string> arguments,
        IReadOnlySet<int>? permittedExitCodes = null
    )
    {
        var startInfo = CreateStartInfo(
            repositoryPath,
            arguments
        );

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        using var cancellation = new CancellationTokenSource(
            Timeout
        );

        try
        {
            var outputTask =
                process.StandardOutput.ReadToEndAsync(
                    cancellation.Token
                );

            var errorTask =
                process.StandardError.ReadToEndAsync(
                    cancellation.Token
                );

            process.WaitForExitAsync(
                cancellation.Token
            ).GetAwaiter().GetResult();

            var output =
                outputTask.GetAwaiter().GetResult();

            var error =
                errorTask.GetAwaiter().GetResult();

            var allowed =
                permittedExitCodes?.Contains(process.ExitCode)
                ?? process.ExitCode == 0;

            if (!allowed)
            {
                throw new InvalidOperationException(
                    $"Git command failed (exit code {process.ExitCode}): {error.Trim()}"
                );
            }

            return new GitCommandResult(
                process.ExitCode,
                output
            );
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            TerminateProcess(process);

            throw new TimeoutException(
                $"Git command exceeded the {Timeout.TotalSeconds:0}-second timeout."
            );
        }
    }

    private sealed record GitCommandResult(
        int ExitCode,
        string Output
    );

    private static GitDiffCommandResult RunBoundedDiff(
        string repositoryPath,
        IReadOnlyList<string> arguments,
        int maxOutputBytes
    )
    {
        var startInfo = CreateStartInfo(
            repositoryPath,
            arguments
        );

        using var process = new Process { StartInfo = startInfo };

        process.Start();

        using var cancellation = new CancellationTokenSource(Timeout);

        try
        {
            var errorTask = process.StandardError.ReadToEndAsync(
                cancellation.Token
            );

            using var buffer = new MemoryStream();

            var chunk = new byte[8192];

            var truncated = ReadBoundedOutput(
                process,
                buffer,
                maxOutputBytes,
                cancellation.Token
            );

            if (truncated)
            {
                TerminateProcess(process);
            }

            process.WaitForExitAsync(cancellation.Token)
                .GetAwaiter()
                .GetResult();

            var error = errorTask.GetAwaiter().GetResult();

            if (!truncated && process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Git diff failed (exit code {process.ExitCode}): {error.Trim()}"
                );
            }

            var bytes = buffer.ToArray();

            var utf8 = new UTF8Encoding(false, true);

            for (var trim = 0; trim <= Math.Min(3, bytes.Length); trim++)
            {
                try
                {
                    var content = utf8.GetString(
                        bytes.AsSpan(0, bytes.Length - trim)
                    );

                    return new GitDiffCommandResult(
                        content,
                        truncated || trim > 0
                    );
                }
                catch (DecoderFallbackException) when (
                    truncated && trim < Math.Min(3, bytes.Length)
                )
                {
                    // The byte limit may have split a UTF-8 character.
                }
            }

            throw new FormatException(
                "Git diff output contains invalid UTF-8."
            );
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            TerminateProcess(process);

            throw new TimeoutException(
                $"Git diff exceeded the {Timeout.TotalSeconds:0}-second timeout."
            );
        }
    }

    private static string RunBoundedLog(
        string repositoryPath,
        IReadOnlyList<string> arguments,
        int maxOutputBytes
    )
    {
        var startInfo = CreateStartInfo(
            repositoryPath,
            arguments
        );

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        using var cancellation = new CancellationTokenSource(Timeout);

        try
        {
            var errorTask = process.StandardError.ReadToEndAsync(
                cancellation.Token
            );

            using var buffer = new MemoryStream();

            var exceededLimit = ReadBoundedOutput(
                process,
                buffer,
                maxOutputBytes,
                cancellation.Token
            );

            if (exceededLimit)
            {
                TerminateProcess(process);

                throw new InvalidOperationException(
                    $"Git log output exceeds the {maxOutputBytes:N0}-byte limit. " +
                    "Reduce --count or use --path to narrow the history."
                );
            }

            process.WaitForExitAsync(cancellation.Token)
                .GetAwaiter()
                .GetResult();

            var error = errorTask.GetAwaiter().GetResult();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Git log failed (exit code {process.ExitCode}): {error.Trim()}"
                );
            }

            return new UTF8Encoding(false, true).GetString(
                buffer.ToArray()
            );
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            TerminateProcess(process);

            throw new TimeoutException(
                $"Git log exceeded the {Timeout.TotalSeconds:0}-second timeout."
            );
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        string repositoryPath,
        IReadOnlyList<string> arguments
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repositoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = new UTF8Encoding(false, true),
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static void TerminateProcess(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
    }

    private static bool ReadBoundedOutput(
        Process process,
        MemoryStream buffer,
        int maxOutputBytes,
        CancellationToken cancellationToken
    )
    {
        var chunk = new byte[8192];

        while (true)
        {
            var read = process.StandardOutput.BaseStream
                .ReadAsync(chunk, cancellationToken)
                .GetAwaiter()
                .GetResult();

            if (read == 0)
            {
                return false;
            }

            var remaining = maxOutputBytes - (int)buffer.Length;

            if (read > remaining)
            {
                if (remaining > 0)
                {
                    buffer.Write(chunk, 0, remaining);
                }

                return true;
            }

            buffer.Write(chunk, 0, read);
        }
    }
}
