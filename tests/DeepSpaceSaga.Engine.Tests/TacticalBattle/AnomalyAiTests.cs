using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class AnomalyAiTests
{
    private static (Battle Battle, TacticalDrone A, TacticalOrder Order) Arena(bool targets = true, double sigma = 2)
    {
        var (source, a, b) = SalvoTests.Arena(damage: 1);
        var order = new TacticalOrder(a.Limits.MinSpeed, 0);
        var end = TacticalGeometry.Integrate(a.Pose, order.Speed, order.Turn, source.Catalog.Rules.Substeps);
        b.Pose = new(targets ? end.X + 10 : 10000, end.Y, 180);
        var battle = new Battle(source.Catalog, source.ScenarioId, source.Seed, source.Drones, source.Carriers, source.Tactics, source.Random)
        { Anomalies = [new("dust", TacticalAnomalyKind.Dust, [new(end.X, end.Y, 100, sigma, sigma, 0)])] };
        battle.InitializeFireOrder(); return (battle, a, order);
    }

    [Fact]
    public void Ai_moves_end_out_of_dust_when_it_would_have_targets()
    {
        var (battle, a, order) = Arena(); var before = a.Pose;
        var changed = TacticalDustAvoidance.Apply(battle, a, order);
        Assert.NotEqual(order, changed);
        double firstTurn = a.Limits.TurnLimit(order.Speed, battle.Catalog.Rules.TurnAtMaxSpeedFraction) / 4;
        Assert.Equal(new TacticalOrder(order.Speed, firstTurn), changed);
        var end = TacticalGeometry.Integrate(a.Pose, changed.Speed, changed.Turn, battle.Catalog.Rules.Substeps);
        Assert.False(TacticalAnomalyMath.Inside(battle.Anomalies, TacticalAnomalyKind.Dust,
            end.X, end.Y, 100, 170));
        Assert.Equal(before, a.Pose);
    }

    [Fact]
    public void Ai_keeps_order_without_targets_or_exit()
    {
        foreach (var arena in new[] { Arena(targets: false), Arena(sigma: 1e6) })
            Assert.Equal(arena.Order, TacticalDustAvoidance.Apply(arena.Battle, arena.A, arena.Order));
        var (battle, a, order) = Arena(); a.Jammed = true;
        Assert.Equal(order, TacticalDustAvoidance.Apply(battle, a, order));
    }

    [Fact]
    public void Dust_avoidance_draws_no_random()
    {
        var (battle, a, order) = Arena();
        var before = (battle.Random.State, battle.Random.Calls, battle.InitiativeRandom.State, battle.InitiativeRandom.Calls);
        var first = TacticalDustAvoidance.Apply(battle, a, order);
        for (int i = 0; i < 20; i++) Assert.Equal(first, TacticalDustAvoidance.Apply(battle, a, order));
        Assert.Equal(before, (battle.Random.State, battle.Random.Calls, battle.InitiativeRandom.State, battle.InitiativeRandom.Calls));
    }

    [Fact]
    public void Empty_anomalies_keep_orders_bit_exact()
    {
        var (battle, a, _) = SalvoTests.Arena();
        var empty = new Battle(battle.Catalog, battle.ScenarioId, battle.Seed, battle.Drones, battle.Carriers, battle.Tactics, battle.Random);
        foreach (var order in new[] { new TacticalOrder(0, -121.1234), new TacticalOrder(5000, 876.1234) })
            Assert.Equal(order, TacticalDustAvoidance.Apply(empty, a, order));
    }

    [Fact]
    public void Plan_uses_avoidance_for_every_active_drone_without_mutating_poses()
    {
        var (battle, a, _) = Arena();
        var (source, _, _) = Arena();
        var baseline = new Battle(source.Catalog, source.ScenarioId, source.Seed, source.Drones, source.Carriers, source.Tactics, source.Random);
        var poses = battle.Drones.Select(d => d.Pose).ToArray();
        TacticalBattleAi.Plan(baseline); TacticalBattleAi.Plan(battle);
        Assert.Equal(poses, battle.Drones.Select(d => d.Pose));
        // Existing role steering may draw RNG; the appended avoidance must add none.
        Assert.Equal((baseline.Random.State, baseline.Random.Calls), (battle.Random.State, battle.Random.Calls));
        foreach (var d in battle.Drones.Where(d => d.IsActive))
            Assert.Equal(TacticalDustAvoidance.Apply(battle, d, baseline.Drones.Single(b => b.Id == d.Id).Order), d.Order);
    }
}
