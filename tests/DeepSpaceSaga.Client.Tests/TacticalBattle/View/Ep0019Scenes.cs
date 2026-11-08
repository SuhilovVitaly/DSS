using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

internal sealed class Ep0019Scenes : IAsyncDisposable
{
    internal BattleNavigationTests.Fixture Fixture { get; }
    internal TacticalBattleScreen Screen => (TacticalBattleScreen)Fixture.Screens.Current;
    private Ep0019Scenes(BattleNavigationTests.Fixture fixture) => Fixture = fixture;
    internal static async Task<Ep0019Scenes> Create(string scene)
    {
        var f = await BattleNavigationTests.Fixture.Create();
        var result = new Ep0019Scenes(f);
        try
        {
            await f.Route(ScreenEvent.OpenTacticalBattle);
            var setup = (TacticalBattleModalScreen)f.Screens.Current;
            await setup.Flow.OptionsTask!;
            await f.Route(setup.OnKeyDown(Key.Enter));
            Assert.Equal(1, result.Screen.Flow.State!.Seed);
            if (!scene.StartsWith("1-", StringComparison.Ordinal))
            {
                while (result.Screen.Flow.State!.Phase != TacticalBattlePhase.Fire)
                {
                    Assert.True(result.Screen.Flow.State.Turn <= 3);
                    await f.Route(ScreenEvent.MoveTacticalBattle);
                    f.FinishPresentation();
                }
                Assert.Equal(3, result.Screen.Flow.State.Turn);
                result.Screen.CaptureFrame(1920, 1080);
                // First own candidate in the canonical scenario is Marta.
                while (result.Screen.CaptureFrame(1920, 1080)!.FirePlan!.Selected?.Target is null)
                    result.Screen.OnKeyDown(Key.Tab);
                if (scene is "3-salvo-flight" or "4-salvo-results")
                {
                    f.ManualAnimation = true;
                    await f.Route(ScreenEvent.FireTacticalBattle);
                    f.AdvanceAnimation(300 + (long)Math.Round(975 * 0.62));
                    if (scene == "4-salvo-results") result.Screen.OnKeyDown(Key.Enter);
                }
                if (scene == "5-outcome")
                {
                    for (int i = 0; result.Screen.Flow.State!.Phase != TacticalBattlePhase.Over && i < 100; i++)
                    {
                        f.FinishPresentation();
                        await f.Route(result.Screen.OnKeyDown(Key.Enter));
                    }
                    f.FinishPresentation();
                    Assert.Equal(TacticalBattlePhase.Over, result.Screen.Flow.State!.Phase);
                }
            }
            var hoverName = scene is "1-move-no-hover" or "5-outcome" ? null : scene == "2-fire-hover-own" ? "Олег" : "Таг-6";
            result.Screen.HoverUnitId = result.Screen.Flow.State!.Units.FirstOrDefault(u =>
                (u.Build?.PilotName ?? u.Id) == hoverName)?.Id;
            return result;
        }
        catch { await result.DisposeAsync(); throw; }
    }
    internal SKBitmap Render()
    {
        var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        Screen.Render(canvas, bitmap.Width, bitmap.Height);
        return bitmap;
    }
    public ValueTask DisposeAsync() => Fixture.DisposeAsync();
}
