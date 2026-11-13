using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("Battle localization")]
public sealed class TabletopPlateTests
{
    [Theory]
    [InlineData(TacticalUnitState.Air)]
    [InlineData(TacticalUnitState.Disabled)]
    public async Task Plate_moves_past_model_and_draws_a_leader(TacticalUnitState state)
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover", withoutAnomalies: true);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        var frame = scene.Frame;
        var unit = frame.Battle.Units.First(u => u.Kind == TacticalUnitKind.Drone) with { Pose = new(0, 0, 0), State = state };
        frame = frame with
        {
            Battle = frame.Battle with { Units = [unit], Paths = [] },
            View = new(1280, 720, 0, 0, 1),
            Movement = null,
            FirePlan = null
        };
        var layout = new BattleLayout();
        BattleDesign.Recording = layout;
        try { TacticalBattleMapRenderer.Draw(canvas, frame); }
        finally { BattleDesign.Recording = null; }
        var plate = layout.Rects["label:" + unit.Id];
        Assert.Equal(385, plate.Top);
        Assert.Equal(640, plate.MidX);
        Assert.False(plate.IntersectsWith(new SKRect(612, 337.6f, 668, 382.4f)));
        Assert.NotEqual(bitmap.GetPixel(637, 384), bitmap.GetPixel(640, 384));
        if (state == TacticalUnitState.Disabled)
        {
            Assert.DoesNotContain("plate:armor:" + unit.Id, layout.Rects.Keys);
            Assert.DoesNotContain("plate:hull:" + unit.Id, layout.Rects.Keys);
            Assert.Contains(layout.Texts, entry => entry.Text == PilotCardModel.Name(unit) && entry.Color == BattleDesign.Green);
            var cross = layout.Rects["disabled-cross:" + unit.Id];
            Assert.Equal(11, cross.Width);
            Assert.Equal(11, cross.Height);
            Assert.Equal(BattleDesign.Red, bitmap.GetPixel((int)cross.MidX, (int)cross.MidY));
        }
    }

    [Theory]
    [InlineData("1-move-no-hover")]
    [InlineData("2-fire-hover-own")]
    [InlineData("4-salvo-results")]
    public async Task Plates_show_current_values_and_only_damaged_boxes_are_highlighted(string sceneName)
    {
        await using var scene = await Ep0019Scenes.Create(sceneName, withoutAnomalies: true);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        var frame = scene.Frame;
        var layout = new BattleLayout();
        BattleDesign.Recording = layout;
        try { TacticalBattleMapRenderer.Draw(canvas, frame); }
        finally { BattleDesign.Recording = null; }
        foreach (var unit in frame.Battle.Units.Where(u => u.Kind == TacticalUnitKind.Drone && u.State == TacticalUnitState.Air))
        {
            Assert.True(layout.Rects.ContainsKey("label:" + unit.Id));
            var armor = layout.Rects["plate:armor:" + unit.Id];
            var hull = layout.Rects["plate:hull:" + unit.Id];
            Assert.Equal(22, armor.Width, .001f); Assert.Equal(13, armor.Height, .001f);
            Assert.Equal(armor.Right, hull.Left);
            Assert.Equal(27, layout.Rects["label:" + unit.Id].Height, .001f);
            Assert.Contains(layout.Texts, t => t.Text == PilotCardModel.Name(unit) && t.Color == SKColor.Parse("#F6EFD8"));
            Assert.DoesNotContain("plate:damage:armor:" + unit.Id, layout.Rects.Keys);
            Assert.DoesNotContain("plate:damage:hull:" + unit.Id, layout.Rects.Keys);
            Assert.False(layout.Rects.ContainsKey("callout:" + unit.Id));
        }
    }
}
