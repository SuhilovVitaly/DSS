namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Immutable camera state defining the viewport transform.
/// Maps between world coordinates and screen coordinates.
/// </summary>
public sealed class CameraState
{
    /// <summary>World X coordinate at the center of the viewport.</summary>
    public double FocusX { get; private set; }

    /// <summary>World Y coordinate at the center of the viewport.</summary>
    public double FocusY { get; private set; }

    /// <summary>Screen pixels per world unit at the current zoom level.</summary>
    public double PixelsPerWorldUnit { get; }

    public CameraState(double focusX, double focusY, double pixelsPerWorldUnit)
    {
        FocusX = focusX;
        FocusY = focusY;
        PixelsPerWorldUnit = pixelsPerWorldUnit;
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
}
