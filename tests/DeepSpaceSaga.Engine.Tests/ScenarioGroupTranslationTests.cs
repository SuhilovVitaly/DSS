using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ScenarioGroupTranslationTests
{
    public static IEnumerable<object[]> V2StartCases()
    {
        foreach (string scenario in new[] { "Default", "Default_500", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly" })
            foreach (int speed in new[] { 2, 4 })
                foreach (int days in new[] { 50, 75 }) yield return [scenario, speed, days];
    }

    [Theory]
    [MemberData(nameof(V2StartCases))]
    public void V2AllStartsKeepMeaningAndFiftyToSeventyFiveDays(string name, int speed, int days)
    {
        var registry = SeededWorldBootstrapTests.Registry();
        registry = registry.WithModuleTypes([.. Enumerable.Range(0, registry.ModuleTypes.Count).Select(i =>
        {
            var definition = registry.ModuleTypes.GetDefinition(i);
            return definition.MaxSpeedMps is > 0 ? definition with { MaxSpeedMps = speed * 1000 } : definition;
        })]);
        using var engine = new SimulationEngine(registry);
        engine.ConfigureStationResourceFields(JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!);
        var source = SeededWorldBootstrapTests.Scenario(name);
        var config = GenerationInputSchemaTests.V2Config() with { StartMinDays = days, StartMaxDays = days };
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 42, CurrentSpeed = "Speed0" } }, generation: config);
        var snapshot = engine.CaptureSnapshot(); var ship = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
        double actualDays = double.Hypot(ship.X, ship.Y) / (speed * 86400 / 300 * 10);
        Assert.InRange(actualDays, days - 1e-9, days + 1e-9); // C01 geometric floating-point tolerance.
        Assert.Equal(speed, snapshot.ClusterMap!.Geography!.ReferenceSpeedKmS);
        var oldShip = source.GameState.SpaceObjects.Single(o => o.ObjectId == source.GameState.PlayerShipObjectId);
        Assert.Equal(oldShip.IsDocked, ship.IsDocked);
        Assert.Equal(oldShip.DockedStationObjectId, ship.DockedStationObjectId);
        if (ship.IsDocked)
        {
            var parent = snapshot.Objects.Single(o => o.ObjectId == ship.DockedStationObjectId);
            Assert.Equal(parent.X + 1, ship.X, 6); Assert.Equal(parent.Y + 1, ship.Y, 6);
        }
        else Assert.Null(ship.Orbit);
    }

    [Fact]
    public void V2ExplicitPayloadAndReferencesArePreserved()
    {
        foreach (string name in new[] { "Default", "Default_500", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly" })
        {
            var source = SeededWorldBootstrapTests.Scenario(name);
            using var engine = StationGeographyBootstrapTests.Create(name, GenerationInputSchemaTests.V2Config());
            var objects = engine.CaptureSaveState().GameState.SpaceObjects.ToDictionary(o => o.ObjectId);
            var oldPlayer = source.GameState.SpaceObjects.Single(o => o.ObjectId == source.GameState.PlayerShipObjectId);
            string? anchorId = source.GameState.TradingMapGeneration?.StartStationObjectId ?? oldPlayer.DockedStationObjectId ??
                source.GameState.SpaceObjects.Where(o => o.ObjectType == "Station")
                    .OrderBy(o => double.Hypot(o.PositionX - oldPlayer.PositionX, o.PositionY - oldPlayer.PositionY))
                    .ThenBy(o => o.ObjectId, StringComparer.Ordinal).FirstOrDefault()?.ObjectId;
            var oldAnchor = source.GameState.SpaceObjects.FirstOrDefault(o => o.ObjectId == anchorId) ?? oldPlayer;
            var newAnchor = objects[anchorId ?? oldPlayer.ObjectId];
            foreach (var original in source.GameState.SpaceObjects)
            {
                var actual = objects[original.ObjectId];
                if (original.Name is not null) Assert.Equal(original.Name, actual.Name);
                Assert.Equal(original.ObjectType, actual.ObjectType);
                if (original.ObjectType == "Station")
                    foreach (var item in original.Inventory ?? []) Assert.Contains(actual.Inventory!, i => i.ItemTypeId == item.ItemTypeId && i.Quantity == item.Quantity);
                else if (!original.IsDocked)
                {
                    Assert.Equal(original.PositionX - oldAnchor.PositionX, actual.PositionX - newAnchor.PositionX, 6);
                    Assert.Equal(original.PositionY - oldAnchor.PositionY, actual.PositionY - newAnchor.PositionY, 6);
                }
                foreach (var module in original.Modules ?? [])
                {
                    var installed = actual.Modules!.Single(m => m.ModuleId == module.ModuleId);
                    Assert.Equal(module.ModuleTypeId, installed.ModuleTypeId);
                    foreach (var cargo in module.Cargo ?? []) Assert.Contains(installed.Cargo!, c => c.ItemTypeId == cargo.ItemTypeId && c.Quantity == cargo.Quantity);
                }
            }
            if (anchorId is null)
            {
                var solar = SolarSystemGenerator.Generate(source, GenerationInputSchemaTests.V2Config(), SeededWorldBootstrapTests.Registry(), 42);
                var intended = solar.GameState.SpaceObjects.Single(o => o.ObjectId == oldPlayer.ObjectId);
                Assert.Equal(intended.PositionX, objects[oldPlayer.ObjectId].PositionX, 6);
                Assert.Equal(intended.PositionY, objects[oldPlayer.ObjectId].PositionY, 6);
            }
        }
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("Default_500")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    [InlineData("MarketProfiles")]
    [InlineData("PlayerShipOnly")]
    public void AllScenarioIdentityMatrix(string name)
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
