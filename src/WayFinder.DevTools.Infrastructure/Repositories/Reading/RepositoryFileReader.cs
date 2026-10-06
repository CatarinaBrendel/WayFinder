using System.Text;
using WayFinder.DevTools.Application.Projects;
using WayFinder.DevTools.Application.Projects.Files;
using WayFinder.DevTools.Application.Repositories.Reading;

namespace WayFinder.DevTools.Infrastructure.Repositories.Reading;

public sealed class RepositoryFileReader(
    IProjectFileSystem fileSystem
) : IRepositoryFileReader
{
    private const int MaximumBytes = 64 * 1024;

    private static readonly UTF8Encoding StrictUtf8 =
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true
        );

    public RepositoryFileContent Read(
        ProjectContext project,
        string relativePath
    )
    {
        var result = fileSystem.Read(
            project,
            relativePath,
            MaximumBytes
        );

        if (ContainsNullByte(result.Content))
        {
            throw new BinaryFileNotSupportedException(
                relativePath
            );
        }

        string content;

        try
        {
            content = DecodeUtf8(
                result.Content,
                result.Truncated
            );

            content = RemoveUtf8Bom(content);
        }
        catch (DecoderFallbackException)
        {
            throw new BinaryFileNotSupportedException(
                relativePath
            );
        }

        return new RepositoryFileContent(
            Path: relativePath,
            Content: content,
            TotalBytes: result.TotalBytes,
            Truncated: result.Truncated
        );
    }

    private static string DecodeUtf8(
        byte[] bytes,
        bool truncated
    )
    {
        if (!truncated)
        {
            return StrictUtf8.GetString(bytes);
        }

        var decoder = StrictUtf8.GetDecoder();

        var characters = new char[
            StrictUtf8.GetMaxCharCount(bytes.Length)
        ];

        decoder.Convert(
            bytes,
            characters,
            flush: false,
            out var bytesUsed,
            out var charactersUsed,
            out _
        );

        if (bytesUsed != bytes.Length)
        {
            throw new InvalidDataException(
                "Could not process the complete repository read buffer."
            );
        }

        return new string(
            characters,
            0,
            charactersUsed
        );
    }

    private static bool ContainsNullByte(
        ReadOnlySpan<byte> content
    )
    {
        return content.Contains((byte)0);
    }

    private static string RemoveUtf8Bom(
        string content
    )
    {
        return content.Length > 0
            && content[0] == '\uFEFF'
                ? content[1..]
                : content;
    }

}
