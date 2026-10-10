using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class CountermeasureSnapshotTests
{
    [Theory]
    [InlineData(LaunchMode.Auto, CountermeasurePhase.Guiding)]
    [InlineData(LaunchMode.Manual, CountermeasurePhase.Guiding)]
    [InlineData(LaunchMode.Manual, CountermeasurePhase.MissedCoast)]
    public void Countermeasure_lifetime_and_mode_roundtrip(LaunchMode mode, CountermeasurePhase phase)
    {
        var flight = new CountermeasureSnapshot("owner", "defense-2", "torpedo", phase,
            new(100, 1, TorpedoRoutePhase.Straight, false), 100, 500,
            new(new("crew", "Defense", WeaponSkillType.CountermeasureDefense, 0, 55, 55), 5),
            Mode: mode, CapturedAccuracy: 55, CapturedTargetManeuverability: 5,
            MaxFlightTimeMs: 60000, ExpiresAtMotionTimeMs: 60100,
            SpeedKmS: 12.5, TurnRateDegPerSec: 85.25);
        var restored = JsonSerializer.Deserialize<CountermeasureSnapshot>(JsonSerializer.Serialize(flight))!;
        Assert.Equal(mode, restored.Mode);
        Assert.Equal(phase, restored.Phase);
        Assert.Equal(55m, restored.CapturedAccuracy);
        Assert.Equal(5m, restored.CapturedTargetManeuverability);
        Assert.Equal(60000, restored.MaxFlightTimeMs);
        Assert.Equal(60100, restored.ExpiresAtMotionTimeMs);
        Assert.Equal(12.5, restored.SpeedKmS);
        Assert.Equal(85.25, restored.TurnRateDegPerSec);
        Assert.Null(restored.PredictedEncounterMotionTimeMs);
        Assert.Null(restored.ResolutionRoll);
        Assert.Empty(restored.Trail);
    }

    [Theory]
    [InlineData(CountermeasurePhase.Guiding)]
    [InlineData(CountermeasurePhase.MissedCoast)]
    public void Countermeasure_states_roundtrip(CountermeasurePhase phase)
    {
        var breakdown = new InterceptionRatingBreakdown(new("crew", "Defense", WeaponSkillType.CountermeasureDefense, 50, 30, 30), 48);
        var projectile = new CountermeasureSnapshot("owner", "module", "torpedo", phase,
            new(100, 1, TorpedoRoutePhase.Straight, false), 100, 320, breakdown,
            MissExpiresAtMotionTimeMs: phase == CountermeasurePhase.MissedCoast ? 2100.5 : null);
        var motion = new ObjectMotionSnapshot("pr", 0, 0, 12, 0, ObjectType: SpaceObjectType.Countermeasure, Countermeasure: projectile);
        var restored = JsonSerializer.Deserialize<ObjectMotionSnapshot>(JsonSerializer.Serialize(motion))!;
        Assert.Null(restored.Torpedo);
        Assert.Equal(phase, restored.Countermeasure!.Phase);
        Assert.Equal(breakdown, restored.Countermeasure.RatingBreakdown);
        Assert.Equal(320, restored.Countermeasure.FrozenChanceTenths);
        Assert.Equal(projectile.MissExpiresAtMotionTimeMs, restored.Countermeasure.MissExpiresAtMotionTimeMs);
        Assert.Null(restored.Countermeasure.PredictedEncounterMotionTimeMs);
        Assert.Empty(restored.Countermeasure.Trail);
    }

    [Fact]
    public void Reload_uses_physical_deadline()
    {
        var module = new InstalledModuleSnapshot("module", "type", "Defense", 0, [],
            Defense: new(true, null, DefenseState.Reloading, ReloadDueMotionTimeMs: 12345));
        var restored = JsonSerializer.Deserialize<InstalledModuleSnapshot>(JsonSerializer.Serialize(module))!;
        Assert.Equal(12345, restored.Defense!.ReloadDueMotionTimeMs);
        Assert.Null(restored.Defense.ActiveProjectileId);
        Assert.True(restored.Defense.AutoEnabled);
    }

    [Fact]
    public void Defender_status_available_on_npc_snapshot()
    {
        var npc = new ObjectMotionSnapshot("npc", 0, 0, 0, 0, Defense: new(false, null, DefenseState.NoOperator));
        var restored = JsonSerializer.Deserialize<ObjectMotionSnapshot>(JsonSerializer.Serialize(npc))!;
        Assert.Equal(npc.Defense, restored.Defense);
        var legacy = JsonSerializer.Deserialize<ObjectMotionSnapshot>("""{"ObjectId":"old","X":0,"Y":0,"SpeedKmS":0,"Direction":0}""")!;
        Assert.Null(legacy.Defense);
        Assert.Null(legacy.Countermeasure);
        Assert.False(legacy.CountermeasureAttempted);
        var attempted = npc with { CountermeasureAttempted = true };
        Assert.True(JsonSerializer.Deserialize<ObjectMotionSnapshot>(JsonSerializer.Serialize(attempted))!.CountermeasureAttempted);
    }
}
