#nullable enable

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tiedragon.Graph.Image.Exif;
using DrawingImage = System.Drawing.Image;

namespace Tiedragon.Graph.Image;

/// <summary>
/// Reads package-safe metadata from common help-package image bytes.
/// </summary>
public static class GraphImageMetadataReader
{
    public static GraphImageMetadata Read(string packagePath, byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return GraphImageMetadata.Empty;

        return Path.GetExtension(packagePath).ToLowerInvariant() switch
        {
            ".svg" => ReadSvg(bytes),
            ".webp" => ReadWebP(bytes),
            ".gif" => ReadGif(bytes),
            ".bmp" => ReadBmp(bytes),
            _ => ReadRaster(bytes),
        };
    }

    private static GraphImageMetadata ReadRaster(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var image = DrawingImage.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);
            return new GraphImageMetadata(
                image.Width,
                image.Height,
                GraphImageExifReader.ReadDateTaken(image),
                GraphImageExifReader.ReadSource(image));
        }
        catch (ArgumentException)
        {
            return GraphImageMetadata.Empty;
        }
    }

    private static GraphImageMetadata ReadSvg(byte[] bytes)
    {
        try
        {
            var text = Encoding.UTF8.GetString(bytes);
            var width = ReadSvgLength(text, "width");
            var height = ReadSvgLength(text, "height");
            if ((width is null || height is null) &&
                Regex.Match(text, @"viewBox\s*=\s*[""'][^""']*?\s+(?<w>[-+]?\d+(?:\.\d+)?)\s+(?<h>[-+]?\d+(?:\.\d+)?)[""']", RegexOptions.IgnoreCase) is { Success: true } viewBox)
            {
                width ??= (int)Math.Round(double.Parse(viewBox.Groups["w"].Value, CultureInfo.InvariantCulture));
                height ??= (int)Math.Round(double.Parse(viewBox.Groups["h"].Value, CultureInfo.InvariantCulture));
            }

            return new GraphImageMetadata(width, height, null, "SVG");
        }
        catch
        {
            return GraphImageMetadata.Empty;
        }
    }

    private static GraphImageMetadata ReadWebP(byte[] bytes)
    {
        return TryReadWebPDimensions(bytes, out var width, out var height)
            ? new GraphImageMetadata(width, height, null, "WEBP")
            : ReadRaster(bytes);
    }

    private static GraphImageMetadata ReadGif(byte[] bytes)
    {
        if (bytes.Length >= 10 &&
            bytes[0] == 'G' &&
            bytes[1] == 'I' &&
            bytes[2] == 'F')
        {
            return new GraphImageMetadata(ReadUInt16LittleEndian(bytes, 6), ReadUInt16LittleEndian(bytes, 8), null, "GIF");
        }

        return ReadRaster(bytes);
    }

    private static GraphImageMetadata ReadBmp(byte[] bytes)
    {
        if (bytes.Length >= 26 &&
            bytes[0] == 'B' &&
            bytes[1] == 'M')
        {
            var width = Math.Abs(ReadInt32LittleEndian(bytes, 18));
            var height = Math.Abs(ReadInt32LittleEndian(bytes, 22));
            return new GraphImageMetadata(width, height, null, "BMP");
        }

        return ReadRaster(bytes);
    }

    private static int? ReadSvgLength(string text, string attribute)
    {
        var match = Regex.Match(text, @"\b" + Regex.Escape(attribute) + @"\s*=\s*[""'](?<value>[-+]?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        return match.Success
            ? (int)Math.Round(double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture))
            : null;
    }

    private static bool TryReadWebPDimensions(byte[] bytes, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (bytes.Length < 30 ||
            !HasAscii(bytes, 0, "RIFF") ||
            !HasAscii(bytes, 8, "WEBP"))
        {
            return false;
        }

        var offset = 12;
        while (offset + 8 <= bytes.Length)
        {
            var chunk = Encoding.ASCII.GetString(bytes, offset, 4);
            var chunkSize = ReadInt32LittleEndian(bytes, offset + 4);
            var dataOffset = offset + 8;
            if (chunkSize < 0 || dataOffset + chunkSize > bytes.Length)
                return false;

            if (chunk == "VP8X" && chunkSize >= 10)
            {
                width = ReadUInt24LittleEndian(bytes, dataOffset + 4) + 1;
                height = ReadUInt24LittleEndian(bytes, dataOffset + 7) + 1;
                return true;
            }

            if (chunk == "VP8L" && chunkSize >= 5 && bytes[dataOffset] == 0x2F)
            {
                var bits = ReadUInt32LittleEndian(bytes, dataOffset + 1);
                width = (int)(bits & 0x3FFF) + 1;
                height = (int)((bits >> 14) & 0x3FFF) + 1;
                return true;
            }

            if (chunk == "VP8 " && chunkSize >= 10 &&
                bytes[dataOffset + 3] == 0x9D &&
                bytes[dataOffset + 4] == 0x01 &&
                bytes[dataOffset + 5] == 0x2A)
            {
                width = ReadUInt16LittleEndian(bytes, dataOffset + 6) & 0x3FFF;
                height = ReadUInt16LittleEndian(bytes, dataOffset + 8) & 0x3FFF;
                return true;
            }

            offset = dataOffset + chunkSize + (chunkSize % 2);
        }

        return false;
    }

    private static bool HasAscii(byte[] bytes, int offset, string value)
    {
        if (offset + value.Length > bytes.Length)
            return false;

        for (var i = 0; i < value.Length; i++)
        {
            if (bytes[offset + i] != value[i])
                return false;
        }

        return true;
    }

    private static int ReadUInt16LittleEndian(byte[] bytes, int offset) =>
        bytes[offset] | (bytes[offset + 1] << 8);

    private static int ReadInt32LittleEndian(byte[] bytes, int offset) =>
        bytes[offset] |
        (bytes[offset + 1] << 8) |
        (bytes[offset + 2] << 16) |
        (bytes[offset + 3] << 24);

    private static uint ReadUInt32LittleEndian(byte[] bytes, int offset) =>
        (uint)(bytes[offset] |
               (bytes[offset + 1] << 8) |
               (bytes[offset + 2] << 16) |
               (bytes[offset + 3] << 24));

    private static int ReadUInt24LittleEndian(byte[] bytes, int offset) =>
        bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16);
}
