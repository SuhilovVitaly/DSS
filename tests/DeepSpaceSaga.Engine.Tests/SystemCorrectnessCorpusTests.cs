using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SystemCorrectnessCorpusTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (string scenario in new[] { "Default", "Default_500", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly" })
            foreach (int planets in new[] { 3, 7 })
                foreach (int belts in new[] { 2, 5 })
                    foreach (int days in new[] { 50, 75 })
                        yield return [scenario, planets, belts, days];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void HundredSeedScenarioBoundaryCorpus(string name, int planets, int belts, int days)
    {
        var registry = SeededWorldBootstrapTests.Registry();
        var source = SeededWorldBootstrapTests.Scenario(name);
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"))! with
        { MinPlanets = planets, MaxPlanets = planets, MinBelts = belts, MaxBelts = belts, StartMinDays = days, StartMaxDays = days };
        var fields = JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!;
        for (ulong seed = 1; seed <= 100; seed++)
        {
            try
            {
                using var engine = new SimulationEngine(registry);
                engine.ConfigureStationResourceFields(fields);
                engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: config);
                var first = engine.CaptureSnapshot();
                var map = first.SolarSystemMap!;
                Assert.Single(first.Objects, o => o.ObjectType == "Sun");
                Assert.Equal(planets, map.Planets.Length); Assert.Equal(belts, map.Belts.Length);
                var ship = first.Objects.Single(o => o.ObjectId == first.PlayerShipObjectId);
                double radius = ship.MaxSpeedKmS!.Value * days * 86400 / 300 * 10;
                Assert.InRange(Math.Abs(Math.Sqrt(ship.X * ship.X + ship.Y * ship.Y) - radius), 0, 1e-6);
                Assert.Equal(first.Objects.Length, first.Objects.Select(o => o.ObjectId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
                var corridors = map.Belts.Select(b => (Min: b.InnerRadius, Max: b.OuterRadius)).ToList();
                foreach (var planet in map.Planets)
                {
                    var orbit = map.Orbits.Single(o => o.ObjectId == planet.ObjectId).Elements;
                    corridors.Add((orbit.SemiMinorAxis - planet.VisualRadius, orbit.SemiMajorAxis + planet.VisualRadius));
                }
                var ordered = corridors.OrderBy(c => c.Min).ToArray();
                for (int i = 1; i < ordered.Length; i++)
                    Assert.True(ordered[i].Min >= ordered[i - 1].Max + config.OrbitClearanceWorld - 1e-6);
                foreach (var speed in Enum.GetValues<SimulationSpeed>())
                {
                    long time = (long)speed * 123;
                    var snapshot = engine.CaptureSnapshotForTests(time * 300, speed, time);
                    var sample = snapshot.Objects.First(o => o.ObjectType == "Planet");
                    var orbit = sample.Orbit!;
                    double phase = (orbit.InitialPhase + orbit.PhaseOffsetDegrees) * Math.PI / 180 +
                        Math.Tau * (time * 300d / orbit.OrbitalPeriodMs);
                    Assert.Equal(orbit.SemiMajorAxis * Math.Sin(phase), sample.X, 6);
                    Assert.Equal(-orbit.SemiMinorAxis * Math.Cos(phase), sample.Y, 6);
                }
                long end = 12300;
                var saved = engine.CaptureSaveStateForTests(end * 300, SimulationSpeed.Speed0, end);
                using var loaded = new SimulationEngine(registry);
                loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true), isSave: true);
                GeneratedWorldPersistenceTests.EqualWorld(
                    engine.CaptureSnapshotForTests((end + 1234) * 300, SimulationSpeed.Speed0, end + 1234),
                    loaded.CaptureSnapshotForTests((end + 1234) * 300, SimulationSpeed.Speed0, end + 1234));
            }
            catch (Exception ex)
            {
                throw new Xunit.Sdk.XunitException($"seed={seed} scenario={name} planets={planets} belts={belts} days={days} generator=1 config={JsonSerializer.Serialize(config)}: {ex}");
            }
        }
    }

    [Fact]
    public void InvalidGenerationCorpus()
    {
        using var engine = OrbitalSynchronizationTests.Create(42);
        var source = SeededWorldBootstrapTests.Scenario();
        string baseline = ScenarioLoader.Serialize(engine.CaptureSaveState());
        foreach (var config in new[] { GenerationInputSchemaTests.Config() with { MaxPlacementAttempts = 1, OrbitClearanceWorld = 1e30 },
            GenerationInputSchemaTests.Config() with { OrbitSpeedFraction = 0 },
            GenerationInputSchemaTests.Config() with { GeneratorVersion = 2 } })
        {
            Assert.ThrowsAny<Exception>(() => engine.LoadScenario(source, generation: config));
            Assert.Equal(baseline, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        }
        var bad = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o =>
            o.ObjectId == source.GameState.PlayerShipObjectId ? o with { Modules = o.Modules!.Select(m => m with { ModuleTypeId = "missing" }).ToArray() } : o).ToArray()
            }
        };
        Assert.ThrowsAny<Exception>(() => engine.LoadScenario(bad, generation: GenerationInputSchemaTests.Config()));
        Assert.Equal(baseline, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }
}
