using System.Text.Json;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class GoldenBattleTests
{
    private static readonly Lazy<TacticalBattleCatalog> Catalog = new(AssemblyTests.Catalog);
    private static readonly JsonElement[] Rows = Read("battles").EnumerateArray().Select(e => e.Clone()).ToArray();
    public static IEnumerable<object[]> Cases() => Rows.Select((row, index) => new object[]
        { row.GetProperty("seed").GetInt32(), row.GetProperty("tactic").GetString()!, index });

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryIndependentBattleSummaryMatches(int seed, string tactic, int index)
    {
        var expected = Rows[index];
        var battle = Battle.Create(Catalog.Value, "initial", seed, new Dictionary<string, string> { ["A"] = tactic });
        Eq(battle, expected.GetProperty("assemblyCalls").GetInt64(), battle.Random.Calls, "assembly.calls");
        var result = battle.Run(TacticalBattleAi.Plan);
        Summary(battle, result, expected);
    }

    [Fact]
    public void SeedOneMatchesEveryStageWithoutInjectingReferenceStateOrRandom()
    {
        var trace = Read("trace-seed-1");
        var battle = Battle.Create(Catalog.Value, "initial", 1);
        Eq(battle, trace.GetProperty("assembly").GetProperty("calls").GetInt64(), battle.Random.Calls, "assembly.calls");
        var assembly = trace.GetProperty("assembly").GetProperty("units");
        for (int i = 0; i < battle.Drones.Length; i++)
        {
            var expected = assembly[i];
            var actual = battle.Drones[i];
            Eq(battle, expected.GetProperty("id").GetString(), actual.Id, "assembly.id");
            Near(battle, expected, "x", actual.Pose.X, "assembly." + actual.Id);
            Near(battle, expected, "y", actual.Pose.Y, "assembly." + actual.Id);
            Near(battle, expected, "course", actual.Pose.Course, "assembly." + actual.Id);
            Eq(battle, expected.GetProperty("accuracy").GetInt32(), actual.Accuracy, "assembly.accuracy");
            Eq(battle, expected.GetProperty("maneuver").GetInt32(), actual.Maneuver, "assembly.maneuver");
        }
        foreach (var expectedTurn in trace.GetProperty("turns").EnumerateArray())
        {
            Assert.Null(battle.Result);
            battle.Planning(TacticalBattleAi.Plan);
            Eq(battle, expectedTurn.GetProperty("number").GetInt32(), battle.Turn, "planning.turn");
            Eq(battle, expectedTurn.GetProperty("planningCalls").GetInt64(), battle.Random.Calls, "planning.calls");
            var orders = expectedTurn.GetProperty("orders");
            var active = battle.Drones.Where(d => d.IsActive).ToArray();
            Eq(battle, orders.GetArrayLength(), active.Length, "planning.orders.count");
            for (int i = 0; i < active.Length; i++)
            {
                Eq(battle, orders[i].GetProperty("id").GetString(), active[i].Id, "planning.id");
                Eq(battle, orders[i].GetProperty("role").GetString(), active[i].Role, "planning." + active[i].Id + ".role");
                Near(battle, orders[i], "speed", active[i].Order.Speed, "planning." + active[i].Id);
                Near(battle, orders[i], "turn", active[i].Order.Turn, "planning." + active[i].Id);
            }
            battle.Movement();
            Assert.Null(battle.Result);
            Eq(battle, expectedTurn.GetProperty("salvoCallsBefore").GetInt64(), battle.Random.Calls, "movement.calls");
            var positions = expectedTurn.GetProperty("positions");
            Eq(battle, positions.GetArrayLength(), active.Length, "movement.positions.count");
            for (int i = 0; i < active.Length; i++)
            {
                Eq(battle, positions[i].GetProperty("id").GetString(), active[i].Id, "movement.id");
                Near(battle, positions[i], "x", active[i].Pose.X, "movement." + active[i].Id);
                Near(battle, positions[i], "y", active[i].Pose.Y, "movement." + active[i].Id);
                Near(battle, positions[i], "course", active[i].Pose.Course, "movement." + active[i].Id);
            }
            battle.Firing();
            Eq(battle, expectedTurn.GetProperty("salvoCallsAfter").GetInt64(), battle.Random.Calls, "firing.calls");
            var shots = expectedTurn.GetProperty("shots");
            Eq(battle, shots.GetArrayLength(), battle.LastSalvo!.Shots.Length, "firing.shots.count");
            for (int i = 0; i < shots.GetArrayLength(); i++)
            {
                var actual = battle.LastSalvo.Shots[i];
                var expected = shots[i];
                string context = "firing." + actual.Shooter.Id;
                Eq(battle, expected.GetProperty("shooter").GetString(), actual.Shooter.Id, context + ".shooter");
                Eq(battle, expected.GetProperty("target").GetString(), actual.Target.Id, context + ".target");
                Near(battle, expected, "distance", actual.Distance, context);
                Near(battle, expected, "angle", actual.Angle, context);
                Near(battle, expected, "chance", actual.Chance, context);
                Eq(battle, expected.GetProperty("hit").GetInt32() != 0, actual.Hit, context + ".hit");
                Eq(battle, expected.GetProperty("damage").GetInt32(), actual.Damage, context + ".damage");
                Eq(battle, expected.GetProperty("rear").GetInt32() != 0, actual.Rear, context + ".rear");
                Eq(battle, expected.GetProperty("kill").GetInt32() != 0, actual.Kill, context + ".kill");
            }
            foreach (var expected in expectedTurn.GetProperty("states").EnumerateArray())
            {
                var unit = battle.Drones.Cast<TacticalUnit>().Concat(battle.Carriers).First(u => u.Id == expected.GetProperty("id").GetString());
                string context = "state." + unit.Id;
                Eq(battle, expected.GetProperty("hull").GetInt32(), unit.Hull, context + ".hull");
                Eq(battle, expected.GetProperty("armor").GetInt32(), unit.Armor, context + ".armor");
                if (unit is not TacticalDrone drone) continue;
                Eq(battle, expected.GetProperty("status").GetString(), drone.Status.ToString().ToLowerInvariant(), context + ".status");
                Eq(battle, expected.GetProperty("jammed").GetInt32() != 0, drone.Jammed, context + ".jammed");
                for (int i = 0; i < drone.Engines.Length; i++) Eq(battle, expected.GetProperty("engines")[i].GetDouble(), drone.Engines[i].Factor, context + ".engine[" + i + "]");
            }
        }
        Assert.NotNull(battle.Result);
        Summary(battle, battle.Result!, Rows[0]);
    }

    private static void Summary(Battle battle, TacticalBattleResult result, JsonElement expected)
    {
        string outcome = result.Kind switch { TacticalOutcomeKind.A => "A", TacticalOutcomeKind.B => "B", TacticalOutcomeKind.Draw => "draw", _ => "timeout" };
        Eq(battle, expected.GetProperty("outcome").GetString(), outcome, "result.outcome");
        Eq(battle, expected.GetProperty("turns").GetInt32(), result.Turns, "result.turns");
        for (int i = 0; i < 2; i++)
        {
            Eq(battle, expected.GetProperty("losses")[i].GetInt32(), result.Losses[i], "result.losses[" + i + "]");
            Eq(battle, expected.GetProperty("carrierHull")[i].GetInt32(), result.CarrierHull[i], "result.carrierHull[" + i + "]");
        }
        Eq(battle, expected.GetProperty("shots").GetInt64(), result.Statistics.Shots, "statistics.shots");
        Eq(battle, expected.GetProperty("hits").GetInt64(), result.Statistics.Hits, "statistics.hits");
        Eq(battle, expected.GetProperty("rearHits").GetInt64(), result.Statistics.RearHits, "statistics.rearHits");
        Eq(battle, expected.GetProperty("systems").GetInt64(), result.Statistics.Systems, "statistics.systems");
        Eq(battle, expected.GetProperty("disabled").GetInt64(), result.Statistics.Disabled, "statistics.disabled");
        Eq(battle, expected.GetProperty("totalCalls").GetInt64(), result.RandomCalls, "result.randomCalls");
        var reason = expected.GetProperty("reason");
        var actual = Assert.Single(result.Reasons);
        string carrierId = battle.Carrier(reason.GetProperty("side").GetString()!).Id;
        switch (reason.GetProperty("kind").GetString())
        {
            case "NoDrones": Eq(battle, carrierId, Assert.IsType<TacticalNoDrones>(actual).CarrierId, "reason.noDrones"); break;
            case "CarrierDestroyed": Eq(battle, carrierId, Assert.IsType<TacticalCarrierDestroyed>(actual).CarrierId, "reason.carrierDestroyed"); break;
            default: throw new InvalidOperationException("Unknown independent reason.");
        }
    }

    private static JsonElement Read(string name)
    {
        using var doc = ReferenceFixtureTests.Open(name);
        Assert.Equal(2, doc.RootElement.GetProperty("header").GetProperty("formatVersion").GetInt32());
        return doc.RootElement.GetProperty("data").Clone();
    }
    private static void Eq<T>(Battle battle, T expected, T actual, string field) => Assert.True(EqualityComparer<T>.Default.Equals(expected, actual),
        $"seed={battle.Seed}, A={battle.Tactics[0]}, B={battle.Tactics[1]}, turn={battle.Turn}, {field}: expected {expected}, actual {actual}");
    private static void Near(Battle battle, JsonElement expected, string field, double actual, string context)
    {
        double value = expected.GetProperty(field).GetDouble();
        Assert.True(Math.Abs(value - actual) <= 0.0000005,
            $"seed={battle.Seed}, turn={battle.Turn}, {context}.{field}: expected {value:R}, actual {actual:R} (source precision 6 decimals)");
    }
}
