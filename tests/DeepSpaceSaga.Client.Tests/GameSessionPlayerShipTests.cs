using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GameSessionPlayerShipTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    [Fact]
    public void Player_ship_is_rendered_green()
    {
        var buffer = new SnapshotBuffer();
        var playerShip = new ObjectMotionSnapshot("player", 10000, 10000, SpeedKmS: 0, Direction: 0);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(playerShip),
            PlayerShipObjectId: "player"));

        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());

        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        var centerPixel = bitmap.GetPixel(ScreenWidth / 2, ScreenHeight / 2);
        Assert.True(centerPixel.Green > centerPixel.Red);
        Assert.True(centerPixel.Green > centerPixel.Blue);
    }

    [Fact]
    public void Player_ship_is_rendered_as_oriented_glyph()
    {
        var buffer = new SnapshotBuffer();
        var playerShip = new ObjectMotionSnapshot("player", 10000, 10000, SpeedKmS: 0, Direction: 0);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(playerShip),
            PlayerShipObjectId: "player"));

        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());

        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        var nosePixel = bitmap.GetPixel(ScreenWidth / 2, ScreenHeight / 2 - 5);
        Assert.True(nosePixel.Green > nosePixel.Blue);

        var detachedMarkerPixel = bitmap.GetPixel(ScreenWidth / 2, ScreenHeight / 2 - 11);
        Assert.False(
            detachedMarkerPixel.Red > 220 &&
            detachedMarkerPixel.Green > 180 &&
            detachedMarkerPixel.Blue < 120);
    }

    [Fact]
    public void Camera_focus_follows_predicted_player_ship_position()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var playerShip = new ObjectMotionSnapshot("player", 10000, 10000, SpeedKmS: 5, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(playerShip),
            PlayerShipObjectId: "player"));

        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        clock.AdvanceMs(1000);

        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        Assert.Equal(10050, screen.CameraFocusX, precision: 6);
        Assert.Equal(10000, screen.CameraFocusY, precision: 6);
        Assert.True(screen.IsFocusAttachedToPlayer);
    }

    [Fact]
    public void Map_click_detaches_player_ship_focus()
    {
        var buffer = new SnapshotBuffer();
        var playerShip = new ObjectMotionSnapshot("player", 10000, 10000, SpeedKmS: 0, Direction: 0);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(playerShip),
            PlayerShipObjectId: "player"));

        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());

        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        screen.OnMouseDown(ScreenWidth / 2f + 100, ScreenHeight / 2f);

        Assert.False(screen.IsFocusAttachedToPlayer);
        Assert.Equal(10100, screen.CameraFocusX, precision: 6);
        Assert.Equal(10000, screen.CameraFocusY, precision: 6);
    }

    private sealed class FakeTimestamp
    {
        public long Timestamp { get; private set; }

        public void AdvanceMs(long milliseconds)
        {
            Timestamp += milliseconds * Stopwatch.Frequency / 1000;
        }
    }
}
