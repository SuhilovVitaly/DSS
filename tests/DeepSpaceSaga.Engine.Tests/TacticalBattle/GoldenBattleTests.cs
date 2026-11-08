using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class GoldenBattleTests
{
    private static readonly Lazy<TacticalBattleCatalog> Catalog = new(AssemblyTests.Catalog);
    public static IEnumerable<object[]> Cases()
    {
        // Historical rows supply corpus inputs only; their simultaneous results are not a new-rule oracle.
        using var rows = ReferenceFixtureTests.Open("battles");
        return rows.RootElement.GetProperty("data").EnumerateArray().Select(row => new object[]
            { row.GetProperty("seed").GetInt32(), row.GetProperty("tactic").GetString()! }).ToArray();
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void NewRuleBattlesRepeatAcrossDirectAndExplicitPaths(int seed, string tactic)
    {
        var direct = Run(Catalog.Value, seed, tactic, false);
        var explicitBattle = Run(Catalog.Value, seed, tactic, true);
        Assert.Equal(Signature(direct), Signature(explicitBattle));
        Assert.Equal(42, Battle.Create(Catalog.Value, "initial", seed).Random.Calls);
    }

    [Fact]
    public void HandComputedInitiativeCasesMatchEngineAndOracle()
    {
        foreach (var example in InitiativeReferenceCases.All)
        {
            var expected = InitiativeReferenceOracle.Resolve(example);
            Assert.Equal(example.ExpectedHull, expected.Hull);
            Assert.Equal(example.ExpectedShots, expected.Steps.Where(s => s.Fired).Select(s => s.ShooterId));
            var battle = InitiativeReferenceCases.Create(example);
            long calls = battle.Random.Calls;
            var actual = TacticalBattleSalvo.Fire(battle, (d, _) => new(example.Units.Single(u => u.Id == d.Id).TargetId, false));
            Assert.Equal(expected.Calls, battle.Random.Calls - calls);
            Assert.Equal(expected.Steps.Select(s => (s.ShooterId, s.Reason)), actual.Steps.Select(s => (s.ShooterId, s.SkipReason)));
            Assert.Equal(example.ExpectedShots, actual.Shots.Select(s => s.Shooter.Id));
            for (int i = 0; i < actual.Steps.Length; i++)
            {
                var units = actual.Steps[i].UnitsAfter;
                foreach (var state in expected.Steps[i].Units)
                {
                    var unit = units.Single(u => u.Id == state.Id);
                    Assert.Equal((state.Hull, state.Armor, state.Jammed), (unit.Hull, unit.Armor, unit.Jammed));
                }
            }
        }
    }

    internal static Battle Run(TacticalBattleCatalog catalog, long seed, string tactic, bool explicitOrders)
    {
        var battle = Battle.Create(catalog, "initial", seed, new Dictionary<string, string> { ["A"] = tactic });
        if (!explicitOrders) { battle.Run(TacticalBattleAi.Plan); return battle; }
        long shots = 0, hits = 0, hull = 0, armor = 0;
        while (battle.Result is null && battle.Turn < catalog.Rules.SeriesTurnCap)
        {
            battle.Planning(TacticalBattleAi.Plan);
            battle.Movement();
            if (battle.Result is not null) break;
            var queue = battle.CaptureFireOrder();
            battle.Firing((d, candidates) => new(TacticalBattleSalvo.DefaultTarget(d, candidates)?.Target.Id, false));
            var salvo = battle.LastSalvo!;
            Assert.Equal(queue.Select(d => d.Id), salvo.Steps.Select(s => s.ShooterId));
            Assert.Equal(Enumerable.Range(0, queue.Length), salvo.Steps.Select(s => s.Index));
            Assert.Equal(queue.Select(d => d.Initiative).OrderDescending(), queue.Select(d => d.Initiative));
            shots += salvo.Shots.Length;
            hits += salvo.Shots.Count(s => s.Hit);
            hull += salvo.Damage.Sum(d => (long)d.Hull);
            armor += salvo.Damage.Sum(d => (long)d.Armor);
            bool ended = false;
            foreach (var step in salvo.Steps)
            {
                if (ended) { Assert.Equal(TacticalFireSkipReason.BattleEnded, step.SkipReason); Assert.Null(step.Shot); }
                if (step.Outcome is not null) ended = true;
                Assert.All(step.UnitsAfter, u => { Assert.InRange(u.Hull, 0, u.MaxHull); Assert.InRange(u.Armor, 0, u.MaxArmor); });
            }
        }
        battle.Run(TacticalBattleAi.Plan);
        Assert.Equal((shots, hits, hull, armor), (battle.Statistics.Shots, battle.Statistics.Hits, battle.Statistics.Hull, battle.Statistics.Armor));
        return battle;
    }

    internal static string Signature(Battle battle) => JsonSerializer.Serialize(new
    {
        Result = TacticalBattleSnapshotBuilder.Outcome(battle.Result),
        Units = TacticalBattleSnapshotBuilder.Units(battle),
        battle.InitiativeRandom.State,
        battle.InitiativeRandom.Calls
    });
}
