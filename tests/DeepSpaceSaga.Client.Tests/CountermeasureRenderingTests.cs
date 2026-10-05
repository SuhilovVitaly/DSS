using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class CountermeasureRenderingTests
{
    [Fact]
    public void Countermeasure_route_matches_snapshot_and_is_selectable_marker()
    {
        var op = new WeaponOperatorSnapshot("crew", "Crew", WeaponSkillType.CountermeasureDefense, 50, 30, 30);
        var route = new TorpedoRoute(0, 1, TorpedoRoutePhase.Straight, true, [new(0, 0, 90, 12, 0, 1000)]);
        var pr = new ObjectMotionSnapshot("pr", 0, 0, 12, 90, RenderObjectType: SpaceObjectType.Countermeasure,
            Countermeasure: new("player", "launcher", "target", CountermeasurePhase.Guiding, route, 0, 500, new(op, 30), []));
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0), pr, new("target", 120, 0, 0, 0)], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.True(GameSessionScreen.HasCombatMarker(pr));
        var geometry = screen.DefenseGeometry["pr"];
        Assert.Equal(120, geometry.Intercept!.Value.X, 5);
        Assert.Equal(0, geometry.Intercept!.Value.Y, 5);
        screen.OnDeactivated();
    }
    [Fact]
    public void Miss_text_expires_in_real_time_and_never_replays()
    {
        long now = 0;
        var store = new CombatEffectStore(() => now);
        var entry = new CombatJournalEntry(1, 1000, CombatEventType.Miss, "ship", "torpedo", "pr", 0, 0, 500, 801);
        store.ReceiveJournal([entry], now);
        Assert.Single(store.Results);
        Assert.Empty(store.Active);
        now = Stopwatch.Frequency * 2;
        store.ReceiveJournal([entry], now);
        Assert.Empty(store.Results);
        store.ResetJournal([entry]);
        store.ReceiveJournal([entry], now);
        Assert.Empty(store.Results);
    }
}
