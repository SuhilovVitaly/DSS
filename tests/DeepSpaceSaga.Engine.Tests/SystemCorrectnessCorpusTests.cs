using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using Xunit.Abstractions;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SystemCorrectnessCorpusTests(ITestOutputHelper output)
{
    private static readonly Lazy<GameDataRegistry> Catalog = new(SeededWorldBootstrapTests.Registry);
    private static readonly Lazy<GameDataRegistry> Speed2 = new(() => WithSpeed(2));
    private static readonly Lazy<GameDataRegistry> Speed4 = new(() => WithSpeed(4));
    private static GameDataRegistry WithSpeed(int speed) => Catalog.Value.WithModuleTypes([.. Enumerable.Range(0, Catalog.Value.ModuleTypes.Count)
        .Select(i => Catalog.Value.ModuleTypes.GetDefinition(i)).Select(m => m.MaxSpeedMps is > 0 ? m with { MaxSpeedMps = speed * 1000 } : m)]);
    internal static GameDataRegistry V2Registry(ulong seed) => ((seed - 1) & 32) == 0 ? Speed2.Value : Speed4.Value;
    internal static SolarSystemGenerationConfig LegacyConfig() => EngineContentLoader.LoadSolarSystemGenerationConfig(
        Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"))! with
    { GeneratorVersion = 1, BeltWidthFraction = .08, Clusters = new(3, 5, 10, 12, .5, 2, 3, 7, 15, 35) };

    public static IEnumerable<object[]> V2Ranges()
    {
        foreach (string name in new[] { "Default", "Default_500", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly" })
            foreach (int start in new[] { 1, 21, 41, 61, 81 }) yield return [name, $"{start:000}-{start + 19:000}", start];
    }

    internal static SolarSystemGenerationConfig V2BoundaryConfig(ulong seed)
    {
        int bits = (int)(seed - 1) % 64;
        int planets = (bits & 1) == 0 ? 3 : 7, belts = (bits & 2) == 0 ? 2 : 5;
        int clusters = (bits & 4) == 0 ? 3 : 5, stations = (bits & 8) == 0 ? 10 : 12;
        int days = (bits & 16) == 0 ? 50 : 75, ai = clusters == 3 ? 2 : 4;
        var config = V2FullSystemPlacementTests.Config();
        return config with
        {
            MinPlanets = planets,
            MaxPlanets = planets,
            MinBelts = belts,
            MaxBelts = belts,
            StartMinDays = days,
            StartMaxDays = days,
            Clusters = config.Clusters! with
            { MinClusters = clusters, MaxClusters = clusters, MinStations = stations, MaxStations = stations },
            Ai = config.Ai! with { MinBases = ai, MaxBases = ai }
        };
    }

    internal static SimulationEngine V2Create(string name, ulong seed, SolarSystemGenerationConfig config) =>
        CreateWithRegistry(name, seed, config, V2Registry(seed));

    private static SimulationEngine CreateWithRegistry(string name, ulong seed, SolarSystemGenerationConfig config, GameDataRegistry registry)
    {
        var engine = new SimulationEngine(registry);
        engine.ConfigureStationResourceFields(JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!);
        var source = SeededWorldBootstrapTests.Scenario(name);
        try { engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: config); }
        catch { engine.Dispose(); throw; }
        return engine;
    }

    internal static void CorpusFailure(Exception error, string scenario, ulong seed, SolarSystemGenerationConfig config, string stage, ScenarioFile? world = null)
    {
        string run = Environment.GetEnvironmentVariable("DSS_VALIDATION_RUN") ?? Path.Combine(
            "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale", "corpus-repro-" + Guid.NewGuid().ToString("N"));
        string root = Path.GetFullPath("D:/DeepSpaceSaga/artifacts/validation/");
        run = Path.GetFullPath(run);
        if (!run.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Corpus evidence must stay under workspace validation root.", error);
        Directory.CreateDirectory(run);
        string path = Path.Combine(run, $"repro-{stage}-{scenario}-{seed}.json");
        var source = SeededWorldBootstrapTests.Scenario(scenario);
        double speed = config.GeneratorVersion == 2 ? (((seed - 1) & 32) == 0 ? 2 : 4) :
            Catalog.Value.ModuleTypes.GetDefinition(Catalog.Value.ModuleTypes.GetIndex("module.engine.basic")).MaxSpeedMps!.Value / 1000;
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Scenario = scenario,
            Seed = seed,
            GeneratorVersion = config.GeneratorVersion,
            Stage = stage,
            ModuleMaxSpeedKmS = speed,
            Config = config,
            Input = source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } },
            World = world,
            Error = error.ToString()
        }));
        throw new Xunit.Sdk.XunitException($"seed={seed};scenario={scenario};version={config.GeneratorVersion};stage={stage};repro={path};config={JsonSerializer.Serialize(config)}: {error}");
    }

    [Theory]
    [MemberData(nameof(V2Ranges))]
    public void V2ScenarioSeedRangeBoundaries(string scenario, string range, int seedStart)
    {
        Assert.Equal($"{seedStart:000}-{seedStart + 19:000}", range);
        for (ulong seed = (ulong)seedStart; seed < (ulong)seedStart + 20; seed++)
        {
            var config = V2BoundaryConfig(seed); string stage = "generation"; ScenarioFile? world = null;
            try
            {
                using var engine = V2Create(scenario, seed, config);
                world = engine.CaptureSaveState(); var snapshot = engine.CaptureSnapshot(); stage = "system-oracle";
                Assert.Equal(2, snapshot.SolarSystemMap!.GeneratorVersion);
                Assert.Equal(config.MinPlanets, snapshot.SolarSystemMap.Planets.Length);
                Assert.Equal(config.MinBelts, snapshot.SolarSystemMap.Belts.Length);
                Assert.Equal(config.Clusters!.MinClusters, snapshot.ClusterMap!.Clusters.Length);
                Assert.Equal(config.Clusters.MinClusters * config.Clusters.MinStations, snapshot.ClusterMap.Stations.Length);
                var player = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
                double reference = snapshot.ClusterMap.Geography!.ReferenceSpeedKmS;
                Assert.Equal(((seed - 1) & 32) == 0 ? 2 : 4, reference); Assert.Equal(reference, player.MaxSpeedKmS);
                Assert.InRange(double.Hypot(player.X, player.Y) / (reference * 86400 / 300 * 10), config.StartMinDays - 1e-8, config.StartMaxDays + 1e-8);
                var metrics = ClusterCorrectnessCorpusTests.VerifyV2Geography(world);
                output.WriteLine($"scenario={scenario};seed={seed};range={range};version=2;reference={reference};min={metrics.Minimum};nearest={metrics.NearestMinimum}..{metrics.NearestMaximum};laneMin={metrics.LaneMinimum};config={JsonSerializer.Serialize(config)}");
            }
            catch (Exception error) { CorpusFailure(error, scenario, seed, config, stage, world); }
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (string scenario in new[] { "Default", "Default_500", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly" })
            foreach (int planets in new[] { 3, 7 })
                foreach (int belts in new[] { 2, 5 })
                    foreach (int days in new[] { 50, 75 })
                        foreach (int start in new[] { 1, 21, 41, 61, 81 })
                            yield return [scenario, planets, belts, days, $"{start:000}-{start + 19:000}", start];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void HundredSeedScenarioBoundaryCorpus(string name, int planets, int belts, int days, string range, int seedStart)
    {
        Assert.Equal($"{seedStart:000}-{seedStart + 19:000}", range);
        var registry = SeededWorldBootstrapTests.Registry();
        var source = SeededWorldBootstrapTests.Scenario(name);
        var config = LegacyConfig() with
        { MinPlanets = planets, MaxPlanets = planets, MinBelts = belts, MaxBelts = belts, StartMinDays = days, StartMaxDays = days };
        var fields = JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!;
        for (ulong seed = (ulong)seedStart; seed < (ulong)seedStart + 20; seed++)
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
                var expected = engine.CaptureSnapshotForTests((end + 1234) * 300, SimulationSpeed.Speed0, end + 1234);
                GeneratedWorldPersistenceTests.EqualWorld(expected,
                    loaded.CaptureSnapshotForTests((end + 1234) * 300, SimulationSpeed.Speed0, end + 1234));
            }
            catch (Exception ex)
            {
                CorpusFailure(ex, name, seed, config, "legacy-system");
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
