using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatPresentationResumeTests
{
    [Fact]
    public async Task Loaded_active_torpedo_restores_remaining_prediction_and_busy_panel()
    {
        await using var fixture = await CombatSessionFixture.Create();
        await fixture.Launch("active");
        var before = await fixture.Advance(12345);
        var loaded = await fixture.SaveAndRestore();
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.Update(loaded);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        var flightObject = Assert.Single(loaded.Objects.Where(o => o.Torpedo is not null));
        for (int reopen = 0; reopen < 3; reopen++)
        {
            screen.OnActivated();
            Render(screen);
            Assert.Equal("Guiding", screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher").StatusBarText);
            Assert.False(screen.CommandsPanel.AllCommandButtons.Single(b => b.CommandTypeId == CombatCommandTypes.Fire).Enabled);
            Assert.Empty(screen.CombatEffects.Active);
            var path = screen.CombatTrajectories[flightObject.ObjectId].Prediction;
            Assert.NotEmpty(path);
            Assert.Equal(before.Objects.Single(o => o.Torpedo is not null).Torpedo!.Trail.ToArray(), flightObject.Torpedo!.Trail.ToArray());
            Assert.Equal(flightObject.X, path[0].X, 6);
            Assert.Equal(flightObject.Y, path[0].Y, 6);
            screen.OnDeactivated();
            now += Stopwatch.Frequency * 5;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Loaded_completed_impact_has_no_explosion_or_terminal_trail(int hits)
    {
        await using var fixture = await CombatSessionFixture.Create();
        AuthoritativeSnapshot before = fixture.Engine.CaptureSnapshot();
        for (int shot = 1; shot <= hits; shot++)
        {
            var launched = await fixture.Launch("hit-" + shot);
            var flight = Assert.Single(launched.Objects.Where(o => o.Torpedo is not null)).Torpedo!;
            before = await fixture.Advance(flight.PredictedImpactMotionTimeMs!.Value - launched.MotionTimeMs + 1000);
        }
        Assert.Equal(hits, before.CombatImpacts.Length);
        var loaded = await fixture.SaveAndRestore();
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(loaded);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        for (int reopen = 0; reopen < 3; reopen++)
        {
            screen.OnActivated();
            Render(screen);
            Assert.Empty(screen.CombatEffects.Active);
            Assert.Empty(screen.CombatTrajectories);
            Assert.Equal("Ready", screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher").StatusBarText);
            screen.OnDeactivated();
        }
        if (hits < 3)
            Assert.Equal(450 - hits * 150, loaded.Objects.Single(o => o.ObjectId == CombatSessionFixture.Target).HullCombat!.CurrentHp);
        else
        {
            Assert.DoesNotContain(loaded.Objects, o => o.ObjectId == CombatSessionFixture.Target);
            Assert.Single(loaded.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
        }
        // A new post-load fact is displayed once and expires on monotonic UI time.
        long now = 0;
        var effects = new CombatEffectStore(() => now);
        effects.Reset(loaded.CombatImpacts);
        var newImpact = before.CombatImpacts[^1] with { EventId = hits + 1 };
        effects.Receive([newImpact]);
        Assert.Single(effects.Active);
        now = Stopwatch.Frequency * 2;
        effects.Receive([newImpact]);
        Assert.Empty(effects.Active);
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
    }
}
