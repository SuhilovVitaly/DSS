using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

public sealed class BasicCombatUiFlowTests
{
    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1920, 1080, 1.2f)]
    public async Task Ui_point_launch_flows_through_real_session_reload_blast_and_paused_effect_lifetime(int width, int height, float scale)
    {
        await using var f = await PointCombatUiFixture.Create(width, height, scale);
        for (int shot = 0; shot < 2; shot++)
        {
            f.Expose(300); f.Aim(); var pixel = f.Pixel(300); f.Screen.OnMouseMove(pixel.X, pixel.Y); f.Render();
            Assert.True(f.Screen.CurrentTorpedoAimPreview is not null, f.DebugMap(300));
            var preview = f.Screen.CurrentTorpedoAimPreview!;
            Assert.Equal(300, preview.X, 5); Assert.Equal(0, preview.Y, 5); Assert.Equal(5, preview.RadiusKm);
            Assert.DoesNotContain(f.Presented.Objects, o => o.OwnProjectile is not null);
            var result = await f.Launch(); Assert.Equal(CommandResultStatus.Executed, result.Status);
            await f.Wait(() => f.Presented.Objects.Any(o => o.OwnProjectile is not null));
            var missile = Assert.Single(f.Presented.Objects, o => o.OwnProjectile is not null);
            var flight = missile.OwnProjectile!;
            Assert.Equal(PointCombatUiFixture.Player, flight.OwnerObjectId); Assert.Equal(PointCombatUiFixture.Torpedo, flight.LauncherModuleId);
            Assert.Equal("medium", flight.ProfileId); Assert.Equal(10, flight.SpeedKmS); Assert.Equal(5, flight.BlastRadiusKm);
            Assert.Equal((0d, 0d), (flight.StartX, flight.StartY)); Assert.Null(missile.Torpedo);
            Assert.Equal(SimulationSpeed.Speed0, f.Presented.CurrentSpeed);
            Assert.Equal(GameSessionScreen.CombatText("Reloading", "Reloading"), f.Screen.PointAimFeedback);
            var end = await f.AdvanceTo((long)Math.Ceiling(flight.DetonationMotionTimeMs) + 1);
            Assert.DoesNotContain(end.Objects, o => o.ObjectId == missile.ObjectId);
            Assert.Contains(end.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated && e.ProjectileObjectId == missile.ObjectId);
            var hit = Assert.Single(end.CombatJournal, e => e.Type == CombatEventType.Hit &&
                e.TargetObjectId == PointCombatUiFixture.Enemy && e.ProjectileObjectId == missile.ObjectId);
            Assert.NotNull(hit.HitResult); Assert.Null(hit.Damage); Assert.Null(hit.HitModuleId);
            Assert.DoesNotContain(end.CombatJournal, e => e.ChanceTenths is not null || e.Roll is not null);
            Assert.Contains(f.Screen.CombatEffects.PointEffects, e => e.Entry.ProjectileObjectId == missile.ObjectId);
            Assert.DoesNotContain(missile.ObjectId, f.Screen.CombatTrajectories.Keys);
            long stopped = end.MotionTimeMs;
            f.UiMs += 2100; f.Render(); Assert.Empty(f.Screen.CombatEffects.PointEffects);
            Assert.Equal(stopped, f.Presented.MotionTimeMs);
            double due = end.InstalledModules.Single(m => m.ModuleId == PointCombatUiFixture.Torpedo).LauncherCombat!.ReadyAtMotionTimeMs!.Value;
            await f.AdvanceTo((long)Math.Ceiling(due));
            Assert.Equal(GameSessionScreen.CombatText("Ready", "Ready"), f.Screen.PointAimFeedback);
        }
    }

    [Fact]
    public async Task Ui_selection_and_owner_turn_do_not_retarget_captured_point_flight()
    {
        await using var f = await PointCombatUiFixture.Create();
        Assert.Equal(CommandResultStatus.Executed, (await f.Launch()).Status);
        await f.Wait(() => f.Presented.Objects.Any(o => o.OwnProjectile is not null));
        var original = Assert.Single(f.Presented.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        f.Screen.OnKeyDown(Key.Escape); f.Expose(0); f.Click(0); f.Render();
        Assert.Equal(PointCombatUiFixture.Player, f.Screen.SelectedObjectId);
        f.Screen.OnKeyDown(Key.Right);
        await f.Wait(() => f.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatUiFixture.Player).ActiveEngineCommandType == ShipEngineCommandTypes.TurnRightStep);
        var moved = await f.AdvanceTo(500); f.Screen.OnKeyUp(Key.Right);
        var flight = Assert.Single(moved.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        Assert.Equal(original.DetonationX, flight.DetonationX); Assert.Equal(original.DetonationY, flight.DetonationY);
        Assert.Equal(original.DetonationMotionTimeMs, flight.DetonationMotionTimeMs);
        Assert.NotEqual(90, moved.Objects.Single(o => o.ObjectId == PointCombatUiFixture.Player).Direction);
        double fraction = 500 / original.DetonationMotionTimeMs;
        Assert.Equal(original.StartX + (original.DetonationX - original.StartX) * fraction, flight.X, 6);
        Assert.Equal(original.StartY + (original.DetonationY - original.StartY) * fraction, flight.Y, 6);
    }

    [Fact]
    public async Task Real_point_blasts_replace_destroyed_ship_with_one_stationary_visible_wreck()
    {
        await using var f = await PointCombatUiFixture.Create();
        for (int shot = 0; shot < 32 && f.Presented.Objects.Any(o => o.ObjectId == PointCombatUiFixture.Enemy); shot++)
        {
            Assert.Equal(CommandResultStatus.Executed, (await f.Launch(profile: 2)).Status);
            await f.Wait(() => f.Presented.Objects.Any(o => o.OwnProjectile is not null));
            var flight = Assert.Single(f.Presented.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
            var ended = await f.AdvanceTo((long)Math.Ceiling(flight.DetonationMotionTimeMs) + 1);
            if (ended.Objects.All(o => o.ObjectId != PointCombatUiFixture.Enemy)) break;
            await f.AdvanceTo((long)Math.Ceiling(ended.InstalledModules.Single(m => m.ModuleId == PointCombatUiFixture.Torpedo).LauncherCombat!.ReadyAtMotionTimeMs!.Value));
        }
        Assert.DoesNotContain(f.Presented.Objects, o => o.ObjectId == PointCombatUiFixture.Enemy);
        var wreck = Assert.Single(f.Presented.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        Assert.Equal(0, wreck.SpeedKmS); Assert.Equal(0, wreck.Direction);
        Assert.Contains(f.Screen.RenderStates, s => s.Source.ObjectId == wreck.ObjectId);
    }
}
