using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class WeaponOperatorSnapshotTests
{
    [Theory]
    [InlineData(WeaponSkillType.TorpedoAttack)]
    [InlineData(WeaponSkillType.CountermeasureDefense)]
    public void Operator_payload_roundtrips(WeaponSkillType skillType)
    {
        var weaponOperator = new WeaponOperatorSnapshot("crew-1", "Operator", skillType, 77, 30.125m, 46.3925m);
        var module = new InstalledModuleSnapshot("launcher", "type", "Launcher", 0,
            ImmutableArray<string>.Empty, LauncherCombat: new(null, 3, 90, 150, weaponOperator), Operator: weaponOperator);
        var restoredModule = JsonSerializer.Deserialize<InstalledModuleSnapshot>(JsonSerializer.Serialize(module));
        Assert.Equal(weaponOperator, restoredModule!.LauncherCombat!.Operator);
        Assert.Equal(weaponOperator, restoredModule.Operator);

        var torpedo = new TorpedoSnapshot("owner", "launcher", "target", 0, 3, 90, 150, 0,
            new TorpedoRoute(0, 1, TorpedoRoutePhase.Straight, false),
            TorpedoRating: 46.3925m, RatingBreakdown: weaponOperator);
        var restored = JsonSerializer.Deserialize<TorpedoSnapshot>(JsonSerializer.Serialize(torpedo));
        Assert.Equal(46.3925m, restored!.TorpedoRating);
        Assert.Equal(weaponOperator, restored.RatingBreakdown);
    }

    [Fact]
    public void Missing_operator_differs_from_zero_skill()
    {
        var missing = new LauncherCombatSnapshot(null, 3, 90, 150);
        var assigned = missing with { Operator = new("crew-1", "Operator", WeaponSkillType.TorpedoAttack, 0, 30, 0) };
        Assert.Null(JsonSerializer.Deserialize<LauncherCombatSnapshot>(JsonSerializer.Serialize(missing))!.Operator);
        var restored = JsonSerializer.Deserialize<LauncherCombatSnapshot>(JsonSerializer.Serialize(assigned))!;
        Assert.Equal(0, restored.Operator!.Skill);
        Assert.Equal(0m, restored.Operator.EffectiveRating);
        Assert.NotEqual(missing, restored);
    }

    [Fact]
    public void Old_snapshot_defaults_are_safe()
    {
        var launcher = JsonSerializer.Deserialize<LauncherCombatSnapshot>(
            """{"ActiveTorpedoObjectId":null,"SpeedKmS":3,"TurnRateDegPerSec":90,"Damage":150}""");
        Assert.Null(launcher!.Operator);
        var torpedo = JsonSerializer.Deserialize<TorpedoSnapshot>("""
            {"OwnerObjectId":"owner","LauncherModuleId":"launcher","TargetObjectId":"target",
             "LaunchMotionTimeMs":0,"SpeedKmS":3,"TurnRateDegPerSec":90,"Damage":150,
             "DistanceTravelledWorldUnits":0,"Route":{"StartMotionTimeMs":0,"PlannerVersion":1,"Phase":1,"HasIntercept":false}}
            """);
        Assert.Null(torpedo!.TorpedoRating);
        Assert.Null(torpedo.RatingBreakdown);
    }
}
