using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class FireForecastTests
{
    [Fact]
    public async Task Salvo_strips_armor_in_queue_order_and_stops_at_disable_threshold()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire");
        var frame = scene.Frame;
        var target = frame.Battle.Units.First(u => u.Side != frame.Battle.PlayerSide && u.Kind == TacticalUnitKind.Drone)
            with { Armor = 10, Hull = 12, MaxHull = 40 };
        var drones = frame.FirePlan!.Rows.Take(2).Select(row => row.Drone with { Jammed = false, InDust = false }).ToArray();
        TacticalFirePlanRow Row(int index, int damage, int piercing, double chance, bool rear = false, double rearKill = 0)
        {
            var drone = drones[index];
            drone = drone with { Build = drone.Build! with { DamageMin = damage, DamageMax = damage, Piercing = piercing } };
            return new(drone, new(drone.Id, [new(target.Id, 10, 0, rear, rearKill, chance)], target.Id), target.Id);
        }
        var battle = frame.Battle with
        {
            Units = [target, .. drones],
            Rules = frame.Battle.Rules with { DisabledHullFraction = .25 },
            FireQueue = [new(0, drones[0].Id, 2), new(1, drones[1].Id, 1)]
        };
        var plan = new TacticalFirePlan([Row(1, 10, 0, .5), Row(0, 2, 2, .5)], drones[1].Id);
        var forecast = Assert.IsType<TacticalSalvoForecast>(TacticalSalvoForecast.Calculate(battle, plan));
        Assert.Equal(new TacticalSalvoForecast(2, .5, 0, 10), forecast);

        plan = plan with { Rows = [Row(0, 10, 0, .5), Row(1, 10, 0, .5)] };
        Assert.Equal(new TacticalSalvoForecast(2, .25, 0, 20), TacticalSalvoForecast.Calculate(battle, plan));

        plan = plan with { Rows = [Row(1, 1, 0, .5, rear: true, rearKill: .5)] };
        Assert.Equal(new TacticalSalvoForecast(1, .25, 0, 12), TacticalSalvoForecast.Calculate(battle, plan));
        Assert.Null(TacticalSalvoForecast.Calculate(battle, plan with { Rows = [plan.Rows[0] with { TargetId = null }] }));
    }

    [Fact]
    public async Task Closest_to_a_valid_shot_uses_both_range_and_angle_and_ignores_disabled_units()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire");
        var frame = scene.Frame;
        var drone = frame.FirePlan!.Selected!.Drone with { Pose = new(0, 0, 0), InDust = true };
        drone = drone with { Build = drone.Build! with { Range = 100, ArcDegrees = 30 } };
        var enemy = frame.Battle.Units.First(u => u.Side != drone.Side && u.Kind == TacticalUnitKind.Drone);
        var ahead = enemy with { Id = "ahead", State = TacticalUnitState.Air, Pose = new(120, 0, 0) };
        var behind = ahead with { Id = "behind", Pose = new(-20, 0, 0) };
        var wreck = ahead with { Id = "wreck", State = TacticalUnitState.Disabled, Pose = new(50, 0, 0) };
        var battle = frame.Battle with { Units = [drone, behind, ahead, wreck] };
        Assert.Equal(new TacticalFireBlocker(ahead, 20, 0), TacticalFireBlocker.Nearest(battle, drone));
        var outsideArc = ahead with { Pose = new(0, 130, 0) };
        Assert.Equal(new TacticalFireBlocker(outsideArc, 30, 60), TacticalFireBlocker.Nearest(battle with { Units = [outsideArc] }, drone));
    }
}
