using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class TacticalMapViewTests
{
    private sealed class Scene : IDisposable
    {
        internal long Clock;
        internal SnapshotBuffer Buffer;
        internal GameSessionScreen Screen;
        private readonly SKBitmap _bitmap = new(1920, 1080);
        private readonly SKCanvas _canvas;
        internal Scene(TacticalMapSettings? settings = null, params ObjectMotionSnapshot[] objects)
        {
            Buffer = new(() => Clock);
            Buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
                objects.Prepend(Ship()).ToImmutableArray(), "PLAYER"));
            Screen = new(Buffer, new LinearMotionPredictor(), timestampProvider: () => Clock, mapSettings: settings);
            _canvas = new(_bitmap); Render();
        }
        internal void Render(int milliseconds = 20)
        {
            Clock += Stopwatch.Frequency * milliseconds / 1000;
            Screen.Render(_canvas, 1920, 1080);
        }
        internal void Preset(int index)
        {
            var r = Screen.ScaleButtonRects[index]; Screen.OnMouseDown(r.MidX, r.MidY); Render(150);
        }
        internal (double X, double Y) WorldAt(float x, float y) =>
            (Screen.CameraFocusX + (x - 960) / Screen.CameraPixelsPerWorldUnit,
                Screen.CameraFocusY + (y - 540) / Screen.CameraPixelsPerWorldUnit);
        public void Dispose() { _canvas.Dispose(); _bitmap.Dispose(); }
    }
    private static ObjectMotionSnapshot Ship() => new("PLAYER", 10000, 10000, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip);
    private static ObjectMotionSnapshot Contact(string id, double x, double y, string type = SpaceObjectType.UnknownSpaceObject) =>
        new(id, x, y, 0, 0, RenderObjectType: type);

    [Theory]
    [InlineData(0, 100, 192)]
    [InlineData(1, 1000, 1920)]
    [InlineData(2, 100000, 192000)]
    [InlineData(3, 1000000, 1920000)]
    [InlineData(4, 10000000, 19200000)]
    public void Presets_match_physical_distances_and_viewport(int index, double meters, double widthKm)
    {
        using var s = new Scene(); s.Preset(index);
        Assert.Equal(meters, 100 / s.Screen.CameraPixelsPerWorldUnit, precision: 5);
        Assert.Equal(widthKm, 1920 * .1 / s.Screen.CameraPixelsPerWorldUnit, precision: 4);
    }

    [Fact]
    public void Follow_zoom_remains_centered_after_render_with_real_ship()
    {
        using var s = new Scene(new() { ZoomAnimationMs = 120 });
        s.Screen.OnMouseWheel(1100, 400, -1);
        s.Render(60);
        Assert.True(s.Screen.IsZoomAnimating);
        Assert.Equal(10000, s.Screen.CameraFocusX); Assert.Equal(10000, s.Screen.CameraFocusY);
        s.Render(60);
        Assert.False(s.Screen.IsZoomAnimating); Assert.Equal(.8, s.Screen.CameraPixelsPerWorldUnit, 12);
        Assert.True(s.Screen.IsFocusAttachedToPlayer);
    }

    [Fact]
    public void Free_zoom_preserves_cursor_anchor_during_every_animation_frame()
    {
        using var s = new Scene(new() { ZoomAnimationMs = 120 });
        s.Screen.OnMouseDown(1200, 650); s.Screen.OnMouseUp(1200, 650);
        var before = s.WorldAt(1100, 400);
        s.Screen.OnMouseWheel(1100, 400, -3);
        for (int i = 0; i < 6; i++)
        {
            s.Render(20); var after = s.WorldAt(1100, 400);
            Assert.Equal(before.X, after.X, 8); Assert.Equal(before.Y, after.Y, 8);
        }
        Assert.Equal(Math.Pow(1.25, -3), s.Screen.CameraPixelsPerWorldUnit, 12);
    }

    [Fact]
    public void Wheel_accumulates_fractional_events_and_reverses()
    {
        using var s = new Scene(new() { ZoomAnimationMs = 120 });
        for (int i = 0; i < 4; i++) s.Screen.OnMouseWheel(1100, 400, -.25f);
        s.Render(120); Assert.Equal(.8, s.Screen.CameraPixelsPerWorldUnit, 12);
        s.Screen.OnMouseWheel(1100, 400, 1); s.Render(120);
        Assert.Equal(1, s.Screen.CameraPixelsPerWorldUnit, 12);
    }

    [Fact]
    public void Wheel_is_consumed_by_entire_panel_including_padding_at_150_percent()
    {
        using var s = new Scene(); s.Screen.SetUiScale(1.5f); s.Render();
        foreach (var r in new[] { s.Screen.LastScalePanelRect, s.Screen.LastSpeedPanelRect, s.Screen.MapToolbarRect,
                     s.Screen.CommandsPanel.CaptionRect, s.Screen.ObjectInfoPanel.CaptionRect })
            s.Screen.OnMouseWheel((r.Left + 1) * 1.5f, (r.Top + 1) * 1.5f, -1);
        Assert.Equal(1, s.Screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Plus_minus_keys_and_home_work_on_actual_displayed_camera()
    {
        using var s = new Scene();
        s.Screen.OnKeyDown(Key.Minus); Assert.Equal(.8, s.Screen.CameraPixelsPerWorldUnit, 12);
        s.Screen.OnKeyDown(Key.KeypadAdd); Assert.Equal(1, s.Screen.CameraPixelsPerWorldUnit, 12);
        s.Screen.OnMouseDown(1200, 650); s.Screen.OnMouseUp(1200, 650);
        s.Screen.OnKeyDown(Key.Home); Assert.True(s.Screen.IsFocusAttachedToPlayer);
    }

    [Fact]
    public void Scale_crossing_does_not_lose_selected_unknown_contact_or_reveal_type()
    {
        using var s = new Scene(null, Contact("UNKNOWN", 10100, 10000));
        s.Screen.OnMouseDown(1060, 540); s.Preset(4);
        Assert.Equal("UNKNOWN", s.Screen.SelectedObjectId);
        var target = Assert.Single(s.Screen.RenderStates, r => r.Predicted.ObjectId == "UNKNOWN").Predicted;
        Assert.Null(target.DisplayName); Assert.Null(target.ObjectType);
        Assert.Equal(SpaceObjectType.UnknownSpaceObject, target.RenderObjectType);
        Assert.Equal(0, s.Screen.MapClusterCount);
    }

    [Fact]
    public void Close_contacts_cluster_and_click_expands_them_without_navigation()
    {
        using var s = new Scene(null, Contact("A", 1000000, 10000), Contact("B", 1000020, 10010));
        s.Preset(3); Assert.Equal(1, s.Screen.MapClusterCount);
        double before = s.Screen.CameraPixelsPerWorldUnit;
        s.Screen.OnMouseDown(1059, 540);
        s.Render(); Assert.True(s.Screen.CameraPixelsPerWorldUnit > before);
        Assert.Equal(0, s.Screen.MapClusterCount);
    }

    [Fact]
    public void Fit_target_keeps_both_objects_inside_free_map_region()
    {
        using var s = new Scene(null, Contact("TARGET", 10100, 10000));
        s.Screen.OnMouseDown(1060, 540); s.Preset(4);
        Assert.True(s.Screen.FitMapView(MapFitMode.Target));
        var rect = s.Screen.AvailableMapRect();
        foreach (var p in s.Screen.RenderStates)
        {
            float x = (float)(960 + (p.Predicted.X - s.Screen.CameraFocusX) * s.Screen.CameraPixelsPerWorldUnit);
            float y = (float)(540 + (p.Predicted.Y - s.Screen.CameraFocusY) * s.Screen.CameraPixelsPerWorldUnit);
            Assert.True(rect.Contains(x, y));
        }
        Assert.False(s.Screen.IsFocusAttachedToPlayer);
    }

    [Fact]
    public void System_fits_known_planet_radius_without_including_unknown_far_object()
    {
        using var s = new Scene(null, Contact("SUN", 0, 0, SpaceObjectType.Sun),
            Contact("PLANET", 1e10, 0, SpaceObjectType.Planet), Contact("UNKNOWN", 1e15, 1e15));
        Assert.True(s.Screen.FitMapView(MapFitMode.System));
        Assert.InRange(s.Screen.CameraPixelsPerWorldUnit, 1e-9, 1e-6);
        Assert.NotEmpty(GridRenderer.GetEligibleLevels(s.Screen.CameraPixelsPerWorldUnit));
        Assert.Equal(4, s.Buffer.Latest!.Snapshot.Objects.Length);
    }

    [Fact]
    public void Navigation_target_is_not_clustered_and_full_route_is_fitted()
    {
        using var s = new Scene(null, Contact("TARGET", 100000, 10000), Contact("OTHER", 100010, 10010));
        var ship = Ship() with { NavigationTargetObjectId = "TARGET", NavigationTargetX = 100000,
            NavigationTargetY = 10000, ActiveEngineCommandType = ShipEngineCommandTypes.Orbit,
            NavigationAngularInertiaDegPerSec = 1, SpeedKmS = 1 };
        s.Buffer.Update(new AuthoritativeSnapshot(2, 0, SimulationSpeed.Speed0,
            [ship, Contact("TARGET", 100000, 10000), Contact("OTHER", 100010, 10010)], "PLAYER"));
        s.Render(); s.Preset(4);
        Assert.Equal(0, s.Screen.MapClusterCount);
        Assert.True(s.Screen.FitMapView(MapFitMode.Route));
        var rect = s.Screen.AvailableMapRect();
        foreach (var p in s.Screen.GetNavigationTrajectory("PLAYER"))
        {
            var x = 960 + (p.X - s.Screen.CameraFocusX) * s.Screen.CameraPixelsPerWorldUnit;
            var y = 540 + (p.Y - s.Screen.CameraFocusY) * s.Screen.CameraPixelsPerWorldUnit;
            Assert.True(rect.Contains((float)x, (float)y));
        }
    }

    [Fact]
    public void Wheel_clamps_at_system_limit_and_rejects_invalid_input()
    {
        using var s = new Scene();
        for (int i = 0; i < 10; i++) s.Screen.OnMouseWheel(1100, 400, -32);
        Assert.Equal(1e-12, s.Screen.CameraPixelsPerWorldUnit);
        double focus = s.Screen.CameraFocusX;
        s.Screen.OnMouseWheel(1100, 400, float.NaN);
        s.Screen.OnMouseWheel(1100, 400, float.PositiveInfinity);
        Assert.Equal(1e-12, s.Screen.CameraPixelsPerWorldUnit);
        Assert.Equal(focus, s.Screen.CameraFocusX);
    }

    [Theory]
    [InlineData(1)] [InlineData(.1)] [InlineData(.001)] [InlineData(.00001)] [InlineData(1e-12)]
    public void Grid_has_bounded_visible_levels_at_every_scale(double ppu)
    {
        var settings = new TacticalMapSettings();
        var levels = GridRenderer.GetEligibleLevels(ppu, settings);
        Assert.InRange(levels.Count, 2, 6);
        Assert.Contains(levels, step => GridRenderer.LevelOpacity(step, ppu, settings) > .5);
        Assert.All(levels, step => Assert.InRange(step * ppu, 24, 480));
    }

    [Fact]
    public void Grid_fades_in_continuously_at_threshold()
    {
        var settings = new TacticalMapSettings();
        Assert.Equal(0, GridRenderer.LevelOpacity(100, .24, settings));
        Assert.InRange(GridRenderer.LevelOpacity(100, .240001, settings), 0, .0001);
        Assert.Equal(1, GridRenderer.LevelOpacity(100, .48, settings));
    }

    [Fact]
    public void Settings_reject_invalid_order_and_nonfinite_values()
    {
        Assert.Throws<ArgumentException>(() => (new TacticalMapSettings { MetersPerPixel = [100, 50, 1000, 10000, 100000] }).Validate());
        Assert.Throws<ArgumentException>(() => (new TacticalMapSettings { WheelFactor = double.NaN }).Validate());
        Assert.Throws<ArgumentException>(() => (new TacticalMapSettings { GridMantissas = [0] }).Validate());
    }

    [Fact]
    public void Distance_format_supports_meters_kilometers_and_AU()
    {
        Assert.Equal("100 m", TacticalMapSettings.FormatDistance(100));
        Assert.Equal("1 km", TacticalMapSettings.FormatDistance(1000));
        Assert.Equal("1 AU", TacticalMapSettings.FormatDistance(TacticalMapSettings.AstronomicalUnitKm * 1000));
    }
}
