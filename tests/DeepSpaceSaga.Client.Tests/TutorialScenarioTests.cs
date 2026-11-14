using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>EP-0027-US-0001-TK-0001: hidden Tutorial scenario, its lookup and factory entry.</summary>
public sealed class TutorialScenarioTests
{
    private static string ScenariosDirectory => Path.Combine(AppContext.BaseDirectory, "Scenarios");

    private static string ExpectedTutorialPath => Path.Combine(ScenariosDirectory, "Tutorial", "scenario.json");

    private static string MinimalScenario(string id, string extraMetadata = "") => $$"""
    {
      "scenarioMetadata": { "scenarioId": "{{id}}", "name": "{{id}}", "description": "d"{{extraMetadata}} },
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed0", "playerShipObjectId": "SPC-0001",
        "spaceObjects": [
          { "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary" }
        ]
      }
    }
    """;

    private static string CreateTempScenarios(params (string Folder, string Json)[] files)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-scenarios-hidden-{Guid.NewGuid():N}");
        foreach (var (folder, json) in files)
        {
            Directory.CreateDirectory(Path.Combine(dir, folder));
            File.WriteAllText(Path.Combine(dir, folder, "scenario.json"), json);
        }
        return dir;
    }

    [Fact]
    public void ScenarioRepository_ListScenarios_excludes_hidden_scenarios()
    {
        string dir = CreateTempScenarios(
            ("Visible", MinimalScenario("visible")),
            ("Shown", MinimalScenario("shown", ", \"hidden\": false")),
            ("Secret", MinimalScenario("secret", ", \"hidden\": true")));
        try
        {
            var scenarios = ScenarioRepository.ListScenarios(dir);

            Assert.Equal(["shown", "visible"], scenarios.Select(s => s.ScenarioId).ToArray());
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ScenarioRepository_FindTutorialScenarioPath_returns_hidden_tutorial_file()
    {
        string dir = CreateTempScenarios(
            ("Default", MinimalScenario("default")),
            ("Tutorial", MinimalScenario(ScenarioRepository.TutorialScenarioId, ", \"hidden\": true")));
        try
        {
            Assert.Equal(Path.Combine(dir, "Tutorial", "scenario.json"), ScenarioRepository.FindTutorialScenarioPath(dir));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ScenarioRepository_FindTutorialScenarioPath_returns_null_when_absent()
    {
        string dir = CreateTempScenarios(("Default", MinimalScenario("default")));
        try
        {
            Assert.Null(ScenarioRepository.FindTutorialScenarioPath(dir));
            Assert.Null(ScenarioRepository.FindTutorialScenarioPath(Path.Combine(dir, "missing")));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Shipped_tutorial_is_hidden_and_absent_from_scenario_picker()
    {
        Assert.Equal(ExpectedTutorialPath, ScenarioRepository.FindTutorialScenarioPath(ScenariosDirectory));
        var metadata = ScenarioLoader.LoadFromFile(ExpectedTutorialPath).Metadata;
        Assert.Equal("tutorial", metadata.ScenarioId);
        Assert.True(metadata.Hidden);

        var listed = ScenarioRepository.ListScenarios(ScenariosDirectory);
        Assert.DoesNotContain(listed, s => s.ScenarioId == ScenarioRepository.TutorialScenarioId);
        Assert.Equal(["default", "default_500", "docked", "market-profiles", "player_ship_only", "undocked"],
            listed.Select(s => s.ScenarioId).ToArray());
    }

    [Fact]
    public void Shipped_tutorial_world_matches_default_scenario()
    {
        var tutorial = ScenarioLoader.LoadFromFile(ExpectedTutorialPath);
        var source = ScenarioLoader.LoadFromFile(Path.Combine(ScenariosDirectory, "Default", "scenario.json"));

        Assert.Equal(ScenarioLoader.Serialize(source with { Metadata = tutorial.Metadata }), ScenarioLoader.Serialize(tutorial));
    }

    [Fact]
    public async Task Tutorial_starts_in_generated_solar_system()
    {
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        Assert.Contains("Tutorial", config.EnabledScenarios);

        await using var connection = LocalGameSessionConnection.CreateFromScenarioFile(DefaultSystemContentTests.Settings, ExpectedTutorialPath);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await snapshots.MoveNextAsync());
        var snapshot = snapshots.Current;
        Assert.Single(snapshot.Objects, o => o.ObjectType == SpaceObjectType.Sun);
        var ship = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
        Assert.False(ship.IsDocked);
        Assert.InRange(snapshot.ClusterMap!.Clusters.Single(c => c.Id == snapshot.ClusterMap.StartClusterId).StationIds.Length, 10, 12);
    }

    [Fact]
    public void Real_session_factory_resolves_tutorial_path()
    {
        IGameSessionFactory factory = new Program.LocalGameSessionFactory();

        Assert.Equal(ExpectedTutorialPath, factory.GetTutorialScenarioPath());
        Assert.DoesNotContain(factory.ListScenarios(), s => s.ScenarioId == ScenarioRepository.TutorialScenarioId);
    }

    [Fact]
    public void Session_factory_without_tutorial_support_returns_null_by_default()
    {
        IGameSessionFactory factory = new MinimalFactory();

        Assert.Null(factory.GetTutorialScenarioPath());
    }

    private sealed class MinimalFactory : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string scenarioPath) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public bool HasQuickSave() => false;
        public SaveSlotInfo[] ListSaveSlots() => [];
        public void DeleteSaveSlot(string slotId) { }
    }
}
