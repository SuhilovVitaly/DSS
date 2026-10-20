using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class PointCombatUS0001TK0003Tests
{
    [Theory]
    [InlineData(CombatEncounterState.Idle)]
    [InlineData(CombatEncounterState.Active)]
    [InlineData(CombatEncounterState.Won)]
    [InlineData(CombatEncounterState.Lost)]
    public void Encounter_roundtrip_preserves_state_deadlines_and_all_counters(CombatEncounterState state)
    {
        var stats = new CombatEncounterStats(1, 2, 3, 4, 5, 6, 7, 8, 9000000000, 10000000000);
        long? ended = state is CombatEncounterState.Won or CombatEncounterState.Lost ? 11250 : null;
        var encounter = new CombatEncounterSnapshot(state, 1000, ended, stats, 10.25);
        var snapshot = new AuthoritativeSnapshot(7, 987654321, SimulationSpeed.Speed0, [],
            SimulationTimeMs: 11250, Encounter: encounter);

        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;

        Assert.Equal(encounter, restored.Encounter);
        Assert.Equal(10.25, restored.Encounter!.DurationSeconds);
        Assert.Equal(ended, restored.Encounter.EndedAtMotionMs);
        Assert.Equal(1000, restored.Encounter.StartedAtMotionMs);
        Assert.Equal(stats, restored.Encounter.Stats);
        Assert.Equal(10, typeof(CombatEncounterStats).GetProperties().Length);
        Assert.All(typeof(CombatEncounterStats).GetProperties(), property => Assert.Equal(typeof(long), property.PropertyType));
        Assert.Equal(SessionOutcomeState.Running, restored.EffectiveOutcome.State);
    }

    [Fact]
    public void Enemy_status_has_no_module_collection()
    {
        string[] expected = ["Damaged", "ImmobilizedUntilMotionMs", "Destroyed"];
        var status = new EnemyShipStatusSnapshot(true, 12345, false);
        var motion = new ObjectMotionSnapshot("enemy-1", 1, 2, 0, 90, EnemyStatus: status);
        var json = JsonSerializer.SerializeToNode(motion)!.AsObject();

        Assert.Equal(expected.Order(), typeof(EnemyShipStatusSnapshot).GetProperties().Select(p => p.Name).Order());
        Assert.Equal(expected.Order(), json["EnemyStatus"]!.AsObject().Select(p => p.Key).Order());
        Assert.Equal(status, JsonSerializer.Deserialize<ObjectMotionSnapshot>(json.ToJsonString())!.EnemyStatus);
        json["EnemyStatus"]!.AsObject()["ModuleHealth"] = new JsonArray();
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ObjectMotionSnapshot>(json.ToJsonString()));
    }

    [Fact]
    public void Legacy_snapshots_do_not_invent_an_encounter_or_enemy_status()
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new ObjectMotionSnapshot("enemy", 0, 0, 0, 0)]);
        var json = JsonSerializer.Serialize(snapshot);
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json)!;
        Assert.DoesNotContain("\"Encounter\"", json);
        Assert.DoesNotContain("\"EnemyStatus\"", json);
        Assert.Null(restored.Encounter);
        Assert.Null(restored.Objects[0].EnemyStatus);
        Assert.Equal(SessionOutcomeState.Running, restored.EffectiveOutcome.State);
    }
}
