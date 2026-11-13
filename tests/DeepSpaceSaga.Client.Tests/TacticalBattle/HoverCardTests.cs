using DeepSpaceSaga.Client.Tests.TacticalBattle.View;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed partial class HoverCardTests
{
    [Fact]
    public async Task Tabletop_hover_uses_larger_radius_outside_render_scope()
    {
        await using var scenes = await Ep0019Scenes.Create("1-move-no-hover", withoutAnomalies: true);
        using var catalog = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinCatalog.Load(
            Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleScreen(
            scenes.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        using var bitmap = new SkiaSharp.SKBitmap(1280, 720);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        var frame = screen.CaptureFrame(1280, 720)!;
        var unit = frame.Battle.Units.First(u => u.Kind == DeepSpaceSaga.Contracts.TacticalUnitKind.Drone &&
            u.Side == frame.Battle.PlayerSide);
        var point = frame.View.Project(unit.Pose);
        screen.OnMouseMove(point.X + 24 * frame.View.UnitScale, point.Y);
        Assert.Equal(unit.Id, screen.HoverUnitId);
        scenes.Screen.Render(canvas, 1280, 720);
        scenes.Screen.OnMouseMove(point.X + 24 * frame.View.UnitScale, point.Y);
        Assert.NotEqual(unit.Id, scenes.Screen.HoverUnitId);
        screen.OnMouseMove(point.X + 27 * frame.View.UnitScale, point.Y);
        Assert.NotEqual(unit.Id, screen.HoverUnitId);
    }

    [Theory]
    [InlineData("Таг-6", "ПОД КУРСОРОМ · ПРОТИВНИК", "Обрез I · 40–50 · дальн. 110 · иниц. 41")]
    [InlineData("Олег", "ПОД КУРСОРОМ · ПИЛОТ", "Корпус 55/55")]
    public async Task Mouse_hover_changes_only_hover_projection(string id, string heading, string details)
    {

        await using var s = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true); using var locale = Localization.UseLocaleForTests("Russian"); using var initial = s.Render();
        var f = s.Screen.CaptureFrame(1920, 1080)!; var p = f.View.Project(f.Battle.Units.Single(u => u.Id == id).Pose);
        var targets = s.Screen.BuildFireRequest().Targets;
        s.Screen.OnMouseMove(p.X * 1.5f, p.Y * 1.5f);
        Assert.Equal(id, s.Screen.HoverUnitId);
        using var hovered = s.Render(); Assert.Contains(s.Screen.Layout.Texts, t => t.Text == heading); Assert.Contains(s.Screen.Layout.Texts, t => t.Text == details);
        Assert.Equal(targets.ToArray(), s.Screen.BuildFireRequest().Targets.ToArray()); Assert.Equal(f.SelectedDroneId, s.Screen.CaptureFrame(1920, 1080)!.SelectedDroneId);
        s.Screen.OnMouseMove(0, 0); using var clear = s.Render(); Assert.False(s.Screen.Layout.Rects.ContainsKey("hover"));
    }
}
