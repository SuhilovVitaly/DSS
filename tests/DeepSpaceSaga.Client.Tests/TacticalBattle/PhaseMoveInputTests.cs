using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PhaseMoveInputTests
{
    [Fact]
    public async Task Slider_drag_changes_issued_order_and_restore_ai()
    {

        await using var s = await Ep0019Scenes.Create("1-move"); using var locale = Localization.UseLocaleForTests("Russian"); using var b = s.Render();
        Assert.Contains(s.Screen.Layout.Texts, t => t.Text == "77,3");
        var slider = s.Screen.CardCommandBounds("slider:speed");
        Assert.False(slider.IsEmpty);
        s.Screen.OnMouseDown(slider.Left + 1, slider.MidY, MouseButton.Left);
        s.Screen.OnMouseUp(slider.Left + 1, slider.MidY, MouseButton.Left);
        Assert.Equal(50, Assert.Single(s.Screen.BuildMoveRequest().Orders).Speed);

        var restore = s.Screen.CardCommandBounds("ai");
        Assert.False(restore.IsEmpty);
        s.Screen.OnMouseDown(restore.MidX, restore.MidY, MouseButton.Left);
        Assert.InRange(Assert.Single(s.Screen.BuildMoveRequest().Orders).Speed, 77.26, 77.28);

        var submit = s.Screen.Layout.Rects["button:primary"];
        Assert.Equal(ScreenEvent.MoveTacticalBattle, s.Screen.OnMouseDown(submit.MidX * 1.5f, submit.MidY * 1.5f, MouseButton.Left));
    }
}
