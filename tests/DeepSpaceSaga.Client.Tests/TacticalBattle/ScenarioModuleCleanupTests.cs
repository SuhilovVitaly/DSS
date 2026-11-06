using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class ScenarioModuleCleanupTests
{
    private static readonly string Source = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryNewGameEntryLoadsWithoutRealtimeCombat(bool deployed)
    {
        string root = deployed ? AppContext.BaseDirectory : Source;
        string scenarios = Path.Combine(root, "Scenarios");
        string[] availableFiles = Directory.GetFiles(scenarios, "scenario.json", SearchOption.AllDirectories);
        var entries = ScenarioRepository.ListScenarios(scenarios);
        Assert.Equal(availableFiles.Order(StringComparer.Ordinal), entries.Select(e => e.ScenarioPath).Order(StringComparer.Ordinal));
        Assert.Equal(["default", "default_500", "docked", "market-profiles", "player_ship_only", "undocked"],
            entries.Select(e => e.ScenarioId));
        foreach (var entry in entries)
        {
            var authored = ScenarioLoader.LoadFromFile(entry.ScenarioPath);
            Assert.All(authored.GameState.SpaceObjects, o =>
            {
                Assert.Null(o.Ai);
                Assert.NotEqual(PlayerRelation.Enemy, o.RelationToPlayer);
                Assert.DoesNotContain(o.Modules ?? [], m => IsCombatModule(m.ModuleTypeId));
            });
            using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(root, "Settings.json"), entry.ScenarioPath);
            AssertWorld(engine.CaptureSnapshot());
        }
    }

    [Fact]
    public async Task DefaultNewGameHasWorldControlsWithoutLegacyWeapons()
    {
        await using var connection = LocalGameSessionConnection.CreateFromSettingsFile(Path.Combine(Source, "Settings.json"));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await snapshots.MoveNextAsync());
        AssertWorld(snapshots.Current);
        Assert.Contains(snapshots.Current.InstalledModules, m => m.ModuleTypeId.StartsWith("module.scanner.", StringComparison.Ordinal));
        Assert.Contains(snapshots.Current.InstalledModules, m => m.CommandTypeIds.Contains(ShipEngineCommandTypes.TurnHold));
    }

    private static void AssertWorld(AuthoritativeSnapshot snapshot)
    {
        Assert.NotNull(snapshot.PlayerShipObjectId);
        Assert.NotEmpty(snapshot.InstalledModules);
        Assert.DoesNotContain(snapshot.InstalledModules, m => IsCombatModule(m.ModuleTypeId));
        Assert.DoesNotContain(snapshot.Objects, o => o.ObjectType is "Missile" or "Countermeasure" or "Wreck");
    }

    private static bool IsCombatModule(string id) =>
        id.StartsWith("module.torpedo.", StringComparison.Ordinal) ||
        id.StartsWith("module.countermeasure.", StringComparison.Ordinal) ||
        id.StartsWith("module.combat.", StringComparison.Ordinal);
}
