using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ScenarioGroupTranslationTests
{
    [Theory]
    [InlineData("Default")]
    [InlineData("Default_500")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    [InlineData("MarketProfiles")]
    [InlineData("PlayerShipOnly")]
    public void FiveScenarioIdentityMatrix(string name)
    {
        var scenario = SeededWorldBootstrapTests.Scenario(name);
        var originalJson = ScenarioLoader.Serialize(scenario);
        var translated = ScenarioGroupPlacement.Translate(scenario, 720000, 0);
        Assert.Equal(originalJson, ScenarioLoader.Serialize(scenario));
        var player = translated.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.Equal(720000, player.PositionX); Assert.Equal(0, player.PositionY);
        foreach (var source in scenario.GameState.SpaceObjects)
        {
            var actual = translated.GameState.SpaceObjects.Single(o => o.ObjectId == source.ObjectId);
            Assert.Equal(source, actual with { PositionX = source.PositionX, PositionY = source.PositionY });
            foreach (var other in scenario.GameState.SpaceObjects.Take(6))
            {
                if (source.IsDocked || other.IsDocked) continue;
                var target = translated.GameState.SpaceObjects.Single(o => o.ObjectId == other.ObjectId);
                Assert.Equal(source.PositionX - other.PositionX, actual.PositionX - target.PositionX, 6);
                Assert.Equal(source.PositionY - other.PositionY, actual.PositionY - target.PositionY, 6);
            }
        }
        var input = scenario with { GameState = scenario.GameState with { MasterSeed = 42 } };
        using var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        engine.LoadScenario(input, generation: GenerationInputSchemaTests.Config());
        var snapshot = engine.CaptureSnapshot();
        var ship = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
        Assert.InRange(Math.Sqrt(ship.X * ship.X + ship.Y * ship.Y) / 10 / ship.MaxSpeedKmS!.Value * 300 / 86400, 50, 75);
        Assert.All(snapshot.Objects.Where(o => o.ObjectType == "Station"), s => Assert.NotNull(s.Orbit));
        var save = engine.CaptureSaveStateForTests(3600000, SimulationSpeed.Speed1, 12000);
        using var restored = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true);
        Assert.Equal(JsonSerializer.Serialize(save.GameState.SolarSystem), JsonSerializer.Serialize(restored.CaptureSnapshot().SolarSystemMap));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(75)]
    public void DockingOffsetAtRadiusBoundaries(int days)
    {
        var input = SeededWorldBootstrapTests.Scenario("Docked");
        double radius = 4 * days * 86400 / 300.0 * 10;
        var output = ScenarioGroupPlacement.Translate(input, radius, 0);
        var ship = output.GameState.SpaceObjects.Single(o => o.ObjectId == input.GameState.PlayerShipObjectId);
        var station = output.GameState.SpaceObjects.Single(o => o.ObjectId == ship.DockedStationObjectId);
        Assert.Equal(1, ship.PositionX - station.PositionX, 6); Assert.Equal(1, ship.PositionY - station.PositionY, 6);
        Assert.Equal(days, Math.Sqrt(ship.PositionX * ship.PositionX + ship.PositionY * ship.PositionY) / 10 / 4 * 300 / 86400, 8);
    }

    [Fact]
    public void GeneratedResourceWorldRoundTripsAfterMovement()
    {
        using var engine = DeepSpaceSaga.Engine.Content.EngineContentLoader.CreateEngineFromSettingsFile(
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"));
        var save = engine.CaptureSaveStateForTests(3600000, SimulationSpeed.Speed1, 12000);
        Assert.NotNull(save.GameState.StationResourceFields);
        using var restored = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true);
        var expected = engine.CaptureSnapshotForTests(3900000, SimulationSpeed.Speed1, 13000);
        var actual = restored.CaptureSnapshotForTests(3900000, SimulationSpeed.Speed1, 13000);
        foreach (var obj in expected.Objects)
        {
            var other = actual.Objects.Single(o => o.ObjectId == obj.ObjectId);
            Assert.Equal(obj.X, other.X, 6); Assert.Equal(obj.Y, other.Y, 6);
        }
    }

    [Fact]
    public void AllShippedScenariosCovered()
    {
        var found = Directory.GetFiles(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Scenarios"), "scenario.json", SearchOption.AllDirectories)
            .Select(p => new DirectoryInfo(Path.GetDirectoryName(p)!).Name).Order().ToArray();
        Assert.Equal(new[] { "Default", "Default_500", "Docked", "MarketProfiles", "PlayerShipOnly", "Undocked" }, found);
    }
}
