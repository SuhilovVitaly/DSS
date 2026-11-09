using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed partial class ResultsPhaseTests
{
    [Fact]
    public async Task Enemy_decoy_is_revealed_only_in_results()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        var before = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.Empty(before.Battle.DecoysLaunched);
        scene.Fixture.ManualAnimation = true;
        await scene.Fixture.Route(ScreenEvent.FireTacticalBattle);
        var actual = scene.Screen.Flow.State!;
        string enemy = actual.DecoysLaunched.First(id => actual.Units.Single(u => u.Id == id).Side != actual.PlayerSide);
        var original = actual.Shots.First(s => s.TargetId == enemy && s.Decoyed);
        var flight = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.True(flight.Salvo!.InFlight);
        Assert.False(TacticalBattleMapRenderer.HasDecoy(flight, flight.Battle.Units.Single(u => u.Id == enemy)));
        Assert.DoesNotContain(enemy, flight.Salvo.DecoysLaunched);
        Assert.DoesNotContain(enemy, flight.Battle.DecoysLaunched);
        Assert.Equal(2, flight.Battle.Units.Single(u => u.Id == enemy).DecoyCharges);
        var shown = flight.Salvo.Flights.Single(f => f.Shot.ShooterId == original.ShooterId).Shot;
        Assert.False(shown.Decoyed);
        Assert.Equal(original.ChanceWithoutDecoy, shown.Chance);
        Assert.DoesNotContain(flight.Battle.Log, e => e.Kind == TacticalBattleLogKind.DecoyLaunched && e.ActorId == enemy);
        scene.Screen.HoverUnitId = enemy;
        using (var bitmap = scene.Render()) { }
        Assert.DoesNotContain(scene.Screen.Layout.Texts, t => t.Text == M("DecoyChip"));
        scene.Screen.OnKeyDown(Key.Enter);
        var result = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.False(result.Salvo!.InFlight);
        Assert.Contains(enemy, result.Salvo.DecoysLaunched);
        Assert.True(TacticalBattleMapRenderer.HasDecoy(result, result.Battle.Units.Single(u => u.Id == enemy)));
        Assert.Equal(1, result.Battle.Units.Single(u => u.Id == enemy).DecoyCharges);
        using (var bitmap = scene.Render()) { }
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == M("DecoyChip"));
        var replay = scene.Screen.Layout.Rects["button:replay"];
        scene.Screen.OnMouseDown(replay.MidX * 1.5f, replay.MidY * 1.5f, MouseButton.Left);
        Assert.DoesNotContain(enemy, scene.Screen.CaptureFrame(1920, 1080)!.Salvo!.DecoysLaunched);
        Assert.Equal(1, scene.Fixture.Proxy.Fires);
        Assert.Equal(1, scene.Fixture.Engine.TacticalBattleSessionForTests!.Battle.Drones.Single(d => d.Id == enemy).DecoyCharges);
        Assert.Same(actual, scene.Screen.Flow.State);
    }

    [Fact]
    public async Task Own_decoy_is_visible_during_flight_with_paid_charge()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        string own = scene.Screen.CaptureFrame(1920, 1080)!.FirePlan!.SelectedDroneId!;
        scene.Screen.OnKeyDown(Key.D);
        scene.Fixture.ManualAnimation = true;
        await scene.Fixture.Route(ScreenEvent.FireTacticalBattle);
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.Contains(own, frame.Salvo!.DecoysLaunched);
        Assert.True(TacticalBattleMapRenderer.HasDecoy(frame, frame.Battle.Units.Single(u => u.Id == own)));
        Assert.Equal(1, frame.Battle.Units.Single(u => u.Id == own).DecoyCharges);
        Assert.Contains(frame.Battle.Log, e => e.Kind == TacticalBattleLogKind.DecoyLaunched && e.ActorId == own);
    }

    [Fact]
    public async Task Decoy_marks_disappear_after_results()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        scene.Screen.OnKeyDown(Key.D);
        scene.Fixture.ManualAnimation = true;
        await scene.Fixture.Route(ScreenEvent.FireTacticalBattle);
        scene.Screen.OnKeyDown(Key.Enter);
        var resolved = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.NotEmpty(resolved.Salvo!.DecoysLaunched);
        scene.Screen.OnKeyDown(Key.Enter);
        var continued = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.Null(continued.Salvo);
        Assert.DoesNotContain(continued.Battle.Units, u => TacticalBattleMapRenderer.HasDecoy(continued, u));
        Assert.Equal(1, scene.Fixture.Proxy.Fires);
    }

    [Fact]
    public async Task Decoyed_miss_gets_callout_and_panel_line()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        using var locale = Localization.UseLocaleForTests("Russian");
        var f = scene.Screen.CaptureFrame(1920, 1080)!;
        var own = f.FirePlan!.Selected!.Drone;
        var enemy = f.Battle.Units.Single(u => u.Id == f.FirePlan.Selected.TargetId);
        TacticalShotSnapshot Shot(TacticalUnitSnapshot a, TacticalUnitSnapshot b) => new(3, a.Id, b.Id,
            a.Pose, b.Pose, .205, false, 0, false, false, false)
        { Decoyed = true, ChanceWithoutDecoy = .47 };
        var shots = new[] { Shot(own, enemy), Shot(enemy, own) };
        var state = f.Battle with { Shots = [.. shots], DecoysLaunched = [own.Id, enemy.Id], BeforeSalvoUnits = f.Battle.Units };
        var presentation = new TacticalBattleResults(state, state.Shots, [], 0);
        f = f with { Battle = state, FirePlan = null, Movement = null, Salvo = presentation.Capture(10000) };
        Assert.Equal(new[] { own.Id, enemy.Id }, TacticalBattleMapRenderer.DecoyMisses(f).Select(s => s.ShooterId));
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(1.5f);
        var layout = new BattleLayout(); Recording = layout;
        try
        {
            TacticalBattleMapRenderer.Draw(canvas, f);
            new PhasePanel().Draw(canvas, f, layout);
        }
        finally { Recording = null; }
        Assert.Contains(layout.Texts, t => t.Text == "Ловушка · промах " + PilotCardModel.Name(own) && t.Color == Green);
        Assert.Contains(layout.Texts, t => t.Text == "Ловушка · промах " + PilotCardModel.Name(enemy) && t.Color == Green);
        Assert.Equal(2, layout.Rects.Keys.Count(k => k.StartsWith("decoy-callout:", StringComparison.Ordinal)));
        Assert.Contains(layout.Texts, t => t.Text == "ловушка цели" && t.Color == Green);
        Assert.Contains(layout.Texts, t => t.Text == Pct(.47) + " → " + Pct(.205) && t.Color == Green);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!, "decoy-results.png"), png.ToArray());
        var flight = f with { Battle = presentation.Project(300), Salvo = presentation.Capture(300) };
        using var queueBitmap = new SKBitmap(1920, 1080);
        using var queueCanvas = new SKCanvas(queueBitmap); queueCanvas.Scale(1.5f);
        var queueLayout = new BattleLayout(); Recording = queueLayout;
        try { new PhasePanel().Draw(queueCanvas, flight, queueLayout); }
        finally { Recording = null; }
        Assert.Contains(queueLayout.Texts, t => t.Text == Pct(.205) && t.Color == Green);
        Assert.Contains(queueLayout.Texts, t => t.Text == Pct(.47) && t.Color != Green);
    }

    [Fact]
    public async Task Zero_shot_decoy_has_results_without_fake_projectile_or_miss()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        using var locale = Localization.UseLocaleForTests("Russian");
        foreach (var drone in scene.Fixture.Engine.TacticalBattleSessionForTests!.Battle.Drones.Where(d => d.Side == "B")) drone.Jammed = true;
        for (int i = 0; i < 4; i++)
        {
            using (var bitmap = scene.Render()) { }
            scene.Screen.OnMouseDown(30, (20 + 87 * i) * 1.5f, MouseButton.Left);
            using (var bitmap = scene.Render()) { }
            var hold = scene.Screen.Layout.Rects["button:hold"];
            scene.Screen.OnMouseDown(hold.MidX * 1.5f, hold.MidY * 1.5f, MouseButton.Left);
        }
        scene.Screen.OnKeyDown(Key.D);
        scene.Fixture.ManualAnimation = true;
        await scene.Fixture.Route(ScreenEvent.FireTacticalBattle);
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.False(frame.Salvo!.InFlight);
        Assert.Empty(frame.Salvo.Flights);
        Assert.Single(frame.Salvo.DecoysLaunched);
        using (var bitmap = scene.Render()) { }
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "огонь не ведётся");
        Assert.DoesNotContain(scene.Screen.Layout.Texts, t => t.Text == "ПРОМАХ");
        Assert.False(scene.Screen.Layout.Rects.ContainsKey("projectile"));
        scene.Screen.OnKeyDown(Key.Enter);
        Assert.Null(scene.Screen.CaptureFrame(1920, 1080)!.Salvo);
        Assert.Equal(1, scene.Fixture.Proxy.Fires);
    }
}
