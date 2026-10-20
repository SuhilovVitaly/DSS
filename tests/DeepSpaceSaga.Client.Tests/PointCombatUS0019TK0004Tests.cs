using System.Numerics;
using System.Reflection;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0019TK0004Tests
{
    public class MouseProxy : DispatchProxy
    {
        public Vector2 Position;
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method!.Name == "get_Position" ? Position : throw new NotSupportedException(method.Name);
    }
    [Fact]
    public async Task Combat_ui_regression_uses_new_commands_and_preserves_scene_time_through_window_middle_binding()
    {
        await using var f = await PointCombatUiFixture.Create(inputs: new() { LaunchMouse = MouseButton.Middle });
        using var window = new SkiaWindow(f.Screen, f, null, createNativeWindow: false);
        f.Render(); f.Aim();
        var pixel = f.Pixel(300);
        var mouse = DispatchProxy.Create<IMouse, MouseProxy>(); ((MouseProxy)(object)mouse).Position = new(pixel.X, pixel.Y);
        typeof(SkiaWindow).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, [mouse, MouseButton.Middle]);
        Assert.NotNull(f.Screen.LastPointCommandId);
        await f.Wait(() => f.Handle.Buffer.FindCommandResult(f.Screen.LastPointCommandId!) is not null);
        var result = f.Handle.Buffer.FindCommandResult(f.Screen.LastPointCommandId!)!;
        Assert.Equal(CommandResultStatus.Executed, result.Status); Assert.Equal(CombatCommandTypes.Launch, result.CommandType);
        Assert.Equal(0, f.Latest.MotionTimeMs); Assert.Equal(SimulationSpeed.Speed0, f.Latest.CurrentSpeed);
        await f.Wait(() => f.Presented.Objects.Any(o => o.OwnProjectile is not null));
        var flight = Assert.Single(f.Presented.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        Assert.Equal("medium", flight.ProfileId); Assert.Equal(PointCombatUiFixture.Torpedo, flight.LauncherModuleId);
        Assert.InRange(Math.Abs(flight.DetonationX - 300), 0, 45); Assert.InRange(Math.Abs(flight.DetonationY), 0, 45);
    }
}
