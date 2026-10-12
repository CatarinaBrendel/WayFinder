using System.Diagnostics;
using System.Text;

namespace WayFinder.DevTools.Infrastructure.Repositories.Git;

internal sealed class GitCommandRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private const int MaxLogOutputBytes = 65_536; // 64 KB

    private const int MaxStatusOutputBytes = 256 * 1024; // 256 KiB

    private const int MaxCommandOutputBytes = 4 * 1024;

    private const int MaxErrorOutputBytes = 16 * 1024;

    private readonly IReadOnlyDictionary<string, string>? _environmentOverrides;

    public GitCommandRunner(IReadOnlyDictionary<string, string>? environmentOverrides = null)
    {
        _environmentOverrides = environmentOverrides;
    }

    public string GetStatus(string repositoryPath)
    {
        return RunBoundedStatus(
            repositoryPath,
            [
                "--no-optional-locks",
                "status",
                "--porcelain=v1",
                "-z",
                "--untracked-files=all",
            ]
        );
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

    private GitCommandResult Run(
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

        Task<bool>? outputTask = null;
        Task<bool>? errorTask = null;

        try
        {
            using var outputBuffer = new MemoryStream();
            using var errorBuffer = new MemoryStream();

            // Start both readers concurrently to prevent pipe deadlocks.
            outputTask = ReadBoundedOutputAsync(
                process.StandardOutput.BaseStream,
                outputBuffer,
                MaxCommandOutputBytes,
                cancellation.Token
            );

            errorTask = ReadBoundedOutputAsync(
                process.StandardError.BaseStream,
                errorBuffer,
                MaxErrorOutputBytes,
                cancellation.Token
            );

            var pending = new List<(Task<bool> Task, string Label)>
            {
                (outputTask, "output"),
                (errorTask, "error output")
            };

            while (pending.Count > 0)
            {
                var completed = Task.WhenAny(
                    pending.Select(item => item.Task)
                ).GetAwaiter().GetResult();

                var exceededLimit = completed.GetAwaiter().GetResult();

                if (exceededLimit)
                {
                    TerminateAndObserveReaders(
                        process,
                        outputTask,
                        errorTask
                    );

                    var label = pending
                        .First(item => ReferenceEquals(item.Task, completed))
                        .Label;

                    throw new InvalidOperationException(
                        label == "output"
                            ? $"Git command output exceeds the {MaxCommandOutputBytes:N0}-byte limit."
                            : $"Git command error output exceeds the {MaxErrorOutputBytes:N0}-byte limit."
                    );
                }

                pending.RemoveAll(
                    item => ReferenceEquals(item.Task, completed)
                );
            }

            process.WaitForExitAsync(
                cancellation.Token
            ).GetAwaiter().GetResult();

            var error = new UTF8Encoding(false, true).GetString(
                errorBuffer.ToArray()
            );

            var allowed =
                permittedExitCodes?.Contains(process.ExitCode)
                ?? process.ExitCode == 0;

            if (!allowed)
            {
                throw new InvalidOperationException(
                    $"Git command failed (exit code {process.ExitCode}): {error.Trim()}"
                );
            }

            var output = new UTF8Encoding(false, true).GetString(
                outputBuffer.ToArray()
            );

            return new GitCommandResult(
                process.ExitCode,
                output
            );
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            if (outputTask is not null && errorTask is not null)
            {
                TerminateAndObserveReaders(
                    process,
                    outputTask,
                    errorTask
                );
            }
            else
            {
                TerminateProcess(process);
            }

            throw new TimeoutException(
                $"Git command exceeded the {Timeout.TotalSeconds:0}-second timeout."
            );
        }
        catch
        {
            if (outputTask is not null && errorTask is not null)
            {
                TerminateAndObserveReaders(
                    process,
                    outputTask,
                    errorTask
                );
            }
            else
            {
                TerminateProcess(process);
            }

            throw;
        }
    }

    private sealed record GitCommandResult(
        int ExitCode,
        string Output
    );

    private GitDiffCommandResult RunBoundedDiff(
        string repositoryPath,
        IReadOnlyList<string> arguments,
        int maxOutputBytes
    )
    {
        var startInfo = CreateStartInfo(repositoryPath, arguments);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        using var cancellation = new CancellationTokenSource(Timeout);

        Task<bool>? outputTask = null;
        Task<bool>? errorTask = null;

        try
        {
            using var outputBuffer = new MemoryStream();
            using var errorBuffer = new MemoryStream();

            outputTask = ReadBoundedOutputAsync(
                process.StandardOutput.BaseStream,
                outputBuffer,
                maxOutputBytes,
                cancellation.Token
            );

            errorTask = ReadBoundedOutputAsync(
                process.StandardError.BaseStream,
                errorBuffer,
                MaxErrorOutputBytes,
                cancellation.Token
            );

            var pending = new List<(Task<bool> Task, string Label)>
            {
                (outputTask, "diff"),
                (errorTask, "error")
            };

            var truncated = false;

            while (pending.Count > 0)
            {
                var completed = Task.WhenAny(
                    pending.Select(item => item.Task)
                ).GetAwaiter().GetResult();

                var exceededLimit = completed.GetAwaiter().GetResult();

                var label = pending
                    .First(item => ReferenceEquals(item.Task, completed))
                    .Label;

                if (exceededLimit)
                {
                    TerminateAndObserveReaders(
                        process,
                        outputTask,
                        errorTask
                    );

                    if (label == "error")
                    {
                        throw new InvalidOperationException(
                            $"Git error output exceeds the {MaxErrorOutputBytes:N0}-byte limit."
                        );
                    }

                    truncated = true;
                    break;
                }

                pending.RemoveAll(
                    item => ReferenceEquals(item.Task, completed)
                );
            }

            if (!truncated)
            {
                process.WaitForExitAsync(cancellation.Token)
                    .GetAwaiter()
                    .GetResult();

                if (process.ExitCode != 0)
                {
                    var error = new UTF8Encoding(false, true).GetString(
                        errorBuffer.ToArray()
                    );

                    throw new InvalidOperationException(
                        $"Git diff failed (exit code {process.ExitCode}): {error.Trim()}"
                    );
                }
            }

            var bytes = outputBuffer.ToArray();
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
                    // The byte limit may split a UTF-8 character.
                }
            }

            throw new FormatException(
                "Git diff output contains invalid UTF-8."
            );
        }
        catch (OperationCanceledException) when (
            cancellation.IsCancellationRequested
        )
        {
            if (outputTask is not null && errorTask is not null)
            {
                TerminateAndObserveReaders(
                    process,
                    outputTask,
                    errorTask
                );
            }
            else
            {
                TerminateProcess(process);
            }

            throw new TimeoutException(
                $"Git diff exceeded the {Timeout.TotalSeconds:0}-second timeout."
            );
        }
    }

    private string RunBoundedLog(
        string repositoryPath,
        IReadOnlyList<string> arguments,
        int maxOutputBytes
    )
    {
        var startInfo = CreateStartInfo(repositoryPath, arguments);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        using var cancellation = new CancellationTokenSource(Timeout);

        Task<bool>? outputTask = null;
        Task<bool>? errorTask = null;

        try
        {
            using var outputBuffer = new MemoryStream();
            using var errorBuffer = new MemoryStream();

            outputTask = ReadBoundedOutputAsync(
                process.StandardOutput.BaseStream,
                outputBuffer,
                maxOutputBytes,
                cancellation.Token
            );

            errorTask = ReadBoundedOutputAsync(
                process.StandardError.BaseStream,
                errorBuffer,
                MaxErrorOutputBytes,
                cancellation.Token
            );

            var pending = new List<(Task<bool> Task, string Label)>
        {
            (outputTask, "log"),
            (errorTask, "error")
        };

            while (pending.Count > 0)
            {
                var completed = Task.WhenAny(
                    pending.Select(item => item.Task)
                ).GetAwaiter().GetResult();

                var exceededLimit = completed.GetAwaiter().GetResult();

                if (exceededLimit)
                {
                    TerminateAndObserveReaders(
                        process,
                        outputTask,
                        errorTask
                    );

                    var label = pending
                        .First(item => ReferenceEquals(item.Task, completed))
                        .Label;

                    throw new InvalidOperationException(
                        label == "log"
                            ? $"Git log output exceeds the {maxOutputBytes:N0}-byte limit. " +
                              "Reduce --count or use --path to narrow the history."
                            : $"Git error output exceeds the {MaxErrorOutputBytes:N0}-byte limit."
                    );
                }

                pending.RemoveAll(
                    item => ReferenceEquals(item.Task, completed)
                );
            }

            process.WaitForExitAsync(cancellation.Token)
                .GetAwaiter()
                .GetResult();

            if (process.ExitCode != 0)
            {
                var error = new UTF8Encoding(false, true).GetString(
                    errorBuffer.ToArray()
                );

                throw new InvalidOperationException(
                    $"Git log failed (exit code {process.ExitCode}): {error.Trim()}"
                );
            }

            return new UTF8Encoding(false, true).GetString(
                outputBuffer.ToArray()
            );
        }
        catch (OperationCanceledException) when (
            cancellation.IsCancellationRequested
        )
        {
            if (outputTask is not null && errorTask is not null)
            {
                TerminateAndObserveReaders(
                    process,
                    outputTask,
                    errorTask
                );
            }
            else
            {
                TerminateProcess(process);
            }

            throw new TimeoutException(
                $"Git log exceeded the {Timeout.TotalSeconds:0}-second timeout."
            );
        }
    }

    private ProcessStartInfo CreateStartInfo(
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
        };

        // Disable repository-configured filesystem monitors.
        //
        // Git configuration is resolved in precedence order, so this
        // command-line setting overrides repository and global configuration.
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("core.fsmonitor=false");

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Apply environment overrides to this child process only.
        if (_environmentOverrides is not null)
        {
            foreach (var (key, value) in _environmentOverrides)
            {
                startInfo.Environment[key] = value;
            }
        }

        // Git subprocesses must not inherit Git-specific environment settings.
        //
        // These variables can override repository locations, configuration,
        // indexes, object databases, executable paths, and other Git behavior.
        //
        // Preserve ordinary environment variables required for cross-platform
        // process execution, but remove all inherited Git-specific settings.
        foreach (var key in startInfo.Environment.Keys
            .Where(key => key.StartsWith(
                "GIT_",
                StringComparison.OrdinalIgnoreCase
            ))
            .ToArray())
        {
            startInfo.Environment.Remove(key);
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

    internal static async Task<bool> ReadBoundedOutputAsync(
        Stream stream,
        MemoryStream buffer,
        int maxOutputBytes,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOutputBytes);

        var chunk = new byte[8192];

        while (true)
        {
            var read = await stream.ReadAsync(
                chunk,
                cancellationToken
            );

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

    internal static bool ReadBoundedOutput(
        Stream stream,
        MemoryStream buffer,
        int maxOutputBytes,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOutputBytes);

        var chunk = new byte[8192];

        while (true)
        {
            var read = stream
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

    private string RunBoundedStatus(
        string repositoryPath,
        IReadOnlyList<string> arguments
    )
    {
        var startInfo = CreateStartInfo(repositoryPath, arguments);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        using var cancellation = new CancellationTokenSource(Timeout);

        Task<bool>? outputTask = null;
        Task<bool>? errorTask = null;

        try
        {
            using var outputBuffer = new MemoryStream();
            using var errorBuffer = new MemoryStream();

            outputTask = ReadBoundedOutputAsync(
                process.StandardOutput.BaseStream,
                outputBuffer,
                MaxStatusOutputBytes,
                cancellation.Token
            );

            errorTask = ReadBoundedOutputAsync(
                process.StandardError.BaseStream,
                errorBuffer,
                MaxErrorOutputBytes,
                cancellation.Token
            );

            var pending = new List<(Task<bool> Task, string Label)>
        {
            (outputTask, "status"),
            (errorTask, "error")
        };

            while (pending.Count > 0)
            {
                var completed = Task.WhenAny(
                    pending.Select(item => item.Task)
                ).GetAwaiter().GetResult();

                var exceededLimit = completed.GetAwaiter().GetResult();

                if (exceededLimit)
                {
                    TerminateAndObserveReaders(
                        process,
                        outputTask,
                        errorTask
                    );

                    var label = pending
                        .First(item => ReferenceEquals(item.Task, completed))
                        .Label;

                    throw new InvalidOperationException(
                        label == "status"
                            ? $"Git status output exceeds the {MaxStatusOutputBytes:N0}-byte limit."
                            : $"Git error output exceeds the {MaxErrorOutputBytes:N0}-byte limit."
                    );
                }

                pending.RemoveAll(
                    item => ReferenceEquals(item.Task, completed)
                );
            }

            process.WaitForExitAsync(cancellation.Token)
                .GetAwaiter()
                .GetResult();

            if (process.ExitCode != 0)
            {
                var error = new UTF8Encoding(false, true).GetString(
                    errorBuffer.ToArray()
                );

                throw new InvalidOperationException(
                    $"Git status failed (exit code {process.ExitCode}): {error.Trim()}"
                );
            }

            return new UTF8Encoding(false, true).GetString(
                outputBuffer.ToArray()
            );
        }
        catch (OperationCanceledException) when (
            cancellation.IsCancellationRequested
        )
        {
            if (outputTask is not null && errorTask is not null)
            {
                TerminateAndObserveReaders(
                    process,
                    outputTask,
                    errorTask
                );
            }
            else
            {
                TerminateProcess(process);
            }

            throw new TimeoutException(
                $"Git status exceeded the {Timeout.TotalSeconds:0}-second timeout."
            );
        }
    }

    internal static void TerminateAndObserveReaders(
        Process process,
        params Task<bool>[] readers
    )
    {
        try
        {
            TerminateProcess(process);
        }
        finally
        {
            foreach (var reader in readers)
            {
                try
                {
                    reader.GetAwaiter().GetResult();
                }
                catch (Exception)
                {
                    // The process has been terminated.
                    // Observe reader failures during cleanup.
                }
            }
        }
    }

}
