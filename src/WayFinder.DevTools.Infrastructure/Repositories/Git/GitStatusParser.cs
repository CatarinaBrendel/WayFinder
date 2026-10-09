using WayFinder.DevTools.Application.Repositories.Git;

namespace WayFinder.DevTools.Infrastructure.Repositories.Git;

internal sealed class GitStatusParser
{
    public GitStatus Parse(
        string repositoryName,
        string? branch,
        string output
    )
    {
        ArgumentNullException.ThrowIfNull(output);

        var staged = new List<GitFileChange>();
        var unstaged = new List<GitFileChange>();
        var untracked = new List<string>();
        var conflicts = new List<GitFileChange>();

        var records = output.Split('\0');
        var count = records.Length;

        if (count > 0 && records[^1].Length == 0)
        {
            count--;
        }

        for (var index = 0; index < count; index++)
        {
            var record = records[index];

            if (record.Length < 4 || record[2] != ' ')
            {
                throw new FormatException(
                    "Invalid Git porcelain status record."
                );
            }

            var indexStatus = record[0];
            var workTreeStatus = record[1];
            var path = record[3..];

            if (indexStatus == '?' && workTreeStatus == '?')
            {
                untracked.Add(path);
                continue;
            }

            if (indexStatus == '!' && workTreeStatus == '!')
            {
                continue;
            }

            if (IsConflict(indexStatus, workTreeStatus))
            {
                conflicts.Add(
                    new GitFileChange(
                        path,
                        GitChangeKind.Unmerged
                    )
                );

                continue;
            }

            string? originalPath = null;

            if (indexStatus is 'R' or 'C'
                || workTreeStatus is 'R' or 'C')
            {
                if (++index >= count)
                {
                    throw new FormatException(
                        "Missing original path in Git rename/copy record."
                    );
                }

                originalPath = records[index];
            }

            if (indexStatus != ' ')
            {
                staged.Add(
                    new GitFileChange(
                        path,
                        MapChangeKind(indexStatus),
                        originalPath
                    )
                );
            }

            if (workTreeStatus != ' ')
            {
                unstaged.Add(
                    new GitFileChange(
                        path,
                        MapChangeKind(workTreeStatus),
                        originalPath
                    )
                );
            }
        }

        return new GitStatus(
            repositoryName,
            branch,
            staged.OrderBy(
                change => change.Path,
                StringComparer.Ordinal
            ).ToArray(),
            unstaged.OrderBy(
                change => change.Path,
                StringComparer.Ordinal
            ).ToArray(),
            untracked.OrderBy(
                path => path,
                StringComparer.Ordinal
            ).ToArray(),
            conflicts.OrderBy(
                change => change.Path,
                StringComparer.Ordinal
            ).ToArray()
        );
    }

    private static bool IsConflict(
        char indexStatus,
        char workTreeStatus
    )
    {
        return indexStatus == 'U'
            || workTreeStatus == 'U'
            || (indexStatus == 'A' && workTreeStatus == 'A')
            || (indexStatus == 'D' && workTreeStatus == 'D');
    }

    private static GitChangeKind MapChangeKind(
        char status
    )
    {
        return status switch
        {
            'A' => GitChangeKind.Added,
            'M' => GitChangeKind.Modified,
            'D' => GitChangeKind.Deleted,
            'R' => GitChangeKind.Renamed,
            'C' => GitChangeKind.Copied,
            'T' => GitChangeKind.TypeChanged,

            _ => throw new FormatException(
                $"Unsupported Git status code '{status}'."
            )
        };
    }
}
