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
    private const int RangeBufferBytes = 16 * 1024;
    private const int MaximumRangeLines = 500;

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

    public RepositoryFileRangeContent Read(
        ProjectContext project,
        string relativePath,
        int startLine,
        int lineCount
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            startLine
        );

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            lineCount
        );

        if (lineCount > MaximumRangeLines)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineCount),
                lineCount,
                $"Line count must not exceed {MaximumRangeLines}."
            );
        }

        var decoder =
            StrictUtf8.GetDecoder();

        var content =
            new StringBuilder();

        var offset = 0L;
        var currentLine = 1;
        var returnedLines = 0;
        long totalBytes = 0;

        try
        {
            while (returnedLines < lineCount)
            {
                var result = fileSystem.Read(
                    project,
                    relativePath,
                    offset,
                    RangeBufferBytes
                );

                totalBytes =
                    result.TotalBytes;

                if (result.Content.Length == 0)
                {
                    break;
                }

                if (ContainsNullByte(result.Content))
                {
                    throw new BinaryFileNotSupportedException(
                        relativePath
                    );
                }

                var characters =
                    new char[
                        StrictUtf8.GetMaxCharCount(
                            result.Content.Length
                        )
                    ];

                decoder.Convert(
                    result.Content,
                    characters,
                    flush: !result.Truncated,
                    out var bytesUsed,
                    out var charactersUsed,
                    out _
                );

                if (bytesUsed != result.Content.Length)
                {
                    throw new InvalidDataException(
                        "Could not process the complete repository read buffer."
                    );
                }

                var chunk =
                    new string(
                        characters,
                        0,
                        charactersUsed
                    );

                if (offset == 0)
                {
                    chunk =
                        RemoveUtf8Bom(chunk);
                }

                foreach (var character in chunk)
                {
                    if (currentLine >= startLine
                        && returnedLines < lineCount)
                    {
                        content.Append(character);
                    }

                    if (character != '\n')
                    {
                        continue;
                    }

                    if (currentLine >= startLine)
                    {
                        returnedLines++;
                    }

                    currentLine++;

                    if (returnedLines == lineCount)
                    {
                        break;
                    }
                }

                offset +=
                    result.Content.Length;

                if (!result.Truncated)
                {
                    break;
                }
            }
        }
        catch (DecoderFallbackException)
        {
            throw new BinaryFileNotSupportedException(
                relativePath
            );
        }

        /*
         * EOF can terminate the final line without a newline.
         * If we collected content from that line, it still counts
         * as a returned line.
         */
        if (returnedLines < lineCount
            && currentLine >= startLine
            && content.Length > 0
            && content[^1] != '\n')
        {
            returnedLines++;
        }

        return new RepositoryFileRangeContent(
            Path: relativePath,
            Content: content.ToString(),
            TotalBytes: totalBytes,
            StartLine: startLine,
            EndLine: returnedLines == 0
                ? null
                : startLine + returnedLines - 1
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
