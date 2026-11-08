using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PhaseFireInputTests
{
    [Fact]
    public async Task Fire_details_and_candidate_click_hold_fire()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var s = await Ep0019Scenes.Create("2-fire"); using var b = s.Render();
        foreach (string text in new[] { "Марта → Таг-4", "56%", "Дистанция 108,1", "Угол 16,1°", "2 стреляют · 2 без цели" })
            Assert.Contains(s.Screen.Layout.Texts, t => t.Text == text);
        var f = s.Screen.CaptureFrame(1920, 1080)!;
        var point = f.View.Project(f.Battle.Units.Single(u => u.Id == "Таг-6").Pose);
        s.Screen.OnMouseDown(point.X * 1.5f, point.Y * 1.5f, MouseButton.Left);
        Assert.Equal("Таг-4", s.Screen.CaptureFrame(1920, 1080)!.FirePlan!.Selected!.TargetId);
        s.Screen.OnMouseDown(1150 * 1.5f, 250 * 1.5f, MouseButton.Left);
        Assert.Null(s.Screen.CaptureFrame(1920, 1080)!.FirePlan!.Selected!.TargetId);
    }
}
