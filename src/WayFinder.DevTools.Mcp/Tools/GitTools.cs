using System.ComponentModel;
using ModelContextProtocol.Server;
using WayFinder.DevTools.Application.Repositories.Git;
using WayFinder.DevTools.Infrastructure.Composition;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.Logging;

namespace WayFinder.DevTools.Mcp.Tools;

[McpServerToolType]
public sealed class GitTools
{
    private readonly WayFinderServices _services;
    private readonly ILogger<GitTools> _logger;

    public GitTools(
        WayFinderServices services,
        ILogger<GitTools> logger)
    {
        _services = services;
        _logger = logger;
    }

    [McpServerTool(Name = "git_status")]
    [Description(
        "Returns the Git working-tree status of a registered WayFinder project. "
        + "Includes the current branch, staged changes, unstaged changes, "
        + "untracked files, and merge conflicts. "
        + "This operation is read-only and does not modify the repository. "
        + "Use git_diff for detailed changes when necessary."
    )]
    public GitStatus Status(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project
    )
    {
        var projectContext =
            _services.RegisteredProjectResolver.Resolve(project);

        return _services.GitStatusReader.Read(projectContext);
    }

    [McpServerTool(Name = "git_diff")]
    [Description(
        "Returns a bounded Git diff for a registered WayFinder project. "
        + "By default, shows unstaged changes. "
        + "Set staged=true to inspect staged changes. "
        + "Set statOnly=true to retrieve a compact change summary without patch content. "
        + "Use path to restrict the diff to a repository-relative literal path. "
        + "The operation is read-only. "
        + "Prefer statOnly=true when detailed changes are unnecessary."
    )]
    public CallToolResult Diff(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

        [Description(
            "Whether to inspect staged changes instead of unstaged changes."
        )]
        bool staged = false,

        [Description(
            "Return diff statistics instead of patch content. "
            + "Recommended for initial inspection to minimize token usage."
        )]
        bool statOnly = false,

        [Description(
            "Optional repository-relative literal path. "
            + "Absolute paths, traversal outside the repository, "
            + "wildcards, and Git pathspec magic are not allowed."
        )]
        string? path = null,

        [Description(
            "Maximum number of UTF-8 output bytes. "
            + "Defaults to 16384 (16 KiB). "
            + "Must be between 1 and 65536."
        )]
        int maxOutputBytes = 16_384
    )
    {
        return McpToolExecutor.Execute(() =>
        {
            if (maxOutputBytes is < 1 or > 65_536)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxOutputBytes),
                    "Maximum output size must be between 1 and 65536.");
            }

            var projectContext =
                _services.RegisteredProjectResolver.Resolve(project);

            var request = new GitDiffRequest(
                Staged: staged,
                StatOnly: statOnly,
                Path: path,
                MaxOutputBytes: maxOutputBytes
            );

            return _services.GitDiffReader.Read(
                projectContext,
                request
            );
        }, _logger);
    }

    [McpServerTool(Name = "git_log")]
    [Description(
        "Returns recent Git commits for a registered WayFinder project. "
        + "The operation is read-only and does not modify the repository. "
        + "Use count to limit the number of commits returned. "
        + "Use path to restrict history to a repository-relative literal path. "
        + "Prefer a small count to minimize token usage."
    )]
    public CallToolResult Log(
        [Description(
            "The ID or exact name of a project registered for AI access in WayFinder."
        )]
        string project,

        [Description(
            "Maximum number of commits to return. Defaults to 5. Must be between 1 and 100."
        )]
        int count = 5,

        [Description(
            "Optional repository-relative literal path. "
            + "Absolute paths, traversal outside the repository, "
            + "wildcards, and Git pathspec magic are not allowed."
        )]
        string? path = null
    )
    {
        return McpToolExecutor.Execute(() =>
        {
            if (count is < 1 or > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "Commit count must be between 1 and 100."
                );
            }

            var projectContext =
                _services.RegisteredProjectResolver.Resolve(project);

            var request = new GitLogRequest(
                Count: count,
                Path: path
            );

            return _services.GitLogReader.Read(
                projectContext,
                request
            );
        }, _logger);
    }
}
