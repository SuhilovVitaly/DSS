using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class PlayerShipInfoPanelTests
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
        string? activeEngineCommandType = null)
    {
        var ship = new ObjectMotionSnapshot(
            playerShipId,
            x,
            y,
            speedKmS,
            direction,
            ActiveEngineCommandType: activeEngineCommandType);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: playerShipId));
    }

    // ── Panel rect ────────────────────────────────────────────────

    [Fact]
    public void Panel_rect_is_populated_after_render()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        Assert.True(screen.LastPlayerShipPanelRect.Width > 0);
        Assert.True(screen.LastPlayerShipPanelRect.Height > 0);
    }

    [Fact]
    public void Panel_rect_is_populated_even_without_snapshot()
    {
        var (_, screen) = CreateScreen();
        RenderScreen(screen);

        Assert.True(screen.LastPlayerShipPanelRect.Width > 0);
        Assert.True(screen.LastPlayerShipPanelRect.Height > 0);
    }

    // ── Panel positioning ─────────────────────────────────────────

    [Fact]
    public void Panel_is_positioned_at_bottom_right()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        var panel = screen.LastPlayerShipPanelRect;
        // Panel should be near the right edge
        Assert.True(panel.Right <= ScreenWidth);
        Assert.True(panel.Right > ScreenWidth / 2f,
            $"Panel right edge ({panel.Right}) should be in the right half of the screen");
        // Panel should be near the bottom edge
        Assert.True(panel.Bottom <= ScreenHeight);
        Assert.True(panel.Bottom > ScreenHeight / 2f,
            $"Panel bottom edge ({panel.Bottom}) should be in the bottom half of the screen");
    }

    [Fact]
    public void Panel_does_not_overlap_engine_command_panel_at_1280x720()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen, 1280, 720);

        var playerPanel = screen.LastPlayerShipPanelRect;
        var commandPanel = screen.LastCommandPanelRect;

        Assert.False(
            playerPanel.IntersectsWith(commandPanel),
            $"Player ship panel {playerPanel} should not overlap command panel {commandPanel}");
    }

    [Fact]
    public void Panel_does_not_overlap_engine_command_panel_at_1920x1080()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen, 1920, 1080);

        var playerPanel = screen.LastPlayerShipPanelRect;
        var commandPanel = screen.LastCommandPanelRect;

        Assert.False(
            playerPanel.IntersectsWith(commandPanel),
            $"Player ship panel {playerPanel} should not overlap command panel {commandPanel}");
    }

    // ── Placeholder values (no snapshot / no player ship) ─────────

    [Fact]
    public void Lines_are_all_placeholders_when_no_snapshot()
    {
        var (_, screen) = CreateScreen();
        var lines = screen.BuildPlayerShipPanelLines(null);

        Assert.Equal(4, lines.Count);
        Assert.Equal(("Speed", "—"), lines[0]);
        Assert.Equal(("Course", "—"), lines[1]);
        Assert.Equal(("Location", "(—, —)"), lines[2]);
        Assert.Equal(("Engine", "—"), lines[3]);
    }

    [Fact]
    public void Lines_are_all_placeholders_when_player_ship_not_found()
    {
        var (buffer, screen) = CreateScreen();
        // Add a non-player ship (different ObjectId)
        var nonPlayerShip = new ObjectMotionSnapshot("OTHER-001", 5000, 5000, SpeedKmS: 5, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(nonPlayerShip),
            PlayerShipObjectId: PlayerShipId)); // PlayerShipObjectId references a ship not in Objects

        // BuildPlayerShipPanelLines with null simulates "player ship not in renderStates"
        var lines = screen.BuildPlayerShipPanelLines(null);

        Assert.Equal(4, lines.Count);
        Assert.Equal(("Speed", "—"), lines[0]);
        Assert.Equal(("Course", "—"), lines[1]);
        Assert.Equal(("Location", "(—, —)"), lines[2]);
        Assert.Equal(("Engine", "—"), lines[3]);
    }

    // ── Value formatting ──────────────────────────────────────────

    [Fact]
    public void Speed_is_formatted_with_three_decimal_places()
    {
        var (_, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 12.3456, Direction: 0);
        var predicted = source; // No prediction delta
        var state = new ObjectRenderState(source, predicted, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);
        var speedLine = Assert.Single(lines, l => l.Label == "Speed");
        Assert.Equal("12.346 km/s", speedLine.Value);
    }

    [Fact]
    public void Speed_zero_is_formatted_correctly()
    {
        var (_, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 0, Direction: 0);
        var state = new ObjectRenderState(source, source, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);
        var speedLine = Assert.Single(lines, l => l.Label == "Speed");
        Assert.Equal("0 km/s", speedLine.Value);
    }

    [Fact]
    public void Course_is_formatted_as_integer_degrees()
    {
        var (_, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 5, Direction: 90);
        var state = new ObjectRenderState(source, source, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);
        var courseLine = Assert.Single(lines, l => l.Label == "Course");
        Assert.Equal("90°", courseLine.Value);
    }

    [Fact]
    public void Course_zero_is_formatted_correctly()
    {
        var (_, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 5, Direction: 0);
        var state = new ObjectRenderState(source, source, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);
        var courseLine = Assert.Single(lines, l => l.Label == "Course");
        Assert.Equal("0°", courseLine.Value);
    }

    [Fact]
    public void Course_359_is_formatted_correctly()
    {
        var (_, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 5, Direction: 359);
        var state = new ObjectRenderState(source, source, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);
        var courseLine = Assert.Single(lines, l => l.Label == "Course");
        Assert.Equal("359°", courseLine.Value);
    }

    [Fact]
    public void Location_is_formatted_without_fractional_part()
    {
        var (_, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(PlayerShipId, X: 12345.67, Y: 98765.43, SpeedKmS: 5, Direction: 0);
        var state = new ObjectRenderState(source, source, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);
        var locLine = Assert.Single(lines, l => l.Label == "Location");
        Assert.Equal("(12346, 98765)", locLine.Value);
    }

    // ── Engine command display ────────────────────────────────────

    [Theory]
    [InlineData(ShipEngineCommandTypes.Accelerate, "Accelerate")]
    [InlineData(ShipEngineCommandTypes.Brake, "Brake")]
    [InlineData(ShipEngineCommandTypes.TurnLeftUntilCancel, "Turn Left Until Cancel")]
    [InlineData(ShipEngineCommandTypes.TurnRightUntilCancel, "Turn Right Until Cancel")]
    public void Cyclic_engine_commands_are_displayed(string commandType, string expectedDisplay)
    {
        var (_, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(
            PlayerShipId, 10000, 10000, SpeedKmS: 5, Direction: 0,
            ActiveEngineCommandType: commandType);
        var state = new ObjectRenderState(source, source, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);
        var engineLine = Assert.Single(lines, l => l.Label == "Engine");
        Assert.Equal(expectedDisplay, engineLine.Value);
    }

    [Theory]
    [InlineData(ShipEngineCommandTypes.TurnRightStep)]
    [InlineData(ShipEngineCommandTypes.TurnLeftStep)]
    [InlineData(ShipEngineCommandTypes.CancelAll)]
    public void One_shot_and_cancel_commands_show_placeholder(string commandType)
    {
        var (_, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(
            PlayerShipId, 10000, 10000, SpeedKmS: 5, Direction: 0,
            ActiveEngineCommandType: commandType);
        var state = new ObjectRenderState(source, source, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);
        var engineLine = Assert.Single(lines, l => l.Label == "Engine");
        Assert.Equal("—", engineLine.Value);
    }

    [Fact]
    public void Null_engine_command_shows_placeholder()
    {
        var (_, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(
            PlayerShipId, 10000, 10000, SpeedKmS: 5, Direction: 0,
            ActiveEngineCommandType: null);
        var state = new ObjectRenderState(source, source, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);
        var engineLine = Assert.Single(lines, l => l.Label == "Engine");
        Assert.Equal("—", engineLine.Value);
    }

    // ── Panel click consumes event ────────────────────────────────

    [Fact]
    public void Click_inside_player_ship_panel_does_not_pan_camera()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        double focusXBefore = screen.CameraFocusX;
        double focusYBefore = screen.CameraFocusY;

        var panel = screen.LastPlayerShipPanelRect;
        float px = panel.MidX;
        float py = panel.MidY;

        var result = screen.OnMouseDown(px, py);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(focusXBefore, screen.CameraFocusX);
        Assert.Equal(focusYBefore, screen.CameraFocusY);
    }

    // ── Predicted state is used ────────────────────────────────────

    [Fact]
    public void Panel_uses_predicted_state_for_values()
    {
        var (buffer, screen) = CreateScreen();
        var source = new ObjectMotionSnapshot(PlayerShipId, X: 10000, Y: 10000, SpeedKmS: 0, Direction: 0);
        // Predicted state has different values (simulating motion prediction)
        var predicted = new ObjectMotionSnapshot(PlayerShipId, X: 10100, Y: 10200, SpeedKmS: 15.5, Direction: 45);
        var state = new ObjectRenderState(source, predicted, IsPlayerShip: true);

        var lines = screen.BuildPlayerShipPanelLines(state);

        Assert.Equal("15.5 km/s", Assert.Single(lines, l => l.Label == "Speed").Value);
        Assert.Equal("45°", Assert.Single(lines, l => l.Label == "Course").Value);
        Assert.Equal("(10100, 10200)", Assert.Single(lines, l => l.Label == "Location").Value);
    }
}
