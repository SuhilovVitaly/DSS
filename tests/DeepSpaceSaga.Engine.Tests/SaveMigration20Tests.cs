using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SaveMigration20Tests
{
    private static readonly string[] Obsolete = ["combatState", "defenseState", "hostileCombatState", "combatReportsState",
        "combatReactionDelays", "pointCombatState"];

    [Fact]
    public void CurrentWriterEmitsOnlyWorldStateAndTheResultRoundTrips()
    {
        using var engine = Create();
        engine.LoadScenario(ScenarioLoader.LoadFromFile(Path.Combine(SaveFixturesTests.ClientRoot, "Scenarios/Default/scenario.json")));
        var save = engine.CaptureSaveState();
        string json = ScenarioLoader.Serialize(save);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(20, document.RootElement.GetProperty("saveFormatVersion").GetInt32());
        var world = document.RootElement.GetProperty("gameState");
        Assert.All(Obsolete, field => Assert.False(world.TryGetProperty(field, out _), field));
        AssertWorld(save);
        using var restored = Create();
        restored.LoadScenario(ScenarioLoader.LoadFromJson(json, allowNonZeroGameTime: true, isSave: true), isSave: true);
        Assert.Equal(json, ScenarioLoader.Serialize(restored.CaptureSaveState()));
    }

    [Theory]
    [InlineData("Default-Save18-combat.json")]
    [InlineData("Default-Save19-combat.json")]
    public void MigrationRemovesCombatAndKeepsWorldClocksSeedAndCatalog(string name)
    {
        string json = File.ReadAllText(Path.Combine(SaveFixturesTests.Fixtures, name));
        using var before = JsonDocument.Parse(json);
        var source = before.RootElement.GetProperty("gameState");
        var migrated = ScenarioLoader.LoadFromJson(json, allowNonZeroGameTime: true, isSave: true);
        AssertWorld(migrated);
        Assert.Equal(source.GetProperty("gameTimeMs").GetInt64(), migrated.GameState.GameTimeMs);
        Assert.Equal(source.GetProperty("simulationTimeMs").GetInt64(), migrated.GameState.MotionTimeMs);
        Assert.Equal(source.GetProperty("masterSeed").GetUInt64(), migrated.GameState.MasterSeed);
        Assert.Equal(source.GetProperty("playerTokens").GetInt64(), migrated.GameState.PlayerTokens);
        Assert.Equal(source.GetProperty("catalogCompatibility").GetProperty("fingerprint").GetString(),
            migrated.GameState.CatalogCompatibility!.Fingerprint);
        Assert.DoesNotContain(migrated.GameState.CommandReceipts ?? [], r => r.CommandId is "fixture-torpedo" or "fixture-defense");
        Assert.Contains(migrated.GameState.SpaceObjects, o => o.ObjectType == SpaceObjectType.Station);
    }

    [Fact]
    public void OldSchemaViolationsAndDamagedLegacyCombatAreRejectedBeforeDiscard()
    {
        var save18 = Fixture("Default-Save18-combat.json");
        save18["gameState"]!["debugTuning"] = new JsonArray();
        Assert.Contains("requires Save19", Assert.Throws<ScenarioException>(() => Load(save18)).Message);

        var corrupt = Fixture("Default-Save19-combat.json");
        corrupt["gameState"]!["pointCombatState"]!["Projectiles"]![0]!["Flight"]!["SpeedKmS"] = -1;
        Assert.Throws<ScenarioException>(() => Load(corrupt));
        corrupt = Fixture("Default-Save19-combat.json");
        corrupt["saveFormatVersion"] = 20;
        Assert.Contains("obsolete real-time combat", Assert.Throws<ScenarioException>(() => Load(corrupt)).Message);

        var unsupported = Fixture("Default-Save19.json");
        unsupported["saveFormatVersion"] = 17;
        Assert.Contains("incompatible_save_version", Assert.Throws<ScenarioException>(() => Load(unsupported)).Message);
    }

    [Fact]
    public void MismatchedLegacyCatalogCannotReplaceAnExistingWorld()
    {
        using var engine = Create();
        engine.LoadScenario(ScenarioLoader.LoadFromFile(Path.Combine(SaveFixturesTests.ClientRoot, "Scenarios/Default/scenario.json")));
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var invalid = Fixture("Default-Save19-combat.json");
        invalid["gameState"]!["catalogCompatibility"]!["fingerprint"] = new string('A', 64);
        var migrated = Load(invalid);
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(migrated, isSave: true));
        Assert.Contains("catalog fingerprint", error.Message);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    private static void AssertWorld(ScenarioFile save)
    {
        Assert.Equal(20, save.SaveFormatVersion);
        Assert.DoesNotContain(save.GameState.SpaceObjects, o => o.ObjectType is "Missile" or "Countermeasure" or "Wreck" || o.Ai is not null);
        var player = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.PlayerShipObjectId);
        Assert.Contains(player.Modules!, m => m.ModuleTypeId.Contains("scanner", StringComparison.Ordinal));
        Assert.Contains(player.Modules!, m => m.ModuleTypeId.Contains("engine", StringComparison.Ordinal));
        Assert.DoesNotContain(player.Modules!, m => m.ModuleTypeId.Contains("torpedo", StringComparison.Ordinal) ||
            m.ModuleTypeId.Contains("countermeasure", StringComparison.Ordinal) || m.ModuleTypeId == "module.combat.laser");
    }

    private static JsonNode Fixture(string name) => JsonNode.Parse(File.ReadAllText(Path.Combine(SaveFixturesTests.Fixtures, name)))!;
    private static ScenarioFile Load(JsonNode json) => ScenarioLoader.LoadFromJson(json.ToJsonString(), allowNonZeroGameTime: true, isSave: true);
    private static SimulationEngine Create() => new(SaveFixturesTests.Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
}
