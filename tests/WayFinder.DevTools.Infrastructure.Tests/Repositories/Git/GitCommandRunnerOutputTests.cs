using System.Diagnostics;
using System.Text;
using WayFinder.DevTools.Infrastructure.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Tests.Repositories.Git;

public sealed class GitCommandRunnerOutputTests
{
    private const int MaxOutputBytes = 4 * 1024;

    [Fact]
    public void ReadBoundedOutput_BelowLimit_ReturnsCompleteOutput()
    {
        var input = Encoding.UTF8.GetBytes("main\n");

        using var stream = new MemoryStream(input);
        using var buffer = new MemoryStream();

        var exceededLimit = GitCommandRunner.ReadBoundedOutput(
            stream,
            buffer,
            MaxOutputBytes,
            CancellationToken.None
        );

        Assert.False(exceededLimit);
        Assert.Equal(input, buffer.ToArray());
    }

    [Fact]
    public void ReadBoundedOutput_ExactlyAtLimit_ReturnsCompleteOutput()
    {
        var input = new byte[MaxOutputBytes];
        Array.Fill(input, (byte)'a');

        using var stream = new MemoryStream(input);
        using var buffer = new MemoryStream();

        var exceededLimit = GitCommandRunner.ReadBoundedOutput(
            stream,
            buffer,
            MaxOutputBytes,
            CancellationToken.None
        );

        Assert.False(exceededLimit);
        Assert.Equal(MaxOutputBytes, buffer.Length);
        Assert.Equal(input, buffer.ToArray());
    }

    [Fact]
    public void ReadBoundedOutput_ExceedingLimit_ReportsOverflow()
    {
        var input = new byte[MaxOutputBytes + 1];
        Array.Fill(input, (byte)'a');

        using var stream = new MemoryStream(input);
        using var buffer = new MemoryStream();

        var exceededLimit = GitCommandRunner.ReadBoundedOutput(
            stream,
            buffer,
            MaxOutputBytes,
            CancellationToken.None
        );

        Assert.True(exceededLimit);
        Assert.Equal(MaxOutputBytes, buffer.Length);
        Assert.Equal(input[..MaxOutputBytes], buffer.ToArray());
    }

    [Fact]
    public void ReadBoundedOutput_UnicodeOutput_EnforcesByteLimit()
    {
        // Each Japanese character occupies three UTF-8 bytes.
        var input = Encoding.UTF8.GetBytes(
            new string('日', 2_000)
        );

        Assert.True(input.Length > MaxOutputBytes);

        using var stream = new MemoryStream(input);
        using var buffer = new MemoryStream();

        var exceededLimit = GitCommandRunner.ReadBoundedOutput(
            stream,
            buffer,
            MaxOutputBytes,
            CancellationToken.None
        );

        Assert.True(exceededLimit);
        Assert.Equal(MaxOutputBytes, buffer.Length);
    }

    [Fact]
    public async Task ReadBoundedOutputAsync_WhenExactlyAtErrorLimit_DoesNotOverflow()
    {
        const int limit = 16 * 1024;

        var data = new byte[limit];

        using var stream = new MemoryStream(data);
        using var buffer = new MemoryStream();

        var exceeded = await GitCommandRunner.ReadBoundedOutputAsync(
            stream,
            buffer,
            limit,
            CancellationToken.None
        );

        Assert.False(exceeded);
        Assert.Equal(limit, buffer.Length);
    }

    [Fact]
    public async Task ReadBoundedOutputAsync_WhenErrorLimitExceeded_StopsAtLimit()
    {
        const int limit = 16 * 1024;

        var data = new byte[limit + 1];

        using var stream = new MemoryStream(data);
        using var buffer = new MemoryStream();

        var exceeded = await GitCommandRunner.ReadBoundedOutputAsync(
            stream,
            buffer,
            limit,
            CancellationToken.None
        );

        Assert.True(exceeded);
        Assert.Equal(limit, buffer.Length);
    }

    [Fact]
    public async Task TerminateAndObserveReaders_StopsProcessAndCompletesReaders()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                ArgumentList = { "hash-object", "--stdin" },
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        Assert.True(process.Start());

        using var outputBuffer = new MemoryStream();
        using var errorBuffer = new MemoryStream();

        using var cancellation = new CancellationTokenSource(
            TimeSpan.FromSeconds(10)
        );

        var outputTask = GitCommandRunner.ReadBoundedOutputAsync(
            process.StandardOutput.BaseStream,
            outputBuffer,
            4096,
            cancellation.Token
        );

        var errorTask = GitCommandRunner.ReadBoundedOutputAsync(
            process.StandardError.BaseStream,
            errorBuffer,
            4096,
            cancellation.Token
        );

        GitCommandRunner.TerminateAndObserveReaders(
            process,
            outputTask,
            errorTask
        );

        Assert.True(process.HasExited);
        Assert.True(outputTask.IsCompleted);
        Assert.True(errorTask.IsCompleted);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ReadBoundedOutputAsync_WhenGitStderrExceedsLimit_DetectsOverflow()
    {
        const int limit = 16 * 1024;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        // Generate a Git error containing more than 16 KiB.
        process.StartInfo.ArgumentList.Add(
            "--invalid-option-" + new string('x', limit + 1024)
        );

        Assert.True(process.Start());

        using var errorBuffer = new MemoryStream();
        using var cancellation = new CancellationTokenSource(
            TimeSpan.FromSeconds(10)
        );

        var exceeded = await GitCommandRunner.ReadBoundedOutputAsync(
            process.StandardError.BaseStream,
            errorBuffer,
            limit,
            cancellation.Token
        );

        Assert.True(exceeded);
        Assert.Equal(limit, errorBuffer.Length);

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync();
    }
}
