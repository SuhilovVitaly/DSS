using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DefaultSystemContentTests
{
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
        Assert.Equal(4, snapshot.Objects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId).MaxSpeedKmS);
        foreach (var original in scenario.GameState.SpaceObjects)
        {
            var actual = saved.GameState.SpaceObjects.Single(o => o.ObjectId == original.ObjectId);
            Assert.Equal(original.SpeedMps, actual.SpeedMps);
            Assert.Equal((original.Modules ?? []).Select(m => (m.ModuleId, m.ModuleTypeId)),
                (actual.Modules ?? []).Select(m => (m.ModuleId, m.ModuleTypeId)));
            foreach (var item in original.Inventory ?? [])
                Assert.Contains(actual.Inventory!, i => i.ItemTypeId == item.ItemTypeId && i.Quantity == item.Quantity);
        }
    }
}
