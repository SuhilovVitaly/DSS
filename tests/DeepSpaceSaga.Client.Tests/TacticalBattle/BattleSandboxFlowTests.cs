using System.Reflection;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BattleSandboxFlowTests
{
    [Fact]
    public async Task ActualF8ExitKeepsWorldSaveIdenticalAndReenablesF5AndMenu()
    {
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
        string directory = Path.Combine(root, "sandbox-slots-" + Guid.NewGuid().ToString("N"));
        string baseline = Path.Combine(directory, SaveSlotNaming.ToFileName("before-battle"));
        string quicksave = Path.Combine(directory, SaveSlotNaming.ToFileName(SaveSlots.Quicksave));
        try
        {
            await using var fixture = await BattleNavigationTests.Fixture.Create(saveDirectory: directory, initialSpeed: SimulationSpeed.Speed0);
            var camera = (CameraState)typeof(GameSessionScreen).GetField("_camera", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Game)!;
            camera.SetFocus(112.25, -245.5);
            camera.SetZoom(0.00123);
            var expectedCamera = (fixture.Game.CameraFocusX, fixture.Game.CameraFocusY, fixture.Game.CameraPixelsPerWorldUnit);
            await fixture.Proxy.Inner.SaveAsync("before-battle");
            byte[] expectedSave = await File.ReadAllBytesAsync(baseline);
            await fixture.PressKey(Key.F8);
            var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
            await setup.Flow.OptionsTask!;
            await fixture.Route(ScreenEvent.StartTacticalBattle);
            var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
            fixture.FinishPresentation();
            await fixture.PressKey(Key.F5);
            Assert.False(File.Exists(quicksave));
            Assert.True(fixture.Engine.IsTacticalBattleActive);
            await fixture.PressKey(Key.Escape);
            Assert.True(battle.ConfirmExit);
            await fixture.PressKey(Key.Enter);
            Assert.Same(fixture.Game, fixture.Screens.Current);
            Assert.False(fixture.Engine.IsTacticalBattleActive);
            Assert.Equal(expectedCamera, (fixture.Game.CameraFocusX, fixture.Game.CameraFocusY, fixture.Game.CameraPixelsPerWorldUnit));
            Assert.Equal(SimulationSpeed.Speed0, fixture.Engine.CurrentSpeed);
            await fixture.PressKey(Key.F5);
            Assert.Equal(expectedSave, await File.ReadAllBytesAsync(quicksave));
            await fixture.PressKey(Key.Escape);
            Assert.IsType<GameMenuScreen>(fixture.Screens.Current);
            await fixture.Route(ScreenEvent.Resume);
            Assert.Same(fixture.Game, fixture.Screens.Current);
        }
        finally
        {
            if (File.Exists(baseline)) File.Delete(baseline);
            if (File.Exists(quicksave)) File.Delete(quicksave);
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }

    [Fact]
    public async Task F8RoundTripRestoresTheRunningWorldSpeed()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.PressKey(Key.F8);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        Assert.Equal(SimulationSpeed.Speed0, fixture.Engine.CurrentSpeed);
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        Assert.Equal(ScreenEvent.None, battle.OnKeyDown(Key.Escape));
        await fixture.Route(battle.OnKeyDown(Key.Enter));
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.Equal(SimulationSpeed.Speed2, fixture.Engine.CurrentSpeed);
        Assert.Equal(new[] { SimulationSpeed.Speed0, SimulationSpeed.Speed2 }, fixture.Proxy.Speeds.ToArray());
    }
}
