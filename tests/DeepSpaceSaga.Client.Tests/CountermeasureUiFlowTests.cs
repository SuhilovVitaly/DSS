using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CountermeasureUiFlowTests
{
    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1.2f)]
    public async Task Defense_ui_dispatches_independent_point_launches_without_guiding_or_automatic_fire(int width, int height, float scale)
    {
        await using var f = await PointCombatUiFixture.Create(width, height, scale);
        Assert.Equal(CommandResultStatus.Executed, (await f.Launch(counter: true, x: 150, y: -60)).Status);
        await f.Wait(() => f.Presented.Objects.Any(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile));
        var first = Assert.Single(f.Presented.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        Assert.Equal("fast", first.ProfileId); Assert.Equal(25, first.SpeedKmS); Assert.Equal(.5, first.BlastRadiusKm);
        Assert.Equal(CommandResultStatus.Executed, (await f.Launch(counter: true, x: 150, y: 60)).Status);
        await f.Wait(() => f.Presented.Objects.Count(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile) == 2);
        var flights = f.Presented.Objects.Where(o => o.OwnProjectile is not null).Select(o => o.OwnProjectile!).ToArray();
        Assert.Equal(2, flights.Select(p => p.LauncherModuleId).Distinct().Count());
        Assert.All(f.Presented.InstalledModules.Where(m => m.Defense is not null), m =>
        { Assert.False(m.Defense!.AutoEnabled); Assert.Equal(DefenseState.Reloading, m.Defense.State); Assert.Equal(12500, m.Defense.ReadyAtMotionTimeMs); });
        Assert.All(flights, p => Assert.Equal(0, p.LaunchMotionTimeMs));
        Assert.Equal(0, f.Presented.MotionTimeMs); Assert.Equal(SimulationSpeed.Speed0, f.Presented.CurrentSpeed);
        Assert.DoesNotContain(f.Presented.Objects, o => o.Countermeasure is not null || o.Torpedo is not null);
        var ended = await f.AdvanceTo(12500);
        Assert.DoesNotContain(ended.Objects, o => o.OwnProjectile is not null);
        Assert.All(ended.InstalledModules.Where(m => m.Defense is not null), m => Assert.Equal(DefenseState.Ready, m.Defense!.State));
        Assert.Equal(2, ended.CombatJournal.Count(e => e.Type == CombatEventType.Launch && e.ProjectileKind == ProjectileKind.Countermissile));
    }
}
