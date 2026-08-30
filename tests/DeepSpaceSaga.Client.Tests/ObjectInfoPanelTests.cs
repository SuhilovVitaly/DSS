using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Object Info panel (top-right) — mirrors the Commands Panel (top-left) chrome and
/// shows two fixed rows: "Player Ship" and "Selected Object" (Image/Speed/Direction/Name).
/// </summary>
public class ObjectInfoPanelTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string PlayerShipId = "SPC-0001";

    private static (SnapshotBuffer buffer, GameSessionScreen screen) CreateScreen()
    {
        var buffer = new SnapshotBuffer();
        var predictor = new LinearMotionPredictor();
        var screen = new GameSessionScreen(buffer, predictor);
        return (buffer, screen);
    }

    private static void RenderScreen(GameSessionScreen screen, int width = ScreenWidth, int height = ScreenHeight)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
    }

    private static void UpdateBufferWithShip(
        SnapshotBuffer buffer,
        string playerShipId,
        double speedKmS = 0,
        double direction = 0,
        double x = 10000,
        double y = 10000,
        string? displayName = null)
    {
        var ship = new ObjectMotionSnapshot(
            playerShipId, x, y, speedKmS, direction, DisplayName: displayName);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: playerShipId));
    }

    // ── BuildLines (pure formatting) ────────────────────────────────

    [Fact]
    public void Lines_are_all_placeholders_when_no_data()
    {
        var lines = ObjectInfoPanel.BuildLines(null);

        Assert.Equal(3, lines.Count);
        Assert.Equal(("Name", "—"), lines[0]);
        Assert.Equal(("Speed", "—"), lines[1]);
        Assert.Equal(("Direction", "—"), lines[2]);
    }

    [Fact]
    public void Speed_is_formatted_with_three_decimal_places()
    {
        var data = new ObjectInfoPanelData("OBJ-1", null, SpeedKmS: 12.3456, Direction: 0, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal("12.346 km/s", Assert.Single(lines, l => l.Label == "Speed").Value);
    }

    [Fact]
    public void Speed_zero_is_formatted_correctly()
    {
        var data = new ObjectInfoPanelData("OBJ-1", null, SpeedKmS: 0, Direction: 0, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal("0 km/s", Assert.Single(lines, l => l.Label == "Speed").Value);
    }

    [Theory]
    [InlineData(0, "0°")]
    [InlineData(90, "90°")]
    [InlineData(359, "359°")]
    public void Direction_is_formatted_as_integer_degrees(double direction, string expected)
    {
        var data = new ObjectInfoPanelData("OBJ-1", null, SpeedKmS: 5, Direction: direction, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal(expected, Assert.Single(lines, l => l.Label == "Direction").Value);
    }

    [Fact]
    public void Name_falls_back_to_ObjectId_when_DisplayName_is_null()
    {
        var data = new ObjectInfoPanelData("OBJ-1", null, SpeedKmS: 5, Direction: 0, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal("OBJ-1", Assert.Single(lines, l => l.Label == "Name").Value);
    }

    [Fact]
    public void Name_uses_DisplayName_when_present()
    {
        var data = new ObjectInfoPanelData("OBJ-1", "Prospector", SpeedKmS: 5, Direction: 0, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal("Prospector", Assert.Single(lines, l => l.Label == "Name").Value);
    }

    // ── Panel geometry ───────────────────────────────────────────────

    [Fact]
    public void Panel_rect_is_populated_after_render()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        var panel = screen.ObjectInfoPanel;
        Assert.True(panel.CaptionRect.Width > 0);
        Assert.True(panel.CaptionRect.Height > 0);
        Assert.Equal(2, panel.RowCaptionRects.Count);
        Assert.Equal(2, panel.RowBodyRects.Count);
        Assert.All(panel.RowBodyRects, r => Assert.True(r.Width > 0 && r.Height > 0));
    }

    [Fact]
    public void Panel_is_positioned_at_top_right()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        var caption = screen.ObjectInfoPanel.CaptionRect;
        Assert.True(caption.Right <= ScreenWidth);
        Assert.True(caption.Left > ScreenWidth / 2f,
            $"Panel left edge ({caption.Left}) should be in the right half of the screen");
        Assert.True(caption.Top < ScreenHeight / 4f,
            $"Panel top edge ({caption.Top}) should be near the top of the screen");
    }

    [Fact]
    public void Hide_show_toggle_collapses_and_restores_the_rows()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        var panel = screen.ObjectInfoPanel;
        var toggle = panel.HideShowButtonRect;

        screen.OnMouseDown(toggle.MidX, toggle.MidY);
        RenderScreen(screen);
        Assert.Equal(ObjectInfoPanelState.Closed, panel.State);
        Assert.All(panel.RowBodyRects, r => Assert.Equal(0f, r.Width));

        screen.OnMouseDown(toggle.MidX, toggle.MidY);
        RenderScreen(screen);
        Assert.Equal(ObjectInfoPanelState.Open, panel.State);
        Assert.All(panel.RowBodyRects, r => Assert.True(r.Width > 0));
    }

    [Fact]
    public void Click_inside_panel_does_not_pan_camera()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        double focusXBefore = screen.CameraFocusX;
        double focusYBefore = screen.CameraFocusY;

        var body = screen.ObjectInfoPanel.RowBodyRects[0];
        var result = screen.OnMouseDown(body.MidX, body.MidY);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(focusXBefore, screen.CameraFocusX);
        Assert.Equal(focusYBefore, screen.CameraFocusY);
    }

    // ── Wiring: Player Ship row content ───────────────────────────────

    [Fact]
    public void PlayerShipInfo_is_null_without_a_snapshot()
    {
        var (_, screen) = CreateScreen();
        Assert.Null(screen.PlayerShipInfo);
    }

    [Fact]
    public void PlayerShipInfo_reflects_the_current_player_ship_state()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId, speedKmS: 15.5, direction: 45, displayName: "My Ship");
        RenderScreen(screen);

        var info = screen.PlayerShipInfo;
        Assert.NotNull(info);
        Assert.Equal(PlayerShipId, info!.Value.ObjectId);
        Assert.Equal("My Ship", info.Value.DisplayName);
        Assert.Equal(15.5, info.Value.SpeedKmS);
        Assert.Equal(45, info.Value.Direction);
    }
}
