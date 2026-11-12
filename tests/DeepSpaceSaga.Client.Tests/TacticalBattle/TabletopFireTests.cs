using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopFireTests
{
    [Theory]
    [InlineData("2-fire-hover-own")]
    [InlineData("2-anomaly-absorption")]
    [InlineData("2-no-target")]
    [InlineData("2-evasion-plan")]
    public async Task Fire_templates_use_authoritative_targets_and_preserve_absorption(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap); canvas.Scale(1.5f);
        var frame = scene.Frame;
        var layout = new BattleLayout(); BattleDesign.Recording = layout;
        try { TacticalBattleMapRenderer.Draw(canvas, frame); }
        finally { BattleDesign.Recording = null; }
        var selected = frame.FirePlan!.Selected!;
        Assert.Equal(selected.Group.List.Length, layout.Rects.Keys.Count(k => k.StartsWith("candidate:", StringComparison.Ordinal)));
        foreach (var candidate in selected.Group.List)
        {
            var rect = layout.Rects["candidate:" + candidate.TargetId];
            var point = TacticalBattleMapRenderer.MarkerPoint(frame.View, frame.Battle.Units.Single(u => u.Id == candidate.TargetId));
            Assert.Equal(44, rect.Width, .001f); Assert.Equal(44, rect.Height, .001f);
            Assert.Equal(point.X, rect.MidX, .001f); Assert.Equal(point.Y, rect.MidY, .001f);
        }
        if (selected.TargetId is not null) Assert.Contains(SKColor.Parse("#FF4628"), bitmap.Pixels);
        if (selected.Group.List.Any(t => t.TargetId != selected.TargetId)) Assert.Contains(SKColor.Parse("#FF9A6A"), bitmap.Pixels);
        if (frame.FirePlan.Rows.Any(r => r.TargetId is not null)) Assert.Contains(SKColor.Parse("#D6CBA6"), bitmap.Pixels);
        if (name == "2-anomaly-absorption")
        {
            Assert.True(layout.Rects.ContainsKey("absorption:" + selected.Drone.Id));
            Assert.Contains(BattleDesign.Heat, bitmap.Pixels);
        }
    }
}
