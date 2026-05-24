#nullable enable

namespace Tiedragon.Graph.Image;

/// <summary>
/// Lightweight image metadata used by graph/help tooling without exposing UI details.
/// </summary>
public sealed record GraphImageMetadata(
    int? Width,
    int? Height,
    DateTime? DateTaken,
    string Source)
{
    public static GraphImageMetadata Empty { get; } = new(null, null, null, "");
}
