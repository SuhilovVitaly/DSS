using System.Collections.Immutable;
using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class CombatEventTests
{
    [Fact]
    public void Impact_event_roundtrips_final_trail_and_object_ids()
    {
        var impact = new CombatImpactSnapshot(17, "torpedo-3", "owner", "launcher", "target", "obstacle",
            1234.125, 12.5, -3.75, [new(1000, new(0, 0, 90, 3, 0, 234.125), 1)], 150, "obstacle", "wreck-1");
        var snapshot = new AuthoritativeSnapshot(1, 900000, SimulationSpeed.Speed0, [], CombatImpacts: [impact]);
        string json = JsonSerializer.Serialize(snapshot);
        var copy = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json)!;
        Assert.Equal(json, JsonSerializer.Serialize(copy));
        var fact = Assert.Single(copy.CombatImpacts);
        Assert.Equal(1234.125, fact.MotionTimeMs);
        Assert.Equal(234.125, Assert.Single(fact.FinalTrail).Segment.DurationMs);
        Assert.Equal((17L, "torpedo-3", "owner", "launcher", "target", "obstacle", "wreck-1"),
            (fact.EventId, fact.TorpedoObjectId, fact.OwnerObjectId, fact.LauncherModuleId,
                fact.TargetObjectId, fact.HitObjectId, fact.WreckObjectId));
        // Repeated journal delivery preserves identity; replacing a session requires
        // the consumer to clear its IDs even if the new session starts at ID 1 again.
        var seen = new HashSet<long>();
        Assert.True(seen.Add(fact.EventId));
        Assert.False(seen.Add(Assert.Single(copy.CombatImpacts).EventId));
        seen.Clear();
        Assert.True(seen.Add(fact.EventId));
        Assert.True(snapshot.Objects.IsEmpty);
    }

    [Fact]
    public void Legacy_snapshot_has_empty_combat_impacts()
    {
        const string json = """{"SnapshotSequence":1,"GameTimeMs":0,"CurrentSpeed":0,"Objects":[]}""";
        var legacy = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json)!;
        Assert.True(legacy.CombatImpacts.IsDefaultOrEmpty);
        var encoded = JsonSerializer.Serialize(legacy);
        Assert.Contains("\"CombatImpacts\":[]", encoded);
        Assert.True(JsonSerializer.Deserialize<AuthoritativeSnapshot>(encoded)!.CombatImpacts.IsEmpty);
        var impact = new CombatImpactSnapshot(1, "p", "o", "m", "t", "h", 0, 0, 0);
        var copy = JsonSerializer.Deserialize<CombatImpactSnapshot>(JsonSerializer.Serialize(impact))!;
        Assert.Equal(ImmutableArray<TrailSegment>.Empty, copy.FinalTrail);
        Assert.Null(copy.DestroyedObjectId);
        Assert.Null(copy.WreckObjectId);
    }
}
