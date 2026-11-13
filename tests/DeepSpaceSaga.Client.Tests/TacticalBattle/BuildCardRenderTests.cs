using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BuildCardRenderTests
{
    [Fact]
    public async Task Battle_log_replaces_selected_build_card()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire"); using var locale = Localization.UseLocaleForTests("Russian");
        scene.Screen.HoverUnitId = null; using var image = scene.Render();
        var r = scene.Screen.Layout.Rects["battle-log"];
        Assert.Equal(new SKRect(1040, 284, 1274, 714), r);
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "Журнал боя");
        Assert.DoesNotContain("build", scene.Screen.Layout.Rects.Keys);
        Assert.Contains("перемещение завершено", scene.Screen.LogTab.Panel.Text);
        Assert.Contains(scene.Screen.PanelRects, raw => raw.Top == r.Top * 1.5f && raw.Bottom == r.Bottom * 1.5f);
        foreach (var label in scene.Screen.Layout.Rects.Where(p => p.Key.StartsWith("label:", StringComparison.Ordinal))) Assert.False(label.Value.IntersectsWith(r), label.Key);
    }
    [Fact]
    public async Task Hover_heavy_and_enemy_keep_correct_cards()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire"); using var locale = Localization.UseLocaleForTests("Russian");
        scene.Screen.HoverUnitId = "Вера"; using var own = scene.Render();
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "ПОД КУРСОРОМ · ПИЛОТ");
        Assert.True(scene.Screen.Layout.Rects["hover"].Bottom < scene.Screen.Layout.Rects["battle-log"].Top);
        scene.Screen.HoverUnitId = "Таг-6"; using var enemy = scene.Render(); Assert.False(scene.Screen.Layout.Rects.ContainsKey("build"));
        Assert.Equal(104, scene.Screen.Layout.Rects["hover"].Height); Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "ПОД КУРСОРОМ · ПРОТИВНИК");
    }
    [Theory]
    [InlineData("light", 9)]
    [InlineData("heavy", 8)]
    [InlineData("thug", 8)]
    [InlineData("unknown", 9)]
    public void Silhouette_per_frame(string frame, int count)
    {
        Assert.Equal(count, BuildCard.Silhouette(frame).Count);
        if (frame == "unknown") Assert.Equal(BuildCard.Silhouette("light"), BuildCard.Silhouette(frame));
        using var bitmap = new SKBitmap(240, 180); using var c = new SKCanvas(bitmap); c.Clear(SKColors.Transparent);
        Polygon(c, BuildCard.Silhouette(frame).Select(p => new SKPoint(120 + p.X, 10 + p.Y)), SKColor.Parse("#122024"), BoxEdge);
        Assert.Equal(SKColor.Parse("#122024"), bitmap.GetPixel(120, 120));
        if (frame == "heavy") Assert.Equal(SKColor.Parse("#122024"), bitmap.GetPixel(220, 90));
    }
    [Fact]
    public async Task Damaged_borders_and_empty_slot_are_literal()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire"); using var locale = Localization.UseLocaleForTests("Russian");
        var f = scene.Screen.CaptureFrame(1920, 1080)!; var u = f.Battle.Units.Single(u => u.Id == "Марта");
        var changed = u with
        {
            Slots = u.Slots.Select(s => s.Kind == TacticalSlotKind.Weapon ? s with { State = TacticalSlotState.Jammed }
            : s.Kind == TacticalSlotKind.Engine && s.Index == 0 ? s with { State = TacticalSlotState.Damaged, Factor = .5 } : s).ToImmutableArray()
        };
        var model = BuildCardModel.From(f.Battle with { Units = f.Battle.Units.Replace(u, changed) }, null, u.Id)!;
        var r = BuildCard.Bounds(f); using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap); canvas.Clear(Bg); canvas.Scale(1.5f);
        var old = Recording; var layout = new BattleLayout(); Recording = layout;
        try { BuildCard.Draw(canvas, model, r, PilotPortraitCatalog.Default); } finally { Recording = old; }
        foreach (var cell in model.Cells.Where(c => c.Alert))
        {
            var box = BuildCard.CellBounds(model, cell, r);
            Assert.InRange(Fraction(bitmap, new(box.Left + 7, box.Top - .35f, box.Right - 7, box.Top + .35f), Red), .5, 1);
        }
        Assert.Contains(model.Cells, c => c.Detail == "заклинило"); Assert.Contains(model.Cells, c => c.Detail == "повреждён ×0,5");
        Save(bitmap, "damaged");
    }
    private static double Fraction(SKBitmap b, SKRect r, SKColor color)
    {
        int total = 0, same = 0;
        for (int y = (int)Math.Ceiling(r.Top * 1.5f); y < (int)Math.Ceiling(r.Bottom * 1.5f); y++)
            for (int x = (int)Math.Ceiling(r.Left * 1.5f); x < (int)Math.Ceiling(r.Right * 1.5f); x++)
            { var p = b.GetPixel(x, y); total++; if (Math.Abs(p.Red - color.Red) <= 40 && Math.Abs(p.Green - color.Green) <= 40 && Math.Abs(p.Blue - color.Blue) <= 40) same++; }
        return same / (double)total;
    }
    private static void Save(SKBitmap image, string name)
    {
        string? root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT"); if (root is null) return;
        using var png = image.Encode(SKEncodedImageFormat.Png, 100); File.WriteAllBytes(Path.Combine(root, "build-" + name + ".png"), png.ToArray());
    }
}
