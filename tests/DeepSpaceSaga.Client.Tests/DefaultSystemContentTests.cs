using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DefaultSystemContentTests
{
    [Fact]
    public void LegacyDefaultStationsRespectDayMinimum()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(Settings);
        var snapshot = engine.CaptureSnapshot();
        AllScenarioSystemContentTests.AssertGeography(snapshot);
        var source = ScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json"));
        var rules = source.GameState.TradingMapGeneration ?? source.GameState.TradingMap!.Rules;
        foreach (var station in rules.Stations)
        {
            Assert.Contains(snapshot.ClusterMap!.Stations, s => s.ObjectId == station.ObjectId && s.MarketProfileId == station.MarketProfileId);
        }
        Assert.Equal(6, snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId).MaxSpeedKmS);
    }

    internal static string Settings => Path.Combine(AppContext.BaseDirectory, "Settings.json");

    [Fact]
    public async Task DefaultContentGeneratesSystem()
    {
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Data", "Maps", "solar-system.json")));
        await using var connection = LocalGameSessionConnection.CreateFromSettingsFile(Settings);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await snapshots.MoveNextAsync());
        var map = Assert.IsType<SolarSystemMapSnapshot>(snapshots.Current.SolarSystemMap);
        Assert.InRange(map.Belts.Length, 2, 5);
        Assert.InRange(map.Planets.Length, 3, 7);
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(Settings)!;
        Assert.Equal(64, config.MaxPlacementAttempts);
        Assert.Equal(0.001, config.OrbitSpeedFraction);
        Assert.Equal(2048, config.DecorationSamplesPerBelt);
    }

    [Fact]
    public void ContentKeepsShipAndInventory()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json");
        var scenario = ScenarioLoader.LoadFromFile(path);
        using var engine = SimulationEngine.CreateFromScenarioFile(Settings, path);
        var saved = engine.CaptureSaveState();
        var snapshot = engine.CaptureSnapshot();
        Assert.NotNull(snapshot.SolarSystemMap);
        Assert.Equal(6, snapshot.Objects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId).MaxSpeedKmS);
        foreach (var original in scenario.GameState.SpaceObjects)
        {
            var actual = saved.GameState.SpaceObjects.Single(o => o.ObjectId == original.ObjectId);
            if (original.ObjectType == SpaceObjectType.Asteroid)
            {
                Assert.Equal(original.SpeedMps, actual.SpeedMps, 5);
                var first = snapshot.Objects.Single(o => o.ObjectId == original.ObjectId);
                Assert.NotNull(first.Orbit);
                Assert.Equal(actual.Orbit, first.Orbit);
                var start = snapshot.Objects.Single(o => o.ObjectId == "SPC-0002");
                var originalStart = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == start.ObjectId);
                Assert.Equal(original.PositionX - originalStart.PositionX, first.X - start.X, 5);
                Assert.Equal(original.PositionY - originalStart.PositionY, first.Y - start.Y, 5);
            }
            else if (original.ObjectType == SpaceObjectType.Station) Assert.NotNull(actual.Orbit);
            else Assert.Equal(original.SpeedMps, actual.SpeedMps);
            Assert.Equal((original.Modules ?? []).Where(m => !m.ModuleTypeId.StartsWith("module.torpedo.", StringComparison.Ordinal) &&
                !m.ModuleTypeId.StartsWith("module.countermeasure.", StringComparison.Ordinal) && m.ModuleTypeId != "module.combat.laser")
                .Select(m => (m.ModuleId, m.ModuleTypeId)),
                (actual.Modules ?? []).Select(m => (m.ModuleId, m.ModuleTypeId)));
            foreach (var item in original.Inventory ?? [])
                Assert.Contains(actual.Inventory!, i => i.ItemTypeId == item.ItemTypeId && i.Quantity == item.Quantity);
        }
    }
}
