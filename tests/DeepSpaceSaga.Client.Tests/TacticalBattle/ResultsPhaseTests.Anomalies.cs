using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed partial class ResultsPhaseTests
{
    [Fact]
    public async Task Absorbed_shot_result_and_callout()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var state = await AnomalySceneSearch.First("absorbed-result", s => s.Shots.FirstOrDefault(shot =>
            s.Units.Any(u => u.Id == shot.ShooterId && u.Side == s.PlayerSide))?.Absorbed == true);
        var result = new TacticalBattleResults(state, state.Shots, [], 0, Skipped: true) { SkippedAtMs = 10000 };
        var camera = new TacticalBattleCamera(); camera.Update(state with { Phase = TacticalBattlePhase.Fire }, 1280, 720);
        var frame = new TacticalBattleFrame(state, camera.View, new(1040, 6, 1274, 366), 1, false, "", Salvo: result.Capture(10000));
        var shot = PilotCardModel.ResultShot(frame)!; Assert.True(shot.Absorbed);
        var text = PhasePanel.AbsorbedText(shot)!.Value;
        Assert.Equal("ПОГЛОЩЁН", text.Title); Assert.Equal("Термоаномалия · " + Math.Round(shot.Absorption * 100) + "% пути", text.Detail);
        var rendered = AnomalySceneSearch.Render(frame, "absorbed-result"); using var bitmap = rendered.Bitmap;
        Assert.DoesNotContain(rendered.Layout.Texts, t => t.Text == M("DecoyOfTarget"));
        Assert.Contains(rendered.Layout.Texts, t => t.Text == text.Title); Assert.Contains(rendered.Layout.Texts, t => t.Text == text.Detail);
        Assert.Contains(rendered.Layout.Texts, t => t.Text == "Поглощён · " + PilotCardModel.Name(state.Units.Single(u => u.Id == shot.ShooterId)));
        Assert.Equal("поглощён", PilotCardModel.Project(frame).Single(c => c.UnitId == shot.ShooterId).Chip);
        var first = AnomalyPresentation.Segments(state, shot.From, shot.To)[0];
        double t = (first.T0 + first.T1) / 2;
        var expected = frame.View.Project(shot.From.X + (shot.To.X - shot.From.X) * t, shot.From.Y + (shot.To.Y - shot.From.Y) * t);
        Assert.Equal(expected, AnomalyPresentation.AbsorptionPoint(frame, shot));
        var ring = rendered.Layout.Rects["absorption-flash:" + shot.ShooterId]; Assert.Equal(20, ring.Width); Assert.Equal(expected.X, ring.MidX);
        foreach (var absorbed in frame.Salvo!.Flights.Where(f => f.Shot.Absorbed))
        {
            var box = rendered.Layout.Rects["absorbed:" + absorbed.Shot.ShooterId]; box.Inflate(10, 10);
            Assert.DoesNotContain(state.Units.Where(u => TacticalBattleMapRenderer.HasDecoy(frame, u)), u => box.Contains(DecoyPoint(frame.View.Project(u.Pose), u.Pose.Course)));
        }
        Assert.DoesNotContain(TacticalBattleMapRenderer.DecoyMisses(frame), s => s.Absorbed);
        Assert.Contains("поглощён аномалией", BattleLogFormatter.Format(state.Log.First(e => e.Kind == TacticalBattleLogKind.ShotAbsorbed)));
        var flightResult = result with { Skipped = false };
        var flight = frame with { Battle = flightResult.Project(0), Salvo = flightResult.Capture(0) };
        Assert.DoesNotContain(PilotCardModel.Project(flight), c => c.Chip == "поглощён");
    }

    [Fact]
    public async Task Queue_shows_effective_chance()
    {
        var state = await AnomalySceneSearch.First("absorbed-queue", s => s.Shots.Any(shot => shot.Absorbed));
        var results = new TacticalBattleResults(state, state.Shots, [], 0);
        var camera = new TacticalBattleCamera(); camera.Update(state with { Phase = TacticalBattlePhase.Fire }, 1280, 720);
        var frame = new TacticalBattleFrame(results.Project(350), camera.View, new(1040, 6, 1274, 382), 1, false, "", Salvo: results.Capture(350));
        foreach (var flight in frame.Salvo!.Flights)
        {
            var original = state.Shots.Single(s => s.ShooterId == flight.Shot.ShooterId);
            bool hideDecoy = original.Decoyed && state.Units.Single(u => u.Id == original.TargetId).Side != state.PlayerSide;
            double expected = hideDecoy ? original.ChanceWithoutDecoy ?? original.Chance : original.Chance;
            Assert.Equal(expected, PhasePanel.QueueChance(frame, flight.Shot));
        }
        var shot = frame.Salvo.Flights.First().Shot with { Chance = .25, Absorption = .5, Decoyed = false };
        Assert.Equal(.25, PhasePanel.QueueChance(frame, shot)); // DTO Chance already includes absorption.
        var rendered = AnomalySceneSearch.Render(frame, "absorbed-queue"); using var bitmap = rendered.Bitmap;
        foreach (var flight in frame.Salvo.Flights)
            Assert.Contains(rendered.Layout.Texts, t => t.Text == Pct(PhasePanel.QueueChance(frame, flight.Shot)));
    }
}
