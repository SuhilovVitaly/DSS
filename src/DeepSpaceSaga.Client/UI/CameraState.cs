namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Camera state defining the viewport transform.
/// Maps between world coordinates and screen coordinates.
/// Focus is mutable via SetFocus to support camera movement.
/// </summary>
public sealed class CameraState
{
    /// <summary>World X coordinate at the center of the viewport.</summary>
    public double FocusX { get; private set; }

    /// <summary>World Y coordinate at the center of the viewport.</summary>
    public double FocusY { get; private set; }

    internal const double DefaultMinPpu = 0.0001;
    internal const double DefaultMaxPpu = 10.0;

    /// <summary>Screen pixels per world unit at the current zoom level.</summary>
    public double PixelsPerWorldUnit { get; private set; }

    public CameraState(double focusX, double focusY, double pixelsPerWorldUnit)
    {
        FocusX = focusX;
        FocusY = focusY;
        PixelsPerWorldUnit = ClampZoom(pixelsPerWorldUnit);
    }

    /// <summary>
    /// Move the camera focus to a new world position.
    /// </summary>
    public void SetFocus(double focusX, double focusY)
    {
        FocusX = focusX;
        FocusY = focusY;
    }

    /// <summary>
    /// Set the zoom level (pixels per world unit). Does not adjust focus.
    /// Clamped to [MinPpu, MaxPpu]. NaN, Infinity, zero and negative → coerced to safe bound.
    /// </summary>
    public void SetZoom(double pixelsPerWorldUnit)
    {
        PixelsPerWorldUnit = ClampZoom(pixelsPerWorldUnit);
    }

    /// <summary>
    /// Zoom by a multiplicative factor, keeping the world point under (screenX, screenY) fixed.
    /// Clamps to [MinPixelsPerWorldUnit, MaxPixelsPerWorldUnit].
    /// </summary>
    public void ZoomAt(
        double factor,
        float screenX,
        float screenY,
        int viewportWidth,
        int viewportHeight,
        double minPpu = DefaultMinPpu,
        double maxPpu = DefaultMaxPpu)
    {
        double newPpu = ClampZoom(PixelsPerWorldUnit * factor, minPpu, maxPpu);

        if (newPpu == PixelsPerWorldUnit)
            return;

        // World point under cursor before zoom
        double worldX = FocusX + (screenX - viewportWidth / 2.0) / PixelsPerWorldUnit;
        double worldY = FocusY + (screenY - viewportHeight / 2.0) / PixelsPerWorldUnit;

        // Update zoom then re-center so the same world point stays under the cursor
        PixelsPerWorldUnit = newPpu;

        FocusX = worldX - (screenX - viewportWidth / 2.0) / newPpu;
        FocusY = worldY - (screenY - viewportHeight / 2.0) / newPpu;
    }

    /// <summary>
    /// Convert world coordinates to screen coordinates.
    /// Center of viewport corresponds to (FocusX, FocusY).
    /// </summary>
    public (float X, float Y) WorldToScreen(double worldX, double worldY, int viewportWidth, int viewportHeight)
    {
        double screenX = viewportWidth / 2.0 + (worldX - FocusX) * PixelsPerWorldUnit;
        double screenY = viewportHeight / 2.0 + (worldY - FocusY) * PixelsPerWorldUnit;
        return ((float)screenX, (float)screenY);
    }

    /// <summary>
    /// Convert screen coordinates to world coordinates.
    /// Inverse of WorldToScreen.
    /// </summary>
    public (double X, double Y) ScreenToWorld(float screenX, float screenY, int viewportWidth, int viewportHeight)
    {
        double worldX = FocusX + (screenX - viewportWidth / 2.0) / PixelsPerWorldUnit;
        double worldY = FocusY + (screenY - viewportHeight / 2.0) / PixelsPerWorldUnit;
        return (worldX, worldY);
    }

    /// <summary>
    /// Normalize a zoom value to a safe range.
    /// NaN → minPpu. ±Infinity → nearest bound. Zero/negative → minPpu.
    /// </summary>
    internal static double ClampZoom(double value, double minPpu = DefaultMinPpu, double maxPpu = DefaultMaxPpu)
    {
        if (double.IsNaN(value))
            return minPpu;

        if (double.IsNegativeInfinity(value) || value <= 0.0)
            return minPpu;

        if (double.IsPositiveInfinity(value))
            return maxPpu;

        return Math.Clamp(value, minPpu, maxPpu);
    }
}
