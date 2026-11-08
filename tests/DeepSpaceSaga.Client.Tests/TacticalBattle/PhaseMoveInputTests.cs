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
        Assert.Contains(s.Screen.Layout.Texts, t => t.Text == "77,5");
        s.Screen.OnMouseDown(1054 * 1.5f, 140 * 1.5f, MouseButton.Left);
        s.Screen.OnMouseUp(1054 * 1.5f, 140 * 1.5f, MouseButton.Left);
        Assert.Equal(50, Assert.Single(s.Screen.BuildMoveRequest().Orders).Speed);
        s.Screen.OnMouseDown(1200 * 1.5f, 225 * 1.5f, MouseButton.Left);
        Assert.InRange(Assert.Single(s.Screen.BuildMoveRequest().Orders).Speed, 77.45, 77.55);
        Assert.Equal(ScreenEvent.MoveTacticalBattle, s.Screen.OnMouseDown(1150 * 1.5f, 347 * 1.5f, MouseButton.Left));
    }
}
