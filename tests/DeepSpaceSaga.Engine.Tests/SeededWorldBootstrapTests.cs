using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SeededWorldBootstrapTests
{
    internal static string ClientRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) dir = dir.Parent;
            return Path.Combine(dir!.FullName, "src", "DeepSpaceSaga.Client");
        }
    }
    internal static GameDataRegistry Registry() => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(ClientRoot, "Settings.json"), out _, out _);
    internal static ScenarioFile Scenario(string name = "Default") => ScenarioLoader.LoadFromFile(
        Path.Combine(ClientRoot, "Scenarios", name, "scenario.json"));

    [Fact]
    public void NamedStartStreamGoldenVector()
    {
        // Independently calculated unsigned FNV-1a UTF-16 + SplitMix64 vector.
        var rng = new SolarSystemGenerator.GeneratorRng(1, "start", 0);
        Assert.Equal(6092774483564337224UL, rng.Next());
        Assert.Equal(1709763660766830426UL, rng.Next());
        Assert.Equal(10821393825090694023UL, rng.Next());
    }

    [Fact]
    public void SameSeedSameWorld()
    {
        var registry = Registry();
        var source = Scenario();
        var config = GenerationInputSchemaTests.Config();
        string? first = null;
        for (ulong seed = 1; seed <= 100; seed++)
        {
            using var a = new SimulationEngine(registry);
            using var b = new SimulationEngine(registry);
            var input = source with { GameState = source.GameState with { MasterSeed = seed } };
            a.LoadScenario(input, generation: config);
            b.LoadScenario(input, generation: config);
            var snapshot = a.CaptureSnapshot();
            if (seed == 1)
            {
                using var restored = new SimulationEngine(registry);
                restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(a.CaptureSaveState()), true), isSave: true);
                Assert.Equal(JsonSerializer.Serialize(snapshot.SolarSystemMap), JsonSerializer.Serialize(restored.CaptureSnapshot().SolarSystemMap));
            }
            var json = JsonSerializer.Serialize(snapshot);
            Assert.Equal(json, JsonSerializer.Serialize(b.CaptureSnapshot()));
            if (first is not null) Assert.NotEqual(first, json);
            first = json;
            Assert.Single(snapshot.Objects.Where(o => o.ObjectType == "Sun"));
            var sun = snapshot.Objects.Single(o => o.ObjectType == "Sun");
            Assert.Equal(0, sun.X); Assert.Equal(0, sun.Y);
            Assert.InRange(snapshot.SolarSystemMap!.Belts.Length, 2, 5);
            Assert.InRange(snapshot.SolarSystemMap.Planets.Length, 3, 7);
            Assert.All(snapshot.SolarSystemMap.Planets, p => Assert.Contains(p.Kind, new[] { "Rocky", "Icy", "Gas" }));
        }
        using var legacy = new SimulationEngine(registry);
        legacy.LoadScenario(source);
        Assert.Null(legacy.CaptureSnapshot().SolarSystemMap);
    }

    [Theory]
    [InlineData(4000, 0, false)]
    [InlineData(4000, 700, true)]
    [InlineData(2000, 0, true)]
    [InlineData(2000, 700, false)]
    public void ActualPlayerStartDays(int vmaxMps, int initialSpeed, bool docked)
    {
        var registry = GameDataRegistry.Create([], [new ModuleTypeDefinition("engine", "Engine", 1, 1, 100, 0, [], MaxSpeedMps: vmaxMps)], [], []);
        var ship = new SpaceObjectData("ship", "PlayerShip", "Permanent", null, 100, 200, initialSpeed, 0, "Linear", null, null,
            [new ShipModuleData("engine", "engine", [], 100, "On", "Ready", null, null)], IsDocked: docked, DockedStationObjectId: docked ? "station" : null);
        var station = new SpaceObjectData("station", "Station", "Permanent", null, 90, 190, 0, 0, "Stationary", null, null, null);
        var input = new ScenarioFile(new("test", "Test"), new(0, "Speed0", "ship", null, [ship, station]));
        foreach (int belts in new[] { 2, 5 })
        {
            var config = GenerationInputSchemaTests.Config() with { MinBelts = belts, MaxBelts = belts };
            var output = SolarSystemGenerator.Generate(input, config, registry, 42);
            var player = output.GameState.SpaceObjects.Single(o => o.ObjectId == "ship");
            double distanceKm = Math.Sqrt(player.PositionX * player.PositionX + player.PositionY * player.PositionY) / 10;
            double days = distanceKm / (vmaxMps / 1000.0) * 300 / 86400;
            Assert.InRange(days, 50, 75);
            Assert.Equal(belts, output.GameState.SolarSystem!.Belts.Length);
            var target = output.GameState.SpaceObjects.Single(o => o.ObjectId == "station");
            Assert.Equal(docked ? 1 : 10, player.PositionX - target.PositionX, 6);
            Assert.Equal(docked ? 1 : 10, player.PositionY - target.PositionY, 6);
        }
    }

    [Fact]
    public void ExhaustedPlacementIsAtomic()
    {
        using var engine = new SimulationEngine(Registry());
        var scenario = Scenario();
        engine.LoadScenario(scenario);
        var before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var config = GenerationInputSchemaTests.Config() with { MaxPlacementAttempts = 3, OrbitClearanceWorld = 1e30 };
        var input = scenario with { GameState = scenario.GameState with { MasterSeed = 1 } };
        var ex = Assert.Throws<ScenarioException>(() => engine.LoadScenario(input, generation: config));
        Assert.Contains("seed=1 version=1 stage=placement attempt=3", ex.Message);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        var collision = scenario.GameState.SpaceObjects[0] with { ObjectId = "sys-sun", ObjectType = "Asteroid", Modules = null, ShipClassId = null };
        input = input with { GameState = input.GameState with { SpaceObjects = [.. input.GameState.SpaceObjects, collision] } };
        Assert.Contains("collision", Assert.Throws<ScenarioException>(() => engine.LoadScenario(input, generation: GenerationInputSchemaTests.Config())).Message);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }
}
