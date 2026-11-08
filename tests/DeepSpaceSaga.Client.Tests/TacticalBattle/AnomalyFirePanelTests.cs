using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class AnomalyFirePanelTests
{
    [Fact]
    public async Task Shot_plan_shows_effective_chance_and_absorption()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var state = await AnomalySceneSearch.First("heat-plan", s => s.Phase == TacticalBattlePhase.Fire && s.Targets.Any(g => g.List.Any(c => c.Absorption >= .1)));
        var group = state.Targets.First(g => g.List.Any(c => c.Absorption >= .1)); var candidate = group.List.First(c => c.Absorption >= .1);
        var orders = new TacticalBattleFireOrders(); orders.Observe(state, group.DroneId); Assert.True(orders.Select(group.DroneId)); Assert.True(orders.Choose(candidate.TargetId));
        var frame = AnomalySceneSearch.Frame(state, group.DroneId) with { FirePlan = orders.Capture() };
        Assert.Equal(1, state.Seed); Assert.Equal(4, state.Turn); Assert.Equal("Олег", group.DroneId);
        Assert.Equal(.355, candidate.Absorption, 12); Assert.Equal(.400664818770654, candidate.Chance, 12);
        string expected = Pct(candidate.Chance) + " · поглощение " + Math.Round(candidate.Absorption * 100) + "%";
        Assert.Equal(expected, PhasePanel.FireCaption(candidate));
        var rendered = AnomalySceneSearch.Render(frame, "heat-plan"); using var bitmap = rendered.Bitmap;
        Assert.Contains(rendered.Layout.Texts, t => t.Text == expected);
        Assert.Contains(rendered.Layout.Texts, t => t.Text == "поглощение " + Math.Round(candidate.Absorption * 100) + "%");
        Assert.Contains(PilotCardModel.Project(frame), p => p.UnitId == group.DroneId && p.Chip.EndsWith(Pct(candidate.Chance), StringComparison.Ordinal));
        var from = state.Units.Single(u => u.Id == group.DroneId); var to = state.Units.Single(u => u.Id == candidate.TargetId);
        var segment = AnomalyPresentation.Segments(state, from.Pose, to.Pose).OrderByDescending(s => s.T1 - s.T0).First();
        var a = frame.View.Project(from.Pose); var b = frame.View.Project(to.Pose); int heat = 0;
        double fullLength = SKPoint.Distance(a, b), length = fullLength * (segment.T1 - segment.T0);
        int dashes = (int)Math.Floor((length - 3) / 9.0) + 1;
        Assert.True(dashes >= 5, $"Need five visible6/3dash centers; length={length}");
        for (int index = 0; index < 5; index++)
        {
            // Probe the middle of five on-dashes, not gaps of the specified6/3 pattern.
            double offset = 3 + 9 * Math.Round(index * (dashes - 1) / 4.0);
            double t = segment.T0 + offset / fullLength;
            var p = a + new SKPoint((float)((b.X - a.X) * t), (float)((b.Y - a.Y) * t));
            var color = bitmap.GetPixel((int)Math.Round(p.X * 1.5), (int)Math.Round(p.Y * 1.5));
            if (Math.Abs(color.Red - Heat.Red) <= 40 && Math.Abs(color.Green - Heat.Green) <= 40 && Math.Abs(color.Blue - Heat.Blue) <= 40) heat++;
        }
        Assert.True(heat >= 3, $"Heat dash probes {heat}/5 seed{state.Seed} turn{state.Turn}");
    }

    [Fact]
    public async Task Dust_drone_panel_and_marks()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var state = await AnomalySceneSearch.First("dust-plan", s => s.Phase == TacticalBattlePhase.Fire && s.Units.Any(u => u.Side == s.PlayerSide && u.Kind == TacticalUnitKind.Drone && u.State == TacticalUnitState.Air && u.InDust));
        var unit = state.Units.First(u => u.Side == state.PlayerSide && u.Kind == TacticalUnitKind.Drone && u.State == TacticalUnitState.Air && u.InDust);
        var orders = new TacticalBattleFireOrders(); orders.Observe(state, unit.Id); Assert.True(orders.Select(unit.Id));
        Assert.Null(orders.Capture().Selected!.TargetId);
        Assert.False(orders.Choose(state.Units.First(u => u.Side != state.PlayerSide).Id)); // guard before Draw
        var frame = AnomalySceneSearch.Frame(state, unit.Id);
        var rendered = AnomalySceneSearch.Render(frame, "dust-plan"); using var bitmap = rendered.Bitmap;
        Assert.Equal(("ОГОНЬ НЕВОЗМОЖЕН", "Дрон в пылевой аномалии", "Пыль запрещает стрельбу до выхода из неё"), PhasePanel.DustText);
        foreach (string text in new[] { PhasePanel.DustText.Tag, PhasePanel.DustText.Message, PhasePanel.DustText.Detail, "в пыли: без огня" })
            Assert.Contains(rendered.Layout.Texts, t => t.Text == text);
        Assert.InRange(RasterProbe.Fraction(bitmap, new(1049.5f, 94, 1053, 108), PurpleDark), .9, 1);
        Assert.Equal("в пыли", PilotCardModel.Project(frame).Single(p => p.UnitId == unit.Id).Chip);
        Assert.Equal(0, TacticalBattleMapRenderer.TargetCount(frame, unit));
        Assert.True(orders.ToggleDecoy(unit.Id)); Assert.True(orders.BuildTargets().Single(t => t.DroneId == unit.Id).Decoy);
        Assert.True(orders.ToggleEvade(unit.Id)); Assert.False(orders.BuildTargets().Single(t => t.DroneId == unit.Id).Decoy);
        Assert.True(orders.BuildTargets().Single(t => t.DroneId == unit.Id).Evade);
    }

    [Fact]
    public async Task Move_end_in_dust_callout()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var state = await AnomalySceneSearch.First("move-dust", s => s.Phase == TacticalBattlePhase.Move && s.Suggestions.Any(o =>
        {
            var unit = s.Units.Single(u => u.Id == o.DroneId);
            var end = TacticalGeometry.Integrate(new(unit.Pose.X, unit.Pose.Y, unit.Pose.Course), o.Speed, o.Turn, s.Rules.Substeps);
            return AnomalyPresentation.Inside(s, TacticalAnomalyKind.Dust, new(end.X, end.Y, end.Course));
        }));
        var frame = AnomalySceneSearch.Frame(state, state.Suggestions.First(o =>
        {
            var unit = state.Units.Single(u => u.Id == o.DroneId);
            var end = TacticalGeometry.Integrate(new(unit.Pose.X, unit.Pose.Y, unit.Pose.Course), o.Speed, o.Turn, state.Rules.Substeps);
            return AnomalyPresentation.Inside(state, TacticalAnomalyKind.Dust, new(end.X, end.Y, end.Course));
        }).DroneId, movement: true);
        var rendered = AnomalySceneSearch.Render(frame, "move-dust"); using var bitmap = rendered.Bitmap;
        Assert.Contains(rendered.Layout.Texts, t => t.Text == "Конец пути в пыли: без огня");
        Assert.True(AnomalyPresentation.Inside(state, TacticalAnomalyKind.Dust, frame.Movement!.SelectedOrder!.Path[^1]));
    }

    [Fact]
    public async Task Dust_summary_counts_evasion_before_dust_and_preserves_decoys()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var scene = await Ep0019Scenes.Create("1-move"); var own = scene.Screen.Flow.State!.Units.First(u => u.Kind == TacticalUnitKind.Drone);
        TacticalFirePlanRow Row(string id, bool dust, bool evade, string? target) => new(own with { Id = id, InDust = dust }, new(id, [], target), target) { Evade = evade };
        var plan = new TacticalFirePlan([Row("a", false, false, "target"), Row("b", true, true, null), Row("c", true, false, null), Row("d", false, false, null)], "c");
        Assert.Equal("1 стреляет · 1 уклоняется · 1 в пыли · 1 без цели", PhasePanel.FireSummary(plan));
        Assert.Equal("1 стреляет · 1 уклоняется · 1 в пыли · 1 без цели · 1 ловушка", PhasePanel.FireSummary(plan with { Rows = plan.Rows.SetItem(2, plan.Rows[2] with { Decoy = true }) }));
    }
}
