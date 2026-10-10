using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class FullMapCorrectnessCorpusTests
{
    private static readonly string[] Scenarios = ["Default", "Default_500", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly"];
    public static IEnumerable<object[]> Cases() => Scenarios.SelectMany(s => new[] { new object[] { s, false }, [s, true] });
    private static SolarSystemGenerationConfig Config(bool max)
    {
        var c = EngineContentLoader.LoadSolarSystemGenerationConfig(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"))!;
        return c with
        {
            MinPlanets = max ? 7 : 3,
            MaxPlanets = max ? 7 : 3,
            MinBelts = max ? 5 : 2,
            MaxBelts = max ? 5 : 2,
            StartMinDays = max ? 75 : 50,
            StartMaxDays = max ? 75 : 50,
            Clusters = c.Clusters! with { MinClusters = max ? 5 : 3, MaxClusters = max ? 5 : 3, MinStations = max ? 12 : 10, MaxStations = max ? 12 : 10 },
            Ai = c.Ai! with { MinBases = max ? 4 : 2, MaxBases = max ? 4 : 2 }
        };
    }
    private static SimulationEngine Create(string scenario, ulong seed, SolarSystemGenerationConfig config)
    {
        var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        engine.ConfigureStationResourceFields(JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!);
        var source = SeededWorldBootstrapTests.Scenario(scenario);
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: config);
        return engine;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void HundredSeedFullMapMatrix(string scenario, bool maximum)
    {
        var config = Config(maximum);
        for (ulong seed = 1; seed <= 100; seed++)
        {
            try
            {
                using var engine = Create(scenario, seed, config); var save = engine.CaptureSaveState(); var map = save.GameState.AiMap!;
                using var repeat = Create(scenario, seed, config); MapEnvironmentSaveTests.SameMap(save, repeat.CaptureSaveState());
                Assert.Equal(maximum ? 4 : 2, map.Bases.Length); Assert.Equal(map.Bases.Length, map.Territories.Length);
                Assert.Equal(5, map.Fields.Length); Assert.Equal(2, map.PointsOfInterest.Length);
                var ids = save.GameState.SpaceObjects.Select(o => o.ObjectId).Concat(save.GameState.SolarSystem!.Belts.Select(b => b.Id))
                    .Concat(save.GameState.ClusterMap!.Clusters.Select(c => c.Id)).Concat(save.GameState.ClusterMap.Links.Select(l => l.Id))
                    .Concat(map.Territories.Select(t => t.Id)).Concat(map.Fields.Select(f => f.Id)).Concat(map.PointsOfInterest.Select(p => p.ObjectId)).ToArray();
                Assert.Equal(ids.Length, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
                foreach (var ai in map.Bases)
                {
                    var obj = save.GameState.SpaceObjects.Single(o => o.ObjectId == ai.ObjectId);
                    Assert.Equal("Ai", ai.Owner); Assert.True(obj.IsKnown); Assert.Null(obj.MarketProfileId); Assert.Empty(obj.Inventory ?? []);
                    Assert.Empty(obj.ProducingModules ?? []); Assert.DoesNotContain(save.GameState.ClusterMap.Stations, s => s.ObjectId == ai.ObjectId);
                }
                var proof = engine.CaptureAiPlacementValidation()!; Assert.True(proof.IsValid); Assert.Empty(proof.Violations);
                foreach (long epoch in new long[] { 0, 86400000, 7 * 86400000L, 30 * 86400000L, 100 * 86400000L, 365 * 86400000L }.Concat(proof.CriticalEpochs).Distinct())
                    CheckGeometry(save, epoch / 300);
                var saved = engine.CaptureSaveStateForTests(300000, SimulationSpeed.Speed0, 1000);
                using var loaded = new SimulationEngine(SeededWorldBootstrapTests.Registry()); loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true), true);
                MapEnvironmentSaveTests.SameMap(saved, loaded.CaptureSaveState());
                GeneratedWorldPersistenceTests.EqualWorld(engine.CaptureSnapshotForTests(600000, SimulationSpeed.Speed0, 2000), loaded.CaptureSnapshotForTests(600000, SimulationSpeed.Speed0, 2000));
            }
            catch (Exception e) { throw new Xunit.Sdk.XunitException($"seed={seed};scenario={scenario};maximum={maximum};config={JsonSerializer.Serialize(config)};stage=full-map: {e}"); }
        }
    }

    // Independent geometry oracle: direct trigonometry and projection, no generation/placement/motion helper.
    private static (double X, double Y) Position(SpaceObjectData obj, long time)
    {
        if (obj.Orbit is not { } o) return (obj.PositionX, obj.PositionY);
        double turns = ((time - o.EpochSimulationTimeMs) * 300d % o.OrbitalPeriodMs) / o.OrbitalPeriodMs;
        double angle = (o.InitialPhase + o.PhaseOffsetDegrees) * Math.PI / 180 + (o.OrbitDirection == "clockwise" ? 1 : -1) * 2 * Math.PI * turns;
        return (o.SemiMajorAxis * Math.Sin(angle) + obj.WorldOffsetX, -o.SemiMinorAxis * Math.Cos(angle) + obj.WorldOffsetY);
    }
    private static double Distance((double X, double Y) c, (double X, double Y) a, (double X, double Y) b)
    {
        double x = b.X - a.X, y = b.Y - a.Y, length = x * x + y * y;
        double fraction = length == 0 ? 0 : Math.Clamp(((c.X - a.X) * x + (c.Y - a.Y) * y) / length, 0, 1);
        return double.Hypot(c.X - a.X - fraction * x, c.Y - a.Y - fraction * y);
    }
    private static void CheckGeometry(ScenarioFile save, long motion)
    {
        var state = save.GameState; var map = state.AiMap!; var clusters = state.ClusterMap!;
        var poses = state.SpaceObjects.ToDictionary(o => o.ObjectId, o => Position(o, motion));
        var discs = map.Territories.Select(t => (Center: poses[t.BaseObjectId], Radius: t.PatrolRadiusKm * 10)).ToList();
        foreach (var cluster in clusters.Clusters)
            foreach (var link in clusters.Links.Where(l => cluster.StationIds.Contains(l.FromStationId) && cluster.StationIds.Contains(l.ToStationId)))
                foreach (var disc in discs) Assert.True(Distance(disc.Center, poses[link.FromStationId], poses[link.ToStationId]) > disc.Radius);
        double radius = state.SolarSystem!.SystemRadius;
        discs.Add(((0, 0), Math.Max(1, radius * .01)));
        var stations = clusters.Stations.Select(s => poses[s.ObjectId]).ToArray(); var nodes = stations.ToList();
        // Outer-ring waypoints permit a geometrically independent detour around the Sun and territories.
        for (int i = 0; i < 64; i++) nodes.Add((radius * .99 * Math.Cos(i * Math.Tau / 64), radius * .99 * Math.Sin(i * Math.Tau / 64)));
        var seen = new HashSet<int> { 0 }; var queue = new Queue<int>(); queue.Enqueue(0);
        while (queue.TryDequeue(out int current))
            for (int i = 0; i < nodes.Count; i++)
                if (!seen.Contains(i) && discs.All(d => Distance(d.Center, nodes[current], nodes[i]) > d.Radius + 1e-6))
                { seen.Add(i); queue.Enqueue(i); }
        for (int i = 0; i < stations.Length; i++) Assert.Contains(i, seen);
        foreach (var ai in map.Bases.Where(b => b.ParentObjectId is not null)) Assert.Equal(poses[ai.ParentObjectId!], poses[ai.ObjectId]);
    }

    [Fact]
    public void InformationalLayersHaveNoEffects()
    {
        foreach (string scenario in Scenarios)
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var config = Config(seed % 2 == 0);
                using var full = Create(scenario, seed, config); using var control = Create(scenario, seed, config with { Environment = null, PoiTemplates = null });
                foreach (long motion in new[] { 0L, 12000, 288000 })
                {
                    var a = full.CaptureSnapshotForTests(motion * 300, SimulationSpeed.Speed0, motion);
                    var b = control.CaptureSnapshotForTests(motion * 300, SimulationSpeed.Speed0, motion);
                    Assert.Equal(JsonSerializer.Serialize(b), JsonSerializer.Serialize(a with { AiMap = b.AiMap }));
                    var x = full.CaptureSaveStateForTests(motion * 300, SimulationSpeed.Speed0, motion).GameState;
                    var y = control.CaptureSaveStateForTests(motion * 300, SimulationSpeed.Speed0, motion).GameState;
                    Assert.Equal(JsonSerializer.Serialize(y), JsonSerializer.Serialize(x with { AiMap = y.AiMap }));
                }
            }
    }

    [Fact]
    public void AiBaseIdCannotAliasTradeDirection()
    {
        using var engine = Create("MarketProfiles", 42, Config(false)); var saved = engine.CaptureSaveState();
        var clusters = saved.GameState.ClusterMap!;
        var invalid = saved with
        {
            GameState = saved.GameState with
            {
                ClusterMap = clusters with
                { Links = clusters.Links.SetItem(0, clusters.Links[0] with { Id = saved.GameState.AiMap!.Bases[0].ObjectId.ToLowerInvariant() }) }
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(invalid, true));
        Assert.Equal(ScenarioLoader.Serialize(saved), ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void InvalidEnvironmentAndHostileAccessCorpus()
    {
        for (ulong seed = 1; seed <= 10; seed++)
        {
            using var engine = Create("MarketProfiles", seed, Config(seed % 2 == 0)); var before = engine.CaptureSaveState();
            foreach (double radius in new[] { 0, -1, double.NaN, double.PositiveInfinity })
            {
                var source = SeededWorldBootstrapTests.Scenario("MarketProfiles");
                Assert.Throws<ContentException>(() => engine.LoadScenario(source, generation: Config(false) with { Ai = Config(false).Ai! with { DefenceRadiusKm = radius } }));
                Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(engine.CaptureSaveState()));
            }
            foreach (var ai in before.GameState.AiMap!.Bases)
            {
                var snapshot = engine.CaptureSnapshot(); string module = snapshot.InstalledModules.First(m => m.CommandTypeIds.Contains(NavigationComputerCommandTypes.Dock)).ModuleId;
                string id = "forbidden-" + ai.ObjectId;
                engine.ReceiveCommand(new(id, seed, snapshot.PlayerShipObjectId!, module, NavigationComputerCommandTypes.Dock, TargetObjectId: ai.ObjectId));
                Assert.Equal("station_access_denied", engine.CaptureSnapshot().CommandResults.Single(r => r.CommandId == id).ReasonCode);
            }
        }
        // All ingress/trade/dialogue variants and malformed one-of/cycle/namespace values have their own full-suite regression tests.
        using var blocked = Create("MarketProfiles", 1, Config(false)); var unchanged = ScenarioLoader.Serialize(blocked.CaptureSaveState());
        var impossible = Config(false) with { Ai = Config(false).Ai! with { PatrolRadiusKm = 1e12, MaxPlacementAttempts = 1 } };
        Assert.Throws<ScenarioException>(() => blocked.LoadScenario(SeededWorldBootstrapTests.Scenario("MarketProfiles"), generation: impossible));
        Assert.Equal(unchanged, ScenarioLoader.Serialize(blocked.CaptureSaveState()));
    }
}
