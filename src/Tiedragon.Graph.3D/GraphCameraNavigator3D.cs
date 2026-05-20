#nullable enable

namespace Tiedragon.Graph.G3D;

/// <summary>
/// Named camera positions for Graph3D.
/// </summary>
public enum GraphCameraPreset3D
{
    Isometric,
    Front,
    Top,
    Right
}

/// <summary>
/// Creates and updates Graph3D camera values for presets, rotation, and zoom.
/// </summary>
public static class GraphCameraNavigator3D
{
    /// <summary>
    /// Gets the default isometric camera.
    /// </summary>
    public static GraphCamera3D Default => Preset(GraphCameraPreset3D.Isometric);

    /// <summary>
    /// Creates a camera for the requested preset.
    /// </summary>
    public static GraphCamera3D Preset(GraphCameraPreset3D preset)
    {
        return preset switch
        {
            GraphCameraPreset3D.Front => new GraphCamera3D(0d, 0d, 1d),
            GraphCameraPreset3D.Top => new GraphCamera3D(0d, 90d, 1d),
            GraphCameraPreset3D.Right => new GraphCamera3D(90d, 0d, 1d),
            _ => new GraphCamera3D(35d, 28d, 1d)
        };
    }

    /// <summary>
    /// Rotates a camera and clamps pitch so the projection stays stable.
    /// </summary>
    public static GraphCamera3D Rotate(GraphCamera3D camera, double deltaYawDegrees, double deltaPitchDegrees)
    {
        return camera with
        {
            YawDegrees = NormalizeDegrees(camera.YawDegrees + deltaYawDegrees),
            PitchDegrees = ClampPitch(camera.PitchDegrees + deltaPitchDegrees),
            Zoom = NormalizeZoom(camera.Zoom)
        };
    }

    /// <summary>
    /// Applies a zoom factor while keeping zoom inside the supported range.
    /// </summary>
    public static GraphCamera3D Zoom(GraphCamera3D camera, double factor)
    {
        if (!double.IsFinite(factor) || factor <= 0d)
            factor = 1d;

        return camera with { Zoom = NormalizeZoom(camera.Zoom * factor) };
    }

    /// <summary>
    /// Zooms the camera in.
    /// </summary>
    public static GraphCamera3D ZoomIn(GraphCamera3D camera, double factor = 1.15d)
    {
        return Zoom(camera, factor);
    }

    /// <summary>
    /// Zooms the camera out.
    /// </summary>
    public static GraphCamera3D ZoomOut(GraphCamera3D camera, double factor = 1.15d)
    {
        return Zoom(camera, 1d / factor);
    }

    /// <summary>
    /// Resets the camera to a preset.
    /// </summary>
    public static GraphCamera3D Reset(GraphCameraPreset3D preset = GraphCameraPreset3D.Isometric)
    {
        return Preset(preset);
    }

    private static double NormalizeDegrees(double degrees)
    {
        if (!double.IsFinite(degrees))
            return 0d;

        degrees %= 360d;
        return degrees < 0d ? degrees + 360d : degrees;
    }

    private static double ClampPitch(double pitch)
    {
        if (!double.IsFinite(pitch))
            return 28d;

        return Math.Clamp(pitch, -89d, 89d);
    }

    private static double NormalizeZoom(double zoom)
    {
        if (!double.IsFinite(zoom) || zoom <= 0d)
            return 1d;

        return Math.Clamp(zoom, 0.05d, 40d);
    }
}
