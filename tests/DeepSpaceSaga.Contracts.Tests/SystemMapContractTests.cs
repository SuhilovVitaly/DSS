using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class SystemMapContractTests
{
    [Fact]
    public void LegacySnapshotRoundTrip()
    {
        const string legacy = """
            {"SnapshotSequence":1,"GameTimeMs":0,"CurrentSpeed":0,
             "Objects":[{"ObjectId":"ship","X":1,"Y":2,"SpeedKmS":0,"Direction":0}]}
            """;
        var snapshot = JsonSerializer.Deserialize<AuthoritativeSnapshot>(legacy)!;
        Assert.Null(snapshot.SolarSystemMap);
        Assert.Null(snapshot.Objects[0].Orbit);
        Assert.Null(snapshot.Objects[0].OrbitSampleSimulationTimeMs);
        Assert.Equal(0, snapshot.Objects[0].WorldOffsetX);
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(snapshot.Objects[0], restored.Objects[0]);
        Assert.Null(restored.SolarSystemMap);

        var empty = new SolarSystemMapSnapshot(1, 0, 1, default, default, default);
        var map = JsonSerializer.Deserialize<SolarSystemMapSnapshot>(JsonSerializer.Serialize(empty))!;
        Assert.True(map.Belts.IsEmpty);
        Assert.True(map.Planets.IsEmpty);
        Assert.True(map.Orbits.IsEmpty);
    }

    [Fact]
    public void PrecisePhaseRoundTrip()
    {
        var orbit = new OrbitalElements(720000, 720000, 25920000000L, 17, 0.000001,
            long.MaxValue - 1, "counterclockwise");
        var map = new SolarSystemMapSnapshot(1, ulong.MaxValue, 1000000,
            [new BeltMapData("belt", 800000, 900000, ulong.MaxValue)],
            [new PlanetMapData("planet", "Rocky", 10)], [new OrbitMapData("planet", orbit)]);
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new ObjectMotionSnapshot("planet", 1, 2, 0, 0, Orbit: orbit,
                OrbitSampleSimulationTimeMs: long.MaxValue, WorldOffsetX: 1, WorldOffsetY: 1)],
            SolarSystemMap: map);
        var json = JsonSerializer.Serialize(snapshot);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(ulong.MaxValue, document.RootElement.GetProperty("solarSystemMap").GetProperty("seed").GetUInt64());
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json)!;
        Assert.Equal(map.Seed, restored.SolarSystemMap!.Seed);
        Assert.Equal(map.Belts.ToArray(), restored.SolarSystemMap.Belts.ToArray());
        Assert.Equal(map.Planets.ToArray(), restored.SolarSystemMap.Planets.ToArray());
        Assert.Equal(orbit, restored.SolarSystemMap.Orbits[0].Elements);
        Assert.Equal(snapshot.Objects[0], restored.Objects[0]);
    }
}
