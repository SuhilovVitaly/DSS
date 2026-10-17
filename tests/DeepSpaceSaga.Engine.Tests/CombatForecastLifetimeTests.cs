using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatForecastLifetimeTests
{
    private static TorpedoRoute Straight(double y, double speed) => new(0, 1, TorpedoRoutePhase.Straight, false,
        [new(0, y, 0, speed, 0, 200_000)]);

    [Fact]
    public void CombatForecast_missing_torpedo_expiry_cannot_predict_contact_or_dependent_interception()
    {
        var ship = new ObjectMotionSnapshot("ship", 0, -1505 - TorpedoCollisionMath.DistanceTolerance, 0, 0);
        var torpedo = new ObjectMotionSnapshot("torpedo", 0, 0, 1, 0,
            Torpedo: new("owner", "launcher", "ship", 0, 1, 90, 1, 0, Straight(0, 1)));
        var result = CombatForecast.Torpedo(torpedo, ship, 0);
        Assert.Equal(CombatForecastStatus.InsufficientData, result.Status);
        Assert.Null(result.ContactMotionTimeMs);
        Assert.InRange(result.EndMotionTimeMs, 0, 100);
        Assert.NotEmpty(result.Projectile);

        var defense = new ObjectMotionSnapshot("defense", 0, 20, 2, 0,
            Countermeasure: new("defender", "module", "torpedo", CountermeasurePhase.Guiding,
                Straight(20, 2), 0, 500, new(null!, 0), SpeedKmS: 2, TurnRateDegPerSec: 90,
                ExpiresAtMotionTimeMs: 200_000));
        var dependent = CombatForecast.Countermeasure(defense, torpedo, result, 0);
        Assert.Equal(CombatForecastStatus.InsufficientData, dependent.Status);
        Assert.Null(dependent.ContactMotionTimeMs);
        Assert.True(dependent.EndMotionTimeMs <= result.EndMotionTimeMs);
    }

    [Theory]
    [InlineData(999, CombatForecastStatus.Expired)]
    [InlineData(1000, CombatForecastStatus.Contact)]
    [InlineData(1001, CombatForecastStatus.Contact)]
    public void CombatForecast_torpedo_contact_before_at_and_after_expiry(long expiry, CombatForecastStatus expected)
    {
        var target = new ObjectMotionSnapshot("ship", 0, -15 - TorpedoCollisionMath.DistanceTolerance, 0, 0);
        var torpedo = new ObjectMotionSnapshot("torpedo", 0, 0, 1, 0,
            Torpedo: new("owner", "launcher", "ship", 0, 1, 90, 1, 0, Straight(0, 1), ExpiresAtMotionTimeMs: expiry));
        var result = CombatForecast.Torpedo(torpedo, target, 0);
        Assert.Equal(expected, result.Status);
        Assert.True(result.EndMotionTimeMs <= expiry);
        if (expected == CombatForecastStatus.Contact) Assert.Equal(1000, result.EndMotionTimeMs, 5);
    }

    [Theory]
    [InlineData(999, 2000, CombatForecastStatus.TargetTerminated)]
    [InlineData(1000, 2000, CombatForecastStatus.Contact)]
    [InlineData(1001, 2000, CombatForecastStatus.Contact)]
    [InlineData(2000, 999, CombatForecastStatus.Expired)]
    [InlineData(2000, 1000, CombatForecastStatus.Contact)]
    [InlineData(2000, 1001, CombatForecastStatus.Contact)]
    public void CombatForecast_defense_contact_before_at_and_after_torpedo_expiry(long expiry, long defenseExpiry, CombatForecastStatus expected)
    {
        var ship = new ObjectMotionSnapshot("ship", 0, -100_000, 0, 0);
        var torpedo = new ObjectMotionSnapshot("torpedo", 0, -15 - TorpedoCollisionMath.DistanceTolerance, 1, 0,
            Torpedo: new("owner", "launcher", "ship", 0, 1, 90, 1, 0,
                Straight(-15 - TorpedoCollisionMath.DistanceTolerance, 1), ExpiresAtMotionTimeMs: expiry));
        var defense = new ObjectMotionSnapshot("defense", 0, 0, 2, 0,
            Countermeasure: new("defender", "module", "torpedo", CountermeasurePhase.Guiding,
                Straight(0, 2), 0, 500, new(null!, 0), SpeedKmS: 2, TurnRateDegPerSec: 90,
                ExpiresAtMotionTimeMs: defenseExpiry));
        var targetForecast = CombatForecast.Torpedo(torpedo, ship, 0);
        var result = CombatForecast.Countermeasure(defense, torpedo, targetForecast, 0);
        Assert.Equal(expected, result.Status);
        Assert.True(result.EndMotionTimeMs <= Math.Min(expiry, defenseExpiry));
        if (expected == CombatForecastStatus.Contact) Assert.Equal(1000, result.EndMotionTimeMs, 5);
    }
}
