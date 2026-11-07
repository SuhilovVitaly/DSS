using System.Collections.Immutable;
using DeepSpaceSaga.Engine.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class FireOrderTests
{
    [Fact]
    public void DescendingInitiativeInterleavesBothSides()
    {
        var original = Battle.Create(AssemblyTests.Catalog(), null, 1);
        TacticalDrone[] drones = [original.Drones[0], original.Drones[4], original.Drones[1]];
        int[] initiative = [80, 60, 40];
        for (int i = 0; i < drones.Length; i++) drones[i].Initiative = initiative[i];
        var battle = Clone(original, drones.ToImmutableArray(), 1);
        Assert.Throws<InvalidOperationException>(() => battle.CaptureFireOrder());
        battle.InitializeFireOrder();
        Assert.Equal(drones.Select(d => d.Id), battle.CaptureFireOrder().Select(d => d.Id));
        Assert.Equal(new[] { "A", "B", "A" }, battle.CaptureFireOrder().Select(d => d.Side));
        Assert.Equal(0, battle.InitiativeRandom.Calls);
    }

    [Fact]
    public void EqualInitiativeUsesSeededOnceOnlyPermutation()
    {
        var battle = Equal(1);
        var original = battle.Drones.Select(d => d.Id).ToArray();
        battle.InitializeFireOrder();
        // Independent integer Mulberry32 + Fisher-Yates vector for domain seed 1 XOR INIT.
        int[] permutation = [9, 2, 4, 5, 0, 6, 8, 1, 7, 3];
        Assert.Equal(permutation.Select(i => original[i]), battle.CaptureFireOrder().Select(d => d.Id));
        Assert.Equal(9, battle.InitiativeRandom.Calls);
        battle.InitializeFireOrder();
        Assert.Equal(9, battle.InitiativeRandom.Calls);
        Assert.Equal(original, battle.Drones.Select(d => d.Id));
        Assert.Equal(0, battle.Random.Calls);
    }

    [Fact]
    public void FilteringLossesAndPlayerSideDoesNotRerollOrReorderSurvivors()
    {
        var battle = Equal(1);
        battle.InitializeFireOrder();
        var order = battle.CaptureFireOrder();
        uint state = battle.InitiativeRandom.State;
        order[2].Status = TacticalDroneStatus.Destroyed;
        order[5].Status = TacticalDroneStatus.Disabled;
        for (int turn = 0; turn < 3; turn++)
        {
            battle.Turn++;
            Assert.Equal(order.Where(d => d.IsActive).Select(d => d.Id), battle.CaptureFireOrder().Select(d => d.Id));
            Assert.Equal(state, battle.InitiativeRandom.State);
        }
        // Player ownership is a session concern and is deliberately absent from battle ordering.
        Assert.Contains(battle.CaptureFireOrder(), d => d.Side == "A");
        Assert.Contains(battle.CaptureFireOrder(), d => d.Side == "B");
    }

    [Fact]
    public void SeveralFixedSeedsPermitBothSidesToLead()
    {
        var leaders = new HashSet<string>();
        foreach (long seed in new long[] { 1, 2, 3, 4, 5, 6, 7, 8 })
        {
            var one = Equal(seed);
            var two = Equal(seed);
            one.InitializeFireOrder(); two.InitializeFireOrder();
            Assert.Equal(one.CaptureFireOrder().Select(d => d.Id), two.CaptureFireOrder().Select(d => d.Id));
            leaders.Add(one.CaptureFireOrder()[0].Side);
        }
        Assert.Equal(new[] { "A", "B" }, leaders.Order(StringComparer.Ordinal));
    }

    private static Battle Equal(long seed)
    {
        var original = Battle.Create(AssemblyTests.Catalog(), null, seed);
        foreach (var drone in original.Drones) drone.Initiative = 50;
        return Clone(original, original.Drones, seed);
    }

    private static Battle Clone(Battle original, ImmutableArray<TacticalDrone> drones, long seed) =>
        new(original.Catalog, original.ScenarioId, seed, drones, original.Carriers, original.Tactics, new Mulberry32(seed));
}
