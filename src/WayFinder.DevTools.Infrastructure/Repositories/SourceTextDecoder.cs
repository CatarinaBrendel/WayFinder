using System.Text;

namespace WayFinder.DevTools.Infrastructure.Repositories;

internal static class SourceTextDecoder
{
    public static string Decode(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length >= 2)
        {
            // UTF-16 little-endian
            if (content[0] == 0xFF && content[1] == 0xFE)
            {
                return new UnicodeEncoding(
                    bigEndian: false,
                    byteOrderMark: true,
                    throwOnInvalidBytes: true
                ).GetString(content, 2, content.Length - 2);
            }

            // UTF-16 big-endian
            if (content[0] == 0xFE && content[1] == 0xFF)
            {
                return new UnicodeEncoding(
                    bigEndian: true,
                    byteOrderMark: true,
                    throwOnInvalidBytes: true
                ).GetString(content, 2, content.Length - 2);
            }
        }

        var utf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true
        );

        // UTF-8 BOM
        if (content.Length >= 3
            && content[0] == 0xEF
            && content[1] == 0xBB
            && content[2] == 0xBF)
        {
            return utf8.GetString(
                content,
                3,
                content.Length - 3
            );
        }

        try
        {
            return utf8.GetString(content);
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return Encoding.GetEncoding(1252).GetString(content);
        }
    }
}
