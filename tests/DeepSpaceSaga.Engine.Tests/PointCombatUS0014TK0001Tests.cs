using System.Text.Json;
using DeepSpaceSaga.CombatAcceptance;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0014TK0001Tests
{
    [Fact]
    public void Production_bot_uses_public_commands_and_production_registry()
    {
        var harness = new ProductionCombatBalanceHarness(TorpedoImpactTests.ClientRoot);
        var dodge = harness.Run(3001, "dodge");
        var stationary = harness.Run(3001, "static");
        foreach (var run in new[] { dodge, stationary })
        {
            Assert.Equal(harness.ScenarioHash, run.ScenarioHash);
            Assert.Equal(harness.CatalogHash, run.CatalogHash);
            Assert.Equal(0, run.LegacyChanceFacts);
            Assert.True(run.PlayerTorpedoes > 0 && run.HostileTorpedoes > 0);
            Assert.True(run.Countermissiles > 0, "Production public counter-launch commands must actually execute.");
            Assert.All(run.Commands, c =>
            {
                Assert.Contains(c.Command.CommandType, new[] { CombatCommandTypes.Launch, DefenseCommandTypes.Launch, ManeuverCommandTypes.Evade });
                Assert.Null(c.Command.TargetObjectId);
                Assert.NotNull(c.Status);
            });
        }
        Assert.True(dodge.Evades > 0, "Dodge policy must execute actual maneuver.evade commands.");
        Assert.Equal(0, stationary.Evades);
    }

    [Fact]
    public void Production_policy_replays_deterministically_without_wall_clock_or_hidden_ai_inputs()
    {
        var harness = new ProductionCombatBalanceHarness(TorpedoImpactTests.ClientRoot);
        var first = harness.Run(3002, "dodge");
        var second = harness.Run(3002, "dodge");
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
    }

    [Fact]
    public void Arc_aim_uses_two_confirmed_physical_second_turns_and_independent_circle_geometry()
    {
        ProductionCombatBalanceHarness.Pose[] poses = [new(0, 0, 0, 0, 3), new(1000, 0, 0, 30, 3), new(2000, 0, 0, 60, 3)];
        Assert.Equal(30, ProductionCombatBalanceHarness.ConfirmedTurn(poses, poses[2]));
        Assert.Equal(0, ProductionCombatBalanceHarness.ConfirmedTurn(poses[..2], poses[1]));
        Assert.Equal(0, ProductionCombatBalanceHarness.ConfirmedTurn([poses[0], poses[1], poses[2] with { Direction = 80 }], poses[2] with { Direction = 80 }));
        var p = ProductionCombatBalanceHarness.ArcPosition(poses[0], 30, 3);
        double radius = 180 / Math.PI;
        Assert.Equal(radius, p.X, 9); Assert.Equal(-radius, p.Y, 9);
        var straight = ProductionCombatBalanceHarness.ArcPosition(poses[0], 0, 3);
        Assert.Equal(0, straight.X); Assert.Equal(-90, straight.Y);
    }
}
