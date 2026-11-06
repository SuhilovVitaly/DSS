using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0010TK0004Tests
{
    private static ObjectMotionSnapshot Player(double turn = 0) => new("player", 0, 0, 1, 0,
        RenderObjectType: SpaceObjectType.PlayerShip, MaxSpeedKmS: 5, TurnHoldRateDegPerSec: turn == 0 ? null : turn);
    private static AuthoritativeSnapshot Scene(params ObjectMotionSnapshot[] objects) => new(1, 0, SimulationSpeed.Speed1,
        objects.ToImmutableArray(), "player");

    private static void AssertOutsidePanels(GameSessionScreen screen, SKRect free, float scale)
    {
        foreach (var panel in new[] { screen.CommandsPanel.CaptionRect, screen.CommandsPanel.BodyRect,
            screen.ObjectInfoPanel.CaptionRect, screen.ObjectInfoPanel.BodyRect, screen.MapToolbarRect })
        {
            if (panel.IsEmpty) continue;
            Assert.False(free.IntersectsWith(new SKRect(panel.Left * scale, panel.Top * scale, panel.Right * scale, panel.Bottom * scale)),
                $"Free map viewport {free} must avoid panel {panel} at scale{scale}");
        }
    }

    [Fact]
    public void Bare_F_and_Ctrl_F_have_distinct_actual_input_edges()
    {
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(Scene(Player()));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1920, 1080);
        var tracker = new KeyboardEdgeTracker(); var down = new HashSet<Key> { Key.F };
        var pressed = new Key[32]; var released = new Key[32];
        var (count, _) = tracker.PollBoth(down.Contains, pressed, released);
        Assert.Equal(1, count); Assert.Equal(Key.F, pressed[0]);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(pressed[0]));
        down.Clear(); var (_, up) = tracker.PollBoth(down.Contains, pressed, released);
        for (int i = 0; i < up; i++) screen.OnKeyUp(released[i]);
        down.UnionWith([Key.ControlLeft, Key.F]);
        (count, _) = tracker.PollBoth(down.Contains, pressed, released);
        Assert.Equal(new[] { Key.ControlLeft, Key.F }, pressed[..count]);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(pressed[0]));
        Assert.Equal(ScreenEvent.OpenFinance, screen.OnKeyDown(pressed[1]));
        tracker.ResyncToCurrentState(down.Contains);
        (count, up) = tracker.PollBoth(down.Contains, pressed, released); Assert.Equal(0, count); Assert.Equal(0, up);
        screen.OnDeactivated();
    }
}
