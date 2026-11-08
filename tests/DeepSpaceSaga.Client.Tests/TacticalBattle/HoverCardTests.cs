using DeepSpaceSaga.Client.Tests.TacticalBattle.View;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class HoverCardTests
{
    [Theory]
    [InlineData("Таг-6", "ПОД КУРСОРОМ · ПРОТИВНИК", "Обрез I · 40–50 · дальн. 110 · иниц. 41")]
    [InlineData("Олег", "ПОД КУРСОРОМ · ПИЛОТ", "Копьё I · 40–60 · точн. 50 · иниц. 50")]
    public async Task Mouse_hover_changes_only_hover_projection(string id, string heading, string details)
    {

        await using var s = await Ep0019Scenes.Create("2-fire"); using var locale = Localization.UseLocaleForTests("Russian"); using var initial = s.Render();
        var f = s.Screen.CaptureFrame(1920, 1080)!; var p = f.View.Project(f.Battle.Units.Single(u => u.Id == id).Pose);
        var targets = s.Screen.BuildFireRequest().Targets;
        s.Screen.OnMouseMove(p.X * 1.5f, p.Y * 1.5f);
        Assert.Equal(id, s.Screen.HoverUnitId);
        using var hovered = s.Render(); Assert.Contains(s.Screen.Layout.Texts, t => t.Text == heading); Assert.Contains(s.Screen.Layout.Texts, t => t.Text == details);
        Assert.Equal(targets.ToArray(), s.Screen.BuildFireRequest().Targets.ToArray()); Assert.Equal(f.SelectedDroneId, s.Screen.CaptureFrame(1920, 1080)!.SelectedDroneId);
        s.Screen.OnMouseMove(0, 0); using var clear = s.Render(); Assert.False(s.Screen.Layout.Rects.ContainsKey("hover"));
    }
}
