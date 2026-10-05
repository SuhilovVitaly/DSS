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
    public async Task Local_session_preserves_live_selection_and_history_without_replaying_effects()
    {
        await using var f = await CombatSessionFixture.Create(defense: true);
        var snapshot = await f.Launch("defense-session");
        for (int i = 0; i < 1000 && snapshot.Objects.All(o => o.Countermeasure is null); i++) snapshot = await f.Advance(500);
        var pr = Assert.Single(snapshot.Objects, o => o.Countermeasure is not null);
        await f.Connection.SetObjectInteractionStateAsync(null, pr.ObjectId);
        snapshot = f.Engine.CaptureSnapshot();
        var loaded = await f.SaveAndRestore();
        Assert.Equal(pr.ObjectId, loaded.SelectedObjectId);
        Assert.Equal(JsonSerializer.Serialize(snapshot.CombatJournal), JsonSerializer.Serialize(loaded.CombatJournal));
        Assert.Equal(JsonSerializer.Serialize(pr.Countermeasure), JsonSerializer.Serialize(loaded.Objects.Single(o => o.ObjectId == pr.ObjectId).Countermeasure));
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        buffer.Update(loaded);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.Equal(pr.ObjectId, screen.SelectedObjectId);
        Assert.Empty(screen.CombatEffects.Results);
        Assert.Empty(screen.CombatEffects.Active);
        for (int i = 0; i < 100 && snapshot.CombatJournal.All(e => e.Roll is null); i++) snapshot = await f.Advance(500);
        Assert.Contains(snapshot.CombatJournal, e => e.Roll is not null);
        buffer.Update(snapshot with { SnapshotSequence = loaded.SnapshotSequence + 1 });
        screen.Render(canvas, 1280, 720);
        Assert.Single(screen.CombatEffects.Results);
        now = Stopwatch.Frequency * 2;
        screen.Render(canvas, 1280, 720);
        Assert.Empty(screen.CombatEffects.Results);
        screen.OnDeactivated();
        loaded = await f.SaveAndRestore();
        var secondBuffer = new SnapshotBuffer(() => now); secondBuffer.Update(loaded);
        var secondScreen = new GameSessionScreen(secondBuffer, new LinearMotionPredictor(), timestampProvider: () => now);
        secondScreen.Render(canvas, 1280, 720);
        Assert.Empty(secondScreen.CombatEffects.Results);
        Assert.Empty(secondScreen.CombatEffects.Active);
        secondScreen.OnDeactivated();
    }
}
