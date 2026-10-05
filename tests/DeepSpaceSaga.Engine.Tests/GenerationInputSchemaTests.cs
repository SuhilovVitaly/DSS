using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class GenerationInputSchemaTests
{
    internal static SolarSystemGenerationConfig Config() => new(1, 1, 32, 3, 7, 2, 5,
        50, 75, 0.01, 0.05, 1000, 12, 128, ["Default"]);

    [Fact]
    public void InvalidConfigDoesNotPublish()
    {
        var config = Config();
        foreach (var invalid in new[] {
            config with { SchemaVersion = 2 }, config with { GeneratorVersion = 0 },
            config with { MaxPlacementAttempts = 0 }, config with { MinPlanets = 2 },
            config with { MaxPlanets = 8 }, config with { MinBelts = 1 }, config with { MaxBelts = 6 },
            config with { StartMinDays = 49 }, config with { StartMaxDays = 76 },
            config with { OrbitSpeedFraction = double.NaN }, config with { BeltWidthFraction = double.PositiveInfinity },
            config with { OrbitClearanceWorld = -1 }, config with { AsteroidsPerBelt = -1 },
            config with { DecorationSamplesPerBelt = -1 }, config with { EnabledScenarios = ["Default", "default"] } })
            Assert.Throws<ContentException>(() => SolarSystemGeneration.ValidateConfig(invalid));

        var folder = Path.Combine(Path.GetTempPath(), "dss-solar-schema-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            var settings = Path.Combine(folder, "Settings.json");
            var content = Path.Combine(folder, "solar.json");
            File.WriteAllText(settings, """{"typeData":{},"defaultScenario":"scenario.json"}""");
            Assert.Null(EngineContentLoader.LoadSolarSystemGenerationConfig(settings));
            foreach (var value in new[] { "null", "42", "\"\"", "\"missing.json\"" })
            {
                File.WriteAllText(settings, "{\"typeData\":{\"solarSystem\":" + value + "}}");
                Assert.Contains(folder, Assert.Throws<ContentException>(() =>
                    EngineContentLoader.LoadSolarSystemGenerationConfig(settings)).Message);
            }
            File.WriteAllText(settings, """{"typeData":{"solarSystem":"solar.json"}}""");
            File.WriteAllText(content, JsonSerializer.Serialize(config));
            Assert.Equal(1, EngineContentLoader.LoadSolarSystemGenerationConfig(settings)!.GeneratorVersion);
            foreach (var json in new[] {
                JsonSerializer.Serialize(config).Replace("\"schemaVersion\":1", "\"schemaVersion\":9"),
                JsonSerializer.Serialize(config).Replace("\"schemaVersion\":1", "\"unknown\":1,\"schemaVersion\":1"),
                JsonSerializer.Serialize(config).Replace("\"orbitSpeedFraction\":0.01", "\"orbitSpeedFraction\":1e999") })
            {
                File.WriteAllText(content, json);
                Assert.Contains(content, Assert.Throws<ContentException>(() =>
                    EngineContentLoader.LoadSolarSystemGenerationConfig(settings)).Message);
            }
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void JsonLoaderRoundTrip()
    {
        const string legacy = """
            {"scenarioMetadata":{"scenarioId":"test","name":"test"},"gameState":{
            "gameTimeMs":0,"currentSpeed":"Speed0","playerShipObjectId":"ship","spaceObjects":[
            {"objectId":"ship","objectType":"PlayerShip","persistenceType":"Permanent",
            "positionX":0,"positionY":0,"speedMps":0,"directionDegrees":0,"movementType":"Linear"}]}}
            """;
        var scenario = ScenarioLoader.LoadFromJson(legacy);
        Assert.Null(scenario.GameState.SolarSystem);
        Assert.Null(scenario.GameState.SpaceObjects[0].Orbit);
        var orbit = new OrbitalElements(720000, 720000, 300000, 17, 0.000001, 0, "clockwise");
        var planet = new SpaceObjectData("planet", "Planet", "Permanent", "Planet", 0, -720000, 0, 0,
            "Orbital", null, null, null, Orbit: orbit);
        var map = new SolarSystemMapSnapshot(1, ulong.MaxValue, 1000000,
            [new BeltMapData("belt", 800000, 900000, 5), new BeltMapData("belt2", 930000, 970000, 6)],
            [new PlanetMapData("planet", "Rocky", 10), new PlanetMapData("planet2", "Icy", 10), new PlanetMapData("planet3", "Gas", 10)],
            [new OrbitMapData("planet", orbit), new OrbitMapData("planet2", orbit with { SemiMajorAxis = 300000, SemiMinorAxis = 300000 }),
             new OrbitMapData("planet3", orbit with { SemiMajorAxis = 400000, SemiMinorAxis = 400000 })]);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                MasterSeed = ulong.MaxValue,
                SolarSystem = map,
                SpaceObjects = [scenario.GameState.SpaceObjects[0], planet,
                    planet with { ObjectId = "planet2", Orbit = map.Orbits[1].Elements },
                    planet with { ObjectId = "planet3", Orbit = map.Orbits[2].Elements },
                    new("sun", "Sun", "Permanent", "Sun", 0, 0, 0, 0, "Stationary", null, null, null)]
            }
        };
        var restored = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(scenario));
        Assert.Equal(orbit, restored.GameState.SpaceObjects[1].Orbit);
        Assert.Equal(ulong.MaxValue, restored.GameState.SolarSystem!.Seed);
        foreach (var invalid in new[] {
            orbit with { SemiMajorAxis = double.NaN }, orbit with { SemiMinorAxis = -1 },
            orbit with { SemiMajorAxis = 1 }, orbit with { OrbitalPeriodMs = 0 },
            orbit with { InitialPhase = 360 }, orbit with { PhaseOffsetDegrees = 1 },
            orbit with { EpochSimulationTimeMs = -1 }, orbit with { OrbitDirection = "unknown" } })
            Assert.Throws<ScenarioException>(() => SolarSystemGeneration.ValidateOrbit(invalid, "planet"));
        foreach (var invalid in new[] {
            map with { GeneratorVersion = 2 }, map with { Seed = 0 },
            map with { Planets = [new PlanetMapData("planet", "Unknown", 1)] },
            map with { Planets = [new PlanetMapData("missing", "Rocky", 1)] },
            map with { Orbits = [] }, map with { Orbits = [map.Orbits[0], map.Orbits[0]] },
            map with { Belts = [new BeltMapData("belt", 10, 1, 0)] } })
            Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
                scenario with { GameState = scenario.GameState with { SolarSystem = invalid } })));
    }
}
