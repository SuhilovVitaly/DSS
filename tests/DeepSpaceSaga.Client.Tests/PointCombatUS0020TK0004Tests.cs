using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class PointCombatUS0020TK0004Tests
{
    [Fact]
    public async Task Torpedo_click_submits_once_and_exits()
    {
        await using var f = await PointCombatUiFixture.Create();
        f.Expose(300); f.Aim(); f.Click(300);
        Assert.NotNull(f.Screen.LastPointCommandId);
        Assert.Null(f.Screen.PointAim); Assert.Null(f.Screen.CurrentTorpedoAimPreview);
        await f.Wait(() => f.Engine.ReceivedCommandCount == 1);
    }

    [Fact]
    public async Task Mouse_after_launch_does_not_repeat()
    {
        await using var f = await PointCombatUiFixture.Create();
        f.Expose(300); f.Aim(); f.Click(300);
        var id = f.Screen.LastPointCommandId;
        f.Screen.OnMouseMove(900, 400); f.Click(350); f.Render();
        await f.Wait(() => f.Engine.ReceivedCommandCount == 1);
        Assert.Equal(id, f.Screen.LastPointCommandId); Assert.Null(f.Screen.PointAim);
    }

    [Fact]
    public async Task Invalid_click_or_friendly_warning_keeps_aim()
    {
        await using var f = await PointCombatUiFixture.Create();
        f.Expose(0); f.Aim(); f.Click(0);
        Assert.NotNull(f.Screen.PointAim); Assert.Null(f.Screen.LastPointCommandId);
        Assert.Equal(0, f.Engine.ReceivedCommandCount);
    }

    [Fact]
    public async Task Failure_and_session_reset_do_not_restore_aim()
    {
        await using var f = await PointCombatUiFixture.Create();
        f.Expose(300); f.Aim(); f.Click(300);
        await f.Wait(() => f.Engine.ReceivedCommandCount == 1);
        f.Render(); Assert.Null(f.Screen.PointAim);
        f.Screen.OnKeyDown(Key.Q); Assert.NotNull(f.Screen.PointAim);
        f.Screen.OnKeyDown(Key.Escape); Assert.Null(f.Screen.PointAim);
    }
}
