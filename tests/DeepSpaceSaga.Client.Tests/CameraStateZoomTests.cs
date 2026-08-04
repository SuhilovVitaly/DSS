using DeepSpaceSaga.Client.UI;

namespace DeepSpaceSaga.Client.Tests;

public class CameraStateZoomTests
{
    private const int ViewportW = 1920;
    private const int ViewportH = 1080;

    [Fact]
    public void SetZoom_updates_PixelsPerWorldUnit()
    {
        var camera = new CameraState(10000, 10000, 1.0);
        camera.SetZoom(2.0);
        Assert.Equal(2.0, camera.PixelsPerWorldUnit);
    }

    [Fact]
    public void ZoomAt_keeps_world_point_under_cursor()
    {
        var camera = new CameraState(10000, 10000, 1.0);

        // Click at viewport center → world (10000, 10000)
        float screenX = ViewportW / 2f; // 960
        float screenY = ViewportH / 2f; // 540

        camera.ZoomAt(factor: 2.0, screenX, screenY, ViewportW, ViewportH);

        // After zoom-in at center, the same world point should still be at center
        var (sx, sy) = camera.WorldToScreen(10000, 10000, ViewportW, ViewportH);
        Assert.Equal(ViewportW / 2f, sx);
        Assert.Equal(ViewportH / 2f, sy);
        Assert.Equal(2.0, camera.PixelsPerWorldUnit);
    }

    [Fact]
    public void ZoomAt_increases_pixels_per_world_unit_on_zoom_in()
    {
        var camera = new CameraState(10000, 10000, 1.0);
        camera.ZoomAt(1.25, 0, 0, ViewportW, ViewportH);
        Assert.Equal(1.25, camera.PixelsPerWorldUnit);
    }

    [Fact]
    public void ZoomAt_decreases_pixels_per_world_unit_on_zoom_out()
    {
        var camera = new CameraState(10000, 10000, 1.0);
        camera.ZoomAt(1.0 / 1.25, 0, 0, ViewportW, ViewportH);
        Assert.Equal(0.8, camera.PixelsPerWorldUnit);
    }

    [Fact]
    public void ZoomAt_is_clamped_to_max()
    {
        var camera = new CameraState(0, 0, 8.0);
        camera.ZoomAt(2.0, 0, 0, ViewportW, ViewportH, maxPpu: 10.0);
        // 8.0 * 2.0 = 16.0 → clamped to 10.0
        Assert.Equal(10.0, camera.PixelsPerWorldUnit);
    }

    [Fact]
    public void ZoomAt_is_clamped_to_min()
    {
        var camera = new CameraState(0, 0, 0.0002);
        camera.ZoomAt(0.5, 0, 0, ViewportW, ViewportH, minPpu: 0.0001);
        // 0.0002 * 0.5 = 0.0001 → exactly min, allowed
        Assert.Equal(0.0001, camera.PixelsPerWorldUnit);

        camera.ZoomAt(0.5, 0, 0, ViewportW, ViewportH, minPpu: 0.0001);
        // 0.0001 * 0.5 = 0.00005 → clamped to 0.0001
        Assert.Equal(0.0001, camera.PixelsPerWorldUnit);
    }

    [Fact]
    public void ZoomAt_anchor_point_off_center()
    {
        var camera = new CameraState(0, 0, 1.0);

        // Cursor at (100, 200) from top-left
        // At ppu=1.0, world point under cursor = (100-960, 200-540) = (-860, -340)
        float sx = 100;
        float sy = 200;

        // World point under cursor before zoom
        var (wxBefore, wyBefore) = camera.ScreenToWorld(sx, sy, ViewportW, ViewportH);

        camera.ZoomAt(2.0, sx, sy, ViewportW, ViewportH);

        // Same world point should still be under the cursor after zoom
        var (sxAfter, syAfter) = camera.WorldToScreen(wxBefore, wyBefore, ViewportW, ViewportH);
        Assert.Equal(sx, sxAfter, precision: 3);
        Assert.Equal(sy, syAfter, precision: 3);
        Assert.Equal(2.0, camera.PixelsPerWorldUnit);
    }

    [Fact]
    public void ZoomAt_at_clamped_boundary_does_not_change_focus_or_zoom()
    {
        var camera = new CameraState(10000, 10000, 10.0);

        double fxBefore = camera.FocusX;
        double fyBefore = camera.FocusY;

        // Already at max, zoom-in at center should clamp ppu and not move focus
        camera.ZoomAt(1.25, ViewportW / 2f, ViewportH / 2f, ViewportW, ViewportH,
                     maxPpu: 10.0);

        Assert.Equal(10.0, camera.PixelsPerWorldUnit);
        Assert.Equal(fxBefore, camera.FocusX);
        Assert.Equal(fyBefore, camera.FocusY);
    }

    [Fact]
    public void SetZoom_clamps_invalid_values()
    {
        var camera = new CameraState(0, 0, 1.0);

        camera.SetZoom(0);
        Assert.Equal(0.0001, camera.PixelsPerWorldUnit);

        camera.SetZoom(-5);
        Assert.Equal(0.0001, camera.PixelsPerWorldUnit);

        camera.SetZoom(999);
        Assert.Equal(10.0, camera.PixelsPerWorldUnit);
    }

    [Fact]
    public void SetZoom_clamps_NaN()
    {
        var camera = new CameraState(0, 0, 1.0);
        camera.SetZoom(double.NaN);
        Assert.Equal(0.0001, camera.PixelsPerWorldUnit);
    }

    [Fact]
    public void Constructor_clamps_invalid_initial_zoom()
    {
        var cam1 = new CameraState(0, 0, double.NaN);
        Assert.Equal(0.0001, cam1.PixelsPerWorldUnit);

        var cam2 = new CameraState(0, 0, 0);
        Assert.Equal(0.0001, cam2.PixelsPerWorldUnit);

        var cam3 = new CameraState(0, 0, -5);
        Assert.Equal(0.0001, cam3.PixelsPerWorldUnit);

        var cam4 = new CameraState(0, 0, 999);
        Assert.Equal(10.0, cam4.PixelsPerWorldUnit);

        var cam5 = new CameraState(0, 0, double.PositiveInfinity);
        Assert.Equal(10.0, cam5.PixelsPerWorldUnit);

        var cam6 = new CameraState(0, 0, double.NegativeInfinity);
        Assert.Equal(0.0001, cam6.PixelsPerWorldUnit);
    }

    [Fact]
    public void ZoomAt_clamps_NaN_factor()
    {
        var camera = new CameraState(0, 0, 1.0);
        camera.ZoomAt(double.NaN, 0, 0, ViewportW, ViewportH);
        // NaN * 1.0 = NaN → clamped to minPpu, focus unchanged at center
        Assert.Equal(0.0001, camera.PixelsPerWorldUnit);
    }
}
