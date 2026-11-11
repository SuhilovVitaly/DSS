using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed partial class HoverCardTests
{
    [Theory]
    [InlineData(TacticalAnomalyKindCode.Heat, "Температурная")]
    [InlineData(TacticalAnomalyKindCode.Dust, "Пылевая")]
    public async Task Anomaly_hover_card(TacticalAnomalyKindCode kind, string name)
    {
        await using var scene = await Ep0019Scenes.Create("1-move"); using var locale = Localization.UseLocaleForTests("Russian"); using (var initial = scene.Render()) { }
        var before = scene.Screen.CaptureFrame(1920, 1080)!;
        var orders = scene.Screen.BuildMoveRequest().Orders;
        var point = before.Battle.Anomalies.Where(a => a.Kind == kind).SelectMany(a => a.Clumps).Select(c => new TacticalPoseSnapshot(c.X, c.Y, 0))
            .Select(p => before.View.Project(p)).First(p => p.X > 202 && p.X < 1034 && p.Y > 6 && p.Y < 714
                && before.Battle.Units.All(u => SKPoint.Distance(p, TacticalBattleMapRenderer.HoverPoint(before, u)) > 25)
                && AnomalyPresentation.HoverKind(before with { HoverPoint = p, HoverUnitId = null }) == kind);
        scene.Screen.OnMouseMove(point.X * 1.5f, point.Y * 1.5f);
        using var hovered = scene.Render(); var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.Null(scene.Screen.HoverUnitId); var model = HoverCard.Anomaly(frame)!;
        Assert.Equal("ПОД КУРСОРОМ · АНОМАЛИЯ", model.Heading); Assert.Equal(name, model.Name); Assert.Equal(3, model.Lines.Count);
        foreach (string text in new[] { model.Heading, model.Name }.Concat(model.Lines)) Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == text);
        Assert.Equal(158, scene.Screen.Layout.Rects["hover"].Top);
        Assert.Contains(scene.Screen.PanelRects, r => r.Top == (frame.Panel.Bottom + 6) * 1.5f);
        Assert.Equal(orders.ToArray(), scene.Screen.BuildMoveRequest().Orders.ToArray());
        var unit = before.Battle.Units.First(u => u.Kind == TacticalUnitKind.Drone);
        Assert.Null(HoverCard.Anomaly(frame with { HoverUnitId = unit.Id }));
        scene.Screen.OnMouseMove(0, 0); using var clear = scene.Render(); Assert.False(scene.Screen.Layout.Rects.ContainsKey("hover"));
    }

    [Fact]
    public async Task Heat_has_priority_when_both_zones_overlap()
    {
        await using var scene = await Ep0019Scenes.Create("1-move"); var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        var clump = new TacticalClumpSnapshot(0, 0, 100, 10, 10, 0);
        frame = frame with { Battle = frame.Battle with { Anomalies = [new("h", TacticalAnomalyKindCode.Heat, [clump]), new("d", TacticalAnomalyKindCode.Dust, [clump])] }, HoverUnitId = null, HoverPoint = frame.View.Project(0, 0) };
        Assert.Equal(TacticalAnomalyKindCode.Heat, AnomalyPresentation.HoverKind(frame));
    }
}
