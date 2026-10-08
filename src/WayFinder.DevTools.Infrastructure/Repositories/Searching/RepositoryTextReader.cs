using System.Text;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;

namespace WayFinder.DevTools.Infrastructure.Repositories.Searching;

internal sealed class RepositoryTextReader(
    IProjectFileSystem fileSystem
)
{
    private const int MaximumFileBytes =
        1024 * 1024;

    private static readonly UTF8Encoding StrictUtf8 =
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true
        );

    public bool TryRead(
        ProjectContext project,
        string relativePath,
        out string content
    )
    {
        var read =
            fileSystem.Read(
                project,
                relativePath,
                MaximumFileBytes
            );

        if (read.Truncated)
        {
            content = string.Empty;
            return false;
        }

        return TryDecodeText(
            read.Content,
            out content
        );
    }

    private static bool TryDecodeText(
        byte[] content,
        out string text
    )
    {
        text = string.Empty;

        if (content.AsSpan().Contains((byte)0))
        {
            return false;
        }

        try
        {
            text =
                StrictUtf8.GetString(content);

            if (text.Length > 0
                && text[0] == '\uFEFF')
            {
                text = text[1..];
            }

            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
