#nullable enable

using System.Globalization;
using System.Text;
using DrawingImage = System.Drawing.Image;

namespace Tiedragon.Graph.Image.Exif;

/// <summary>
/// Reads the small EXIF subset needed by Tiedragon graph/image tooling.
/// </summary>
public static class GraphImageExifReader
{
    private const int MakePropertyId = 0x010F;
    private const int ModelPropertyId = 0x0110;
    private const int SoftwarePropertyId = 0x0131;
    private static readonly int[] DatePropertyIds = [0x9003, 0x9004, 0x0132];

    public static string ReadSource(DrawingImage image)
    {
        var cameraSource = string.Join(" ", new[]
        {
            ReadAsciiProperty(image, MakePropertyId),
            ReadAsciiProperty(image, ModelPropertyId)
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(cameraSource)
            ? ReadAsciiProperty(image, SoftwarePropertyId)
            : cameraSource;
    }

    public static DateTime? ReadDateTaken(DrawingImage image)
    {
        foreach (var propertyId in DatePropertyIds)
        {
            var value = ReadAsciiProperty(image, propertyId);
            if (DateTime.TryParseExact(
                    value,
                    "yyyy:MM:dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var date))
            {
                return date;
            }
        }

        return null;
    }

    public static string ReadAsciiProperty(DrawingImage image, int propertyId)
    {
        try
        {
            if (!image.PropertyIdList.Contains(propertyId))
                return "";

            var value = image.GetPropertyItem(propertyId)?.Value;
            return value is { Length: > 0 }
                ? Encoding.ASCII.GetString(value).Trim('\0', ' ', '\r', '\n', '\t')
                : "";
        }
        catch (ArgumentException)
        {
            return "";
        }
    }
}
