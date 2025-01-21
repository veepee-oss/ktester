using System.IO.Compression;
using System.IO;
using System.Text;
using System.Linq;
using MessagePack;

namespace System;

public static class ByteArrayExtensions
{
    private static readonly byte[] GZipHeader = { 31, 139, 8 };

    public static string ToText(this byte[] bytes, int? count = null, bool tryDecompression = false)
    {
        if (bytes is null)
            return null;

        if (tryDecompression)
        {
            try
            {
                if (bytes.Length > 3 && bytes[0] == GZipHeader[0] && bytes[1] == GZipHeader[1] && bytes[2] == GZipHeader[2])
                    return bytes.Take(count is null ? bytes.Length : Math.Min(count.Value, bytes.Length)).ToArray().Decompress();
                else
                {
                    var message = MessagePackSerializer.ConvertToJson(bytes);
                    return count is null ? message : message.Substring(0, Math.Min(count.Value, message.Length));
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        return Encoding.UTF8.GetString(bytes, 0, count is null ? bytes.Length : Math.Min(count.Value, bytes.Length));
    }

    public static string Decompress(this byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes);
        using var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        gZipStream.CopyTo(resultStream);
        return Encoding.UTF8.GetString(resultStream.ToArray());
    }
}
