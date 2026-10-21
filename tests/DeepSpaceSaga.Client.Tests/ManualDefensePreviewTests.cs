using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using Xunit.Abstractions;
namespace DeepSpaceSaga.Client.Tests;

[Collection("Combat forecast performance")]
public sealed class ManualDefensePreviewTests(ITestOutputHelper output)
{
    [Fact, Trait("Category", "ForecastBudget")]
    public async Task Current_point_preview_preparation_obeys_strict_UI_budget()
    {
        await using var f = await PointCombatUiFixture.Create(hostileShots: 12);
        Assert.Equal(12, f.Presented.Objects.Count(o => o.ObservedProjectile is not null));
        f.Expose(150); f.Aim(counter: true);
        var mouse = f.Pixel(150); f.Screen.OnMouseMove(mouse.X, mouse.Y); f.Render();
        var method = typeof(GameSessionScreen).GetMethod("BuildCounterAimPreview", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        object[] args = [150d, 0d];
        for (int i = 0; i < 100; i++) Assert.NotNull(method.Invoke(f.Screen, args));
        var timings = new List<double>();
        for (int i = 0; i < 200; i++)
        {
            args[0] = 150d + i * .01; long start = Stopwatch.GetTimestamp();
            var preview = method.Invoke(f.Screen, args); timings.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            Assert.NotNull(preview);
        }
        timings.Sort(); output.WriteLine($"Current point preview with 12 actual observed incoming flights UI p99={timings[197]:F4} ms; strict limit=2 ms. Prior retired homing-budget failures remain historical failures.");
        Assert.True(timings[197] <= 2, $"Point preview p99={timings[197]} ms exceeds 2 ms");
        Assert.Null(f.Screen.LastPointCommandId); Assert.Equal(0, f.Engine.ReceivedCommandCount);
    }
}
