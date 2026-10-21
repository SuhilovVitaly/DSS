using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PhaseThreeCombatUiFlowTests
{
    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.2f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1920, 1080, 1.2f)]
    [InlineData(1920, 1080, 1.5f)]
    public async Task Two_point_launchers_keep_preview_labels_inside_viewport_and_dispatch_once_while_paused(int width, int height, float scale)
    {
        await using var f = await PointCombatUiFixture.Create(width, height, scale);
        var viewport = new SKRect(0, 0, width, height);
        for (int index = 0; index < 2; index++)
        {
            f.Expose(180, index == 0 ? -60 : 60); f.Aim(counter: true); var p = f.Pixel(180, index == 0 ? -60 : 60);
            f.Screen.OnMouseMove(p.X, p.Y); f.Render();
            Assert.True(f.Screen.CurrentCounterAimPreview is not null, f.DebugMap(180, index == 0 ? -60 : 60));
            var label = f.Screen.CounterAimLabelBounds;
            Assert.True(label.Width > 0 && label.Height > 0); Assert.True(viewport.Contains(label));
            string module = f.Screen.PointAim!.ModuleId;
            string? before = f.Screen.LastPointCommandId;
            f.Screen.OnMouseDown(p.X, p.Y); f.Screen.OnMouseDown(p.X, p.Y);
            string id = Assert.IsType<string>(f.Screen.LastPointCommandId); Assert.NotEqual(before, id);
            await f.Wait(() => f.Handle.Buffer.FindCommandResult(id) is not null);
            var result = f.Handle.Buffer.FindCommandResult(id)!;
            Assert.Equal(CommandResultStatus.Executed, result.Status); Assert.Equal(module, result.ModuleId);
            await f.Wait(() => f.Presented.Objects.Count(o => o.OwnProjectile is not null) == index + 1);
        }
        Assert.Equal(0, f.Presented.MotionTimeMs); Assert.Equal(SimulationSpeed.Speed0, f.Presented.CurrentSpeed);
        Assert.Equal(2, f.Presented.CombatJournal.Count(e => e.Type == CombatEventType.Launch));
        Assert.All(f.Screen.PointProjectileVisuals.Values, v => Assert.DoesNotContain("%", v.Label));
        Assert.Equal(2, f.Presented.Objects.Select(o => o.OwnProjectile?.LauncherModuleId).Where(x => x is not null).Distinct().Count());
    }
}
