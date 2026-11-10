using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class OverlayCameraInteractionTests
{
    [Fact]
    public async Task Zoom_anchor_bounds_and_pan_keep_camera_across_phases_and_resize()
    {
        await using var s = await Ep0019Scenes.Create("1-move-no-hover"); using var b = s.Render();
        var world = (s.Fixture.Game.CameraFocusX, s.Fixture.Game.CameraFocusY, s.Fixture.Game.CameraPixelsPerWorldUnit);
        var camera = new TacticalBattleCamera(); var state = s.Screen.Flow.State!; camera.Update(state, 1280, 720); var fitted = camera.View;
        var anchor = fitted.Unproject(600, 500); camera.Zoom(600, 500, -100); var p = camera.View.Project(anchor.X, anchor.Y);
        Assert.InRange(p.X, 599.99, 600.01); Assert.InRange(p.Y, 499.99, 500.01);
        camera.Zoom(600, 500, -1000000); Assert.Equal(fitted.WorldWidth * .125, camera.View.WorldWidth, 6);
        camera.Zoom(600, 500, 1000000); Assert.Equal(fitted.WorldWidth * 4, camera.View.WorldWidth, 6);
        var manual = camera.View; camera.Update(state with { Revision = state.Revision + 1 }, 1280, 720); Assert.Equal(manual, camera.View);
        camera.Update(state with { Phase = TacticalBattlePhase.Fire }, 1280, 720);
        Assert.True(camera.UserView);
        Assert.Equal(manual, camera.View);
        camera.Update(state with { Turn = state.Turn + 1 }, 1600, 720);
        Assert.Equal(manual with { PixelWidth = 1600 }, camera.View);
        camera.Update(state with { BattleId = "another-battle" }, 1600, 720);
        Assert.False(camera.UserView);
        Assert.NotEqual(manual.PixelsPerUnit, camera.View.PixelsPerUnit);
        var original = s.Screen.View;
        s.Screen.OnMouseDown(900, 750, MouseButton.Right); s.Screen.OnMouseMove(960, 780); s.Screen.OnMouseUp(960, 780, MouseButton.Right);
        Assert.InRange(s.Screen.View.CenterX, original.CenterX - 40 / original.PixelsPerUnit - .01, original.CenterX - 40 / original.PixelsPerUnit + .01);
        var panned = s.Screen.View;
        s.Screen.OnKeyDown(Key.Number0);
        Assert.Equal(panned, s.Screen.View);
        Assert.Equal(world, (s.Fixture.Game.CameraFocusX, s.Fixture.Game.CameraFocusY, s.Fixture.Game.CameraPixelsPerWorldUnit));
    }
}
