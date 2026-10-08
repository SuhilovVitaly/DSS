using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class GeneratedWorldPersistenceTests
{
    [Theory]
    [InlineData("Default")]
    [InlineData("Default_500")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    [InlineData("MarketProfiles")]
    [InlineData("PlayerShipOnly")]
    public void WorldJsonContinuationMatrix(string name)
    {
        using var original = SimulationEngine.CreateFromScenarioFile(
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"),
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Scenarios", name, "scenario.json"));
        original.SetSpeed(SimulationSpeed.Speed0);
        var before = original.CaptureSnapshotForTests(GameCalendar.DayMs + 12345, SimulationSpeed.Speed0, 12345);
        var save = original.CaptureSaveStateForTests(before.GameTimeMs, SimulationSpeed.Speed0, before.SimulationTimeMs);
        Assert.Equal(SaveFormat.CurrentSaveFormatVersion, save.SaveFormatVersion);
        using var restored = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true);
        EqualWorld(before, restored.CaptureSnapshot());
        EqualWorld(original.CaptureSnapshotForTests(GameCalendar.DayMs + 3333333, SimulationSpeed.Speed4, 13345),
            restored.CaptureSnapshotForTests(GameCalendar.DayMs + 3333333, SimulationSpeed.Speed4, 13345));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActiveApproachAndRealDockContinue(bool docked)
    {
        using var original = OrbitalSynchronizationTests.Create(42);
        long time;
        if (docked)
        {
            time = OrbitalDockingDepartureTests.ApproachAndSynchronize(original);
            OrbitalDockingDepartureTests.Dock(original, time);
        }
        else
        {
            OrbitalSynchronizationTests.Send(original, NavigationComputerCommandTypes.Approach);
            OrbitalSynchronizationTests.At(original, 0);
            time = 12345;
            Assert.NotNull(OrbitalSynchronizationTests.Player(OrbitalSynchronizationTests.At(original, time)).ApproachRoute);
        }
        var save = original.CaptureSaveStateForTests(time * 300, SimulationSpeed.Speed0, time);
        using var restored = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true);
        EqualWorld(OrbitalSynchronizationTests.At(original, time + 1577), OrbitalSynchronizationTests.At(restored, time + 1577));
    }

    internal static void EqualWorld(AuthoritativeSnapshot expected, AuthoritativeSnapshot actual)
    {
        Assert.Equal(expected.GameTimeMs, actual.GameTimeMs);
        Assert.Equal(expected.SimulationTimeMs, actual.SimulationTimeMs);
        Assert.Equal(JsonSerializer.Serialize(expected.SolarSystemMap), JsonSerializer.Serialize(actual.SolarSystemMap));
        Assert.Equal(expected.Objects.Select(o => o.ObjectId), actual.Objects.Select(o => o.ObjectId));
        foreach (var obj in expected.Objects)
        {
            var other = actual.Objects.Single(o => o.ObjectId == obj.ObjectId);
            Assert.InRange(Math.Abs(obj.X - other.X), 0, 1e-6);
            Assert.InRange(Math.Abs(obj.Y - other.Y), 0, 1e-6);
            Assert.Equal(obj.SpeedKmS, other.SpeedKmS, 10);
            Assert.Equal(obj.Direction, other.Direction, 8);
            Assert.Equal(obj.Orbit, other.Orbit);
            Assert.Equal(obj.IsDocked, other.IsDocked);
            Assert.Equal(obj.DockedStationObjectId, other.DockedStationObjectId);
            Assert.Equal(obj.ApproachRoute, other.ApproachRoute);
        }
    }

    [Fact]
    public void LegacyWorldIsNotRegenerated()
    {
        using var original = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        original.LoadScenario(SeededWorldBootstrapTests.Scenario());
        var save = TradingEconomySaveSchemaTests.WithoutNewContinuation(original.CaptureSaveState(), 11);
        using var loaded = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true,
            generation: GenerationInputSchemaTests.Config());
        Assert.Null(loaded.CaptureSnapshot().SolarSystemMap);
        Assert.Equal(save.GameState.SpaceObjects.Select(o => o.ObjectId), loaded.CaptureSnapshot().Objects.Select(o => o.ObjectId));
    }

    [Fact]
    public void InvalidOrbitReferencesAreAtomic()
    {
        using var engine = OrbitalSynchronizationTests.Create(42);
        var save = engine.CaptureSaveState();
        var map = save.GameState.SolarSystem!;
        var first = map.Orbits[0];
        var invalidMaps = new[] {
            map with { GeneratorVersion = 999 }, map with { Seed = map.Seed + 1 },
            map with { Orbits = map.Orbits.Add(first) },
            map with { Orbits = map.Orbits.SetItem(0, first with { ObjectId = "missing" }) },
            map with { Orbits = map.Orbits.RemoveAt(0) },
            map with { Planets = [] }, map with { Belts = [] },
            map with { Belts = map.Belts.SetItem(1, map.Belts[0] with { Id = map.Belts[1].Id }) },
            map with { Planets = map.Planets.SetItem(0, map.Planets[0] with { VisualRadius = map.SystemRadius }) }
        };
        var invalid = invalidMaps.Select(m => save with { GameState = save.GameState with { SolarSystem = m } }).ToList();
        invalid.Add(save with { SaveFormatVersion = SaveFormat.CurrentSaveFormatVersion + 1 });
        invalid.Add(save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Where(o => o.ObjectType != "Sun").ToArray() } });
        invalid.Add(save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Append(save.GameState.SpaceObjects[0] with { ObjectId = save.GameState.SpaceObjects[0].ObjectId.ToLowerInvariant() }).ToArray() } });
        string baseline = ScenarioLoader.Serialize(save);
        foreach (var bad in invalid)
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
            Assert.Equal(baseline, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        }
    }
}
