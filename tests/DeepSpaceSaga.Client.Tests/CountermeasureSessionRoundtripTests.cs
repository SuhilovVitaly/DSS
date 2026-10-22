using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class CountermeasureSessionRoundtripTests
{
    [Fact]
    public async Task Local_session_resets_selection_preserves_point_history_and_does_not_replay_effects()
    {
        await using var f = await CombatSessionFixture.Create(defense: true);
        await f.Connection.SendCommandAsync(new("counter-save", 1, CombatSessionFixture.Player, "MOD-PLAYER-COUNTERMEASURE-01", DefenseCommandTypes.Launch,
            TargetWorldX: 150, TargetWorldY: 0, ProfileId: "wide"));
        var snapshot = await f.Advance(500); var pr = Assert.Single(snapshot.Objects, o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile);
        await f.Connection.SetObjectInteractionStateAsync(null, pr.ObjectId);
        snapshot = f.Engine.CaptureSnapshot();
        var loaded = await f.SaveAndRestore();
        Assert.Null(loaded.SelectedObjectId);
        Assert.Equal(JsonSerializer.Serialize(snapshot.CombatJournal), JsonSerializer.Serialize(loaded.CombatJournal));
        Assert.Equal(JsonSerializer.Serialize(pr.OwnProjectile), JsonSerializer.Serialize(loaded.Objects.Single(o => o.ObjectId == pr.ObjectId).OwnProjectile));
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        buffer.Update(loaded);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.Null(screen.SelectedObjectId);
        Assert.Empty(screen.CombatEffects.HitResults);
        Assert.Empty(screen.CombatEffects.PointEffects);
        snapshot = await f.Advance(2000); Assert.Contains(snapshot.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
        // Compare the original engine continuation under the restored presentation baseline.
        buffer.Update(snapshot with
        {
            SnapshotSequence = loaded.SnapshotSequence + 1,
            CombatReports = snapshot.CombatReports! with { BaselineSequence = loaded.CombatReports!.BaselineSequence }
        });
        screen.Render(canvas, 1280, 720);
        Assert.NotEmpty(screen.CombatEffects.PointEffects);
        now = Stopwatch.Frequency * 2;
        screen.Render(canvas, 1280, 720);
        Assert.Empty(screen.CombatEffects.HitResults);
        screen.OnDeactivated();
        loaded = await f.SaveAndRestore();
        var secondBuffer = new SnapshotBuffer(() => now); secondBuffer.Update(loaded);
        var secondScreen = new GameSessionScreen(secondBuffer, new LinearMotionPredictor(), timestampProvider: () => now);
        secondScreen.Render(canvas, 1280, 720);
        Assert.Empty(secondScreen.CombatEffects.HitResults);
        Assert.Empty(secondScreen.CombatEffects.PointEffects);
        secondScreen.OnDeactivated();
    }
}
