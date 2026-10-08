using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class AllScenarioSystemContentTests
{
    [Fact]
    public async Task EveryListedScenarioStarts()
    {
        var scenarios = ScenarioRepository.ListScenarios(Path.Combine(AppContext.BaseDirectory, "Scenarios"));
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        Assert.Equal(scenarios.Select(s => Path.GetFileName(Path.GetDirectoryName(s.ScenarioPath))).Order(), config.EnabledScenarios.Order());
        foreach (var scenario in scenarios)
        {
            await using var connection = LocalGameSessionConnection.CreateFromScenarioFile(DefaultSystemContentTests.Settings, scenario.ScenarioPath);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
            Assert.True(await snapshots.MoveNextAsync());
            var snapshot = snapshots.Current;
            Assert.Single(snapshot.Objects, o => o.ObjectType == SpaceObjectType.Sun);
            var ship = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
            Assert.InRange(Math.Sqrt(ship.X * ship.X + ship.Y * ship.Y) / 10 / ship.MaxSpeedKmS!.Value * 300 / 86400, 50, 75);
            string name = Path.GetFileName(Path.GetDirectoryName(scenario.ScenarioPath))!;
            if (name == "Default_500") Assert.Equal(500, snapshot.Objects.Count(o => o.ObjectType == "Asteroid" && o.Orbit is null));
            Assert.InRange(snapshot.ClusterMap!.Clusters.Single(c => c.Id == snapshot.ClusterMap.StartClusterId).StationIds.Length, 10, 12);
            Assert.Equal(name == "Docked", ship.IsDocked);
        }
    }

    [Fact]
    public async Task SavedLegacySessionDoesNotGenerate()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DefaultSystemContentTests.Settings, out _, out _);
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromFile(path));
        var save = engine.CaptureSaveState();
        Assert.Null(save.GameState.SolarSystem);
        string folder = Path.Combine(Path.GetTempPath(), "dss-legacy-system-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            string file = Path.Combine(folder, "save.json");
            File.WriteAllText(file, ScenarioLoader.Serialize(save));
            await using var connection = LocalGameSessionConnection.CreateFromSaveFile(DefaultSystemContentTests.Settings, file);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
            Assert.True(await snapshots.MoveNextAsync());
            Assert.Null(snapshots.Current.SolarSystemMap);
            Assert.Equal(save.GameState.SpaceObjects.Count, snapshots.Current.Objects.Length);
        }
        finally { Directory.Delete(folder, true); }
    }
}
