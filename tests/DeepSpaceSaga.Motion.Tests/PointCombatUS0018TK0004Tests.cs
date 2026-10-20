using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class PointCombatUS0018TK0004Tests
{
    private static ObjectMotionSnapshot Own(ProjectileKind kind = ProjectileKind.Torpedo) =>
        new("own", 0, 0, 6, 90, OwnProjectile: new(kind, "player", "launcher", 0, 0, 0, 0,
            600, 0, 0, 10000, 6, "slow", 9, 200, 20));

    private static ObjectMotionSnapshot Enemy() => new("enemy", 100, 20, 10, 90,
        ObservedProjectile: new(ProjectileKind.Torpedo, 100, 20, 1, 0, 10, "medium", 5));

    [Fact]
    public void Shared_forecast_has_no_hidden_enemy_point_and_matches_own_deadline()
    {
        var own = CombatForecast.Torpedo(Own(), null, 0);
        Assert.Equal(10000, own.EndMotionTimeMs);
        Assert.Equal(CombatForecastStatus.Expired, own.Status);
        Assert.Equal(600, own.Projectile[^1].X);
        Assert.Equal(300, own.ProjectilePose(5000).X);
        Assert.Equal(90, own.ProjectilePose(10000).Direction);
        Assert.Empty(own.Legs);
        var enemy = CombatForecast.Torpedo(Enemy(), new("hidden-target", 1234, 9876, 0, 0), 2000,
            new(HorizonMs: 1000));
        Assert.Equal(CombatForecastStatus.InsufficientData, enemy.Status);
        Assert.Null(enemy.ContactMotionTimeMs);
        Assert.Empty(enemy.TargetId);
        Assert.Empty(enemy.Target);
        Assert.Equal(3000, enemy.EndMotionTimeMs);
        Assert.Equal(200, enemy.Projectile[^1].X);
        Assert.Equal(20, enemy.Projectile[^1].Y);
        Assert.Equal(150, enemy.ProjectilePose(2500).X);
    }

    [Fact]
    public void Area_contact_occurs_only_at_detonation_and_excludes_zero_damage_edge()
    {
        var crossing = new ObjectMotionSnapshot("crossing", 300, 0, 0, 0);
        var noEarlyImpact = CombatForecast.PointProjectile(Own(), crossing, 0);
        Assert.Equal(CombatForecastStatus.Expired, noEarlyImpact.Status);
        Assert.Equal(10000, noEarlyImpact.EndMotionTimeMs);
        var core = CombatForecast.PointProjectile(Own(), crossing with { X = 600 }, 0);
        Assert.Equal(CombatForecastStatus.Contact, core.Status);
        Assert.Equal(10000, core.ContactMotionTimeMs);
        Assert.Equal(CombatForecastStatus.Expired, CombatForecast.PointProjectile(Own(), crossing with { X = 690 }, 0).Status);
        Assert.Equal(CombatForecastStatus.Contact, CombatForecast.Countermeasure(
            Own(ProjectileKind.Countermissile), crossing with { X = 690 }, null, 0).Status);
    }

    [Fact]
    public void Current_sample_agrees_with_absolute_forecast_and_preserves_observation_schema()
    {
        var own = Own();
        var middle = CurrentManeuverForecast.Sample(own, 5000);
        Assert.Equal(300, middle.X);
        Assert.Equal(300, middle.OwnProjectile!.X);
        Assert.Equal(600, CurrentManeuverForecast.Sample(own, 12000).X);
        var continued = CombatForecast.PointProjectile(middle, null, 5000);
        Assert.Equal(300, continued.Projectile[0].X);
        Assert.Equal(600, continued.Projectile[^1].X);
        Assert.Equal(10000, continued.EndMotionTimeMs);
        var enemy = CurrentManeuverForecast.Sample(Enemy(), 500);
        Assert.Null(enemy.OwnProjectile);
        Assert.Equal(150, enemy.ObservedProjectile!.X);
        Assert.Equal(150, enemy.X);
    }

    [Fact]
    public void Cancellation_and_budgets_return_bounded_calculation_limited_results()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = CombatForecast.PointProjectile(Own(), null, 0, cancellationToken: cancellation.Token);
        Assert.Equal(CombatForecastStatus.CalculationLimited, cancelled.Status);
        Assert.Equal(0, cancelled.EndMotionTimeMs);
        Assert.Single(cancelled.Projectile);
        var steps = CombatForecast.PointProjectile(Own(), null, 0, new(MaxSteps: 1));
        Assert.Equal(CombatForecastStatus.CalculationLimited, steps.Status);
        Assert.Equal(100, steps.EndMotionTimeMs);
        var points = CombatForecast.PointProjectile(Own(), null, 0, new(MaxPoints: 2));
        Assert.Equal(CombatForecastStatus.CalculationLimited, points.Status);
        Assert.Equal(2, points.Projectile.Length);
        Assert.Null(points.ContactMotionTimeMs);
    }

    [Fact]
    public void Horizon_and_fractional_expiry_do_not_become_early_contacts()
    {
        var target = new ObjectMotionSnapshot("target", 600, 0, 0, 0);
        var shortForecast = CombatForecast.PointProjectile(Own(), target, 0, new(HorizonMs: 5000));
        Assert.Equal(CombatForecastStatus.NoContact, shortForecast.Status);
        Assert.Equal(5000, shortForecast.EndMotionTimeMs);
        Assert.Equal(300, shortForecast.Projectile[^1].X);
        var p = Own();
        p = p with { OwnProjectile = p.OwnProjectile! with { DetonationX = 1.125, DetonationMotionTimeMs = 18.75 } };
        var result = CombatForecast.PointProjectile(p, null, 0);
        Assert.Equal(18.75, result.EndMotionTimeMs);
        Assert.Equal(1.125, result.Projectile[^1].X);
        var expired = CombatForecast.PointProjectile(Own(), target, 20000);
        Assert.Equal(CombatForecastStatus.Expired, expired.Status);
        Assert.Single(expired.Projectile);
    }

    [Fact]
    public void Countermissile_does_not_forecast_contact_with_an_already_detonated_own_projectile()
    {
        var torpedo = Own();
        torpedo = torpedo with { ObjectId = "earlier", OwnProjectile = torpedo.OwnProjectile! with { DetonationMotionTimeMs = 5000, DetonationX = 300 } };
        var result = CombatForecast.PointProjectile(Own(ProjectileKind.Countermissile), torpedo, 0);
        Assert.Equal(CombatForecastStatus.TargetTerminated, result.Status);
        Assert.Equal(5000, result.EndMotionTimeMs);
        Assert.Null(result.ContactMotionTimeMs);
    }

    [Fact]
    public void Invalid_options_and_unsupported_payload_fail_explicitly()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CombatForecast.PointProjectile(Own(), null, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => CombatForecast.PointProjectile(Own(), null, 0, new(MaxPoints: 1)));
        Assert.Throws<ArgumentException>(() => CombatForecast.PointProjectile(new("ship", 0, 0, 0, 0), null, 0));
    }
}
