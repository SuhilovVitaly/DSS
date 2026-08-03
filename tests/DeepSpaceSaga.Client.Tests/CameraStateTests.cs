using DeepSpaceSaga.Client.UI;

namespace DeepSpaceSaga.Client.Tests;

public class CameraStateTests
{
    [Fact]
    public void WorldToScreen_puts_focus_at_viewport_center()
    {
        var camera = new CameraState(focusX: 100, focusY: 200, pixelsPerWorldUnit: 1.0);

        var (sx, sy) = camera.WorldToScreen(100, 200, viewportWidth: 1920, viewportHeight: 1080);

        Assert.Equal(960, sx);
        Assert.Equal(540, sy);
    }

    [Fact]
    public void WorldToScreen_fixed_focus_10000_at_center()
    {
        var camera = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);

        var (sx, sy) = camera.WorldToScreen(10000, 10000, viewportWidth: 1920, viewportHeight: 1080);

        Assert.Equal(960, sx);
        Assert.Equal(540, sy);
    }

    [Fact]
    public void ScreenToWorld_at_viewport_center_returns_focus()
    {
        var camera = new CameraState(focusX: 5000, focusY: 7000, pixelsPerWorldUnit: 1.0);

        var (wx, wy) = camera.ScreenToWorld(960, 540, viewportWidth: 1920, viewportHeight: 1080);

        Assert.Equal(5000, wx, precision: 6);
        Assert.Equal(7000, wy, precision: 6);
    }

    [Fact]
    public void ScreenToWorld_roundtrips_through_WorldToScreen()
    {
        var camera = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);
        const double worldX = 10500;
        const double worldY = 9600;

        var (sx, sy) = camera.WorldToScreen(worldX, worldY, 1920, 1080);
        var (wx, wy) = camera.ScreenToWorld(sx, sy, 1920, 1080);

        Assert.Equal(worldX, wx, precision: 6);
        Assert.Equal(worldY, wy, precision: 6);
    }

    [Fact]
    public void WorldToScreen_with_scale_2_doubles_offset()
    {
        var camera1 = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);
        var camera2 = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 2.0);

        // World point 100 units right of focus
        var (sx1, _) = camera1.WorldToScreen(10100, 10000, 1920, 1080);
        var (sx2, _) = camera2.WorldToScreen(10100, 10000, 1920, 1080);

        // At scale 1: offset = 100 px from center
        Assert.Equal(960 + 100, sx1);
        // At scale 2: offset = 200 px from center
        Assert.Equal(960 + 200, sx2);
    }

    [Fact]
    public void WorldToScreen_handles_negative_world_coordinates()
    {
        var camera = new CameraState(focusX: 0, focusY: 0, pixelsPerWorldUnit: 1.0);

        var (sx, sy) = camera.WorldToScreen(-100, -50, 1920, 1080);

        Assert.Equal(960 - 100, sx);
        Assert.Equal(540 - 50, sy);
    }

    [Fact]
    public void ScreenToWorld_handles_negative_screen_coordinates()
    {
        var camera = new CameraState(focusX: 100, focusY: 200, pixelsPerWorldUnit: 1.0);

        // Screen coords to the left/top of viewport
        var (wx, wy) = camera.ScreenToWorld(100, 200, 1920, 1080);

        Assert.True(wx < 100);
        Assert.True(wy < 200);
    }

    [Fact]
    public void Focus_remains_centered_after_resize()
    {
        var camera = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);

        var (sx1, sy1) = camera.WorldToScreen(10000, 10000, 1920, 1080);
        Assert.Equal(960, sx1);
        Assert.Equal(540, sy1);

        // After resize to 2560×1440
        var (sx2, sy2) = camera.WorldToScreen(10000, 10000, 2560, 1440);
        Assert.Equal(1280, sx2);
        Assert.Equal(720, sy2);
    }

    [Fact]
    public void WorldToScreen_with_non_integer_pixels_per_unit()
    {
        var camera = new CameraState(focusX: 0, focusY: 0, pixelsPerWorldUnit: 0.5);

        // At half scale, a point 200 units right should appear 100 px right of center
        var (sx, _) = camera.WorldToScreen(200, 0, 1920, 1080);

        Assert.Equal(1060, sx);
    }
}
