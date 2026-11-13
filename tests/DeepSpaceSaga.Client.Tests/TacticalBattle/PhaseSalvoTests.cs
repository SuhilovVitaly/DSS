using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using Silk.NET.Input;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using System.Collections.Immutable;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PhaseSalvoTests
{
    [Theory]
    [InlineData(1f, 0)]
    [InlineData(1.5f, 0)]
    [InlineData(1.5f, 400)]
    public async Task Sixteen_queue_rows_scroll_and_follow_active_shooter_without_covering_actions(float scale, int offset)
    {
        await using var s = await Ep0019Scenes.Create("3-salvo-flight");
        var source = s.Screen.CaptureFrame(1280, 720)!;
        var unit = source.Battle.Units.First(u => u.Kind == TacticalUnitKind.Drone);
        var units = Enumerable.Range(0, 16).Select(i => unit with { Id = "queue-" + i }).ToImmutableArray();
        var queue = units.Select((u, i) => new TacticalFireQueueEntry(i, u.Id, 50)).ToImmutableArray();
        var shot = source.Salvo!.Flights[0].Shot;
        var flights = units.Select((u, i) => new TacticalShotFlight(shot with { ShooterId = u.Id }, i, i * 100, 1) { DurationMs = 100 }).ToImmutableArray();
        var frame = source with
        {
            Battle = source.Battle with { Units = source.Battle.Units.AddRange(units), FireQueue = queue },
            Salvo = source.Salvo with { Flights = flights },
            Panel = new(1040 + offset, 6, 1274 + offset, 382)
        };
        var panel = new PhasePanel();
        using var shortQueue = Draw(frame with { Battle = frame.Battle with { FireQueue = queue.Take(8).ToImmutableArray() } });
        using var fullQueue = Draw(frame);
        // More participants must not paint over the unchanged action/status area.
        for (int y = (int)(289 * scale); y < (int)(382 * scale); y++)
            for (int x = (int)((1040 + offset) * scale); x < (int)((1274 + offset) * scale); x++)
                Assert.Equal(shortQueue.GetPixel(x, y), fullQueue.GetPixel(x, y));
        Assert.False(panel.ScrollQueue(new(1150 + offset, 350), -1));
        Assert.True(panel.ScrollQueue(new(1150 + offset, 200), -100));
        var scrolledLayout = new BattleLayout();
        using (var scrolled = Draw(frame, scrolledLayout))
        {
            Assert.Contains(scrolledLayout.Texts, t => t.Text == "4.");
            Assert.DoesNotContain(scrolledLayout.Texts, t => t.Text == "1.");
        }
        var late = frame with { Salvo = frame.Salvo! with { Flights = flights.Select(f => f with { ElapsedMs = 1401 }).ToImmutableArray() } };
        var layout = new BattleLayout();
        using (var active = Draw(late, layout))
        {
            Assert.Contains(layout.Texts, t => t.Text == "15." && t.Color == BattleDesign.Yellow);
            Assert.DoesNotContain(layout.Texts, t => t.Text == "1.");
            Assert.True(layout.Rects["queue"].Bottom < panel.Action.Top);
            for (int i = 0; i < 6; i++) Assert.True(panel.ScrollQueue(new(1150 + offset, 200), 1));
        }
        using (var top = Draw(late, layout = new())) Assert.Contains(layout.Texts, t => t.Text == "1.");

        SKBitmap Draw(TacticalBattleFrame f, BattleLayout? layout = null)
        {
            var bitmap = new SKBitmap((int)((1280 + offset) * scale), (int)(720 * scale));
            using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.Transparent); canvas.Scale(scale);
            BattleDesign.Recording = layout;
            try { panel.Draw(canvas, f, layout ?? new()); }
            finally { BattleDesign.Recording = null; }
            return bitmap;
        }
    }

    [Fact]
    public async Task Salvo_results_replay_and_continue_use_same_revision()
    {

        await using var s = await Ep0019Scenes.Create("3-salvo-flight"); using var locale = Localization.UseLocaleForTests("Russian");
        using (var b = s.Render()) Assert.Contains(s.Screen.Layout.Texts, t => t.Text == "Выстрел 1 из 4");
        s.Screen.OnKeyDown(Key.Enter);
        using (var b = s.Render())
            foreach (string text in new[] { "ПРОМАХ", "Броня: 80 → 80", "100%", "4 выстрела · 3 попадания" }) Assert.Contains(s.Screen.Layout.Texts, t => t.Text == text);
        ulong revision = s.Screen.Flow.State!.Revision;
        var replay = s.Screen.ReplaySalvoBounds;
        s.Screen.OnMouseDown(replay.MidX, replay.MidY, MouseButton.Left);
        Assert.True(s.Screen.CaptureFrame(1920, 1080)!.Salvo!.InFlight);
        Assert.Equal(revision, s.Screen.Flow.State.Revision);
        s.Screen.OnKeyDown(Key.Enter); s.Screen.OnKeyDown(Key.Enter);
        Assert.False(s.Screen.HasSalvoPresentation);
    }
}
