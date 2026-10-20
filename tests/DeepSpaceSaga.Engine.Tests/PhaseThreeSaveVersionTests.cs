using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class PhaseThreeSaveVersionTests
{
    public static IEnumerable<object[]> OldVersions => Enumerable.Range(0, 18).Select(v => new object[] { v });

    [Theory]
    [MemberData(nameof(OldVersions))]
    public void Old_save_versions_are_rejected_without_mutation(int version)
    {
        using var engine = Create();
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        string before = ScenarioLoader.Serialize(save);
        var old = save with { SaveFormatVersion = version };
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(old, isSave: true));
        Assert.Contains("incompatible_save_version", error.Message);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)));
        string path = Path.Combine(Path.GetTempPath(), "dss-old-save-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(old));
            byte[] bytes = File.ReadAllBytes(path);
            Assert.Contains("incompatible_save_version", Assert.Throws<ScenarioException>(() =>
                SimulationEngine.CreateFromSaveFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(ClientRoot, "Settings.json")), path)).Message);
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Version_zero_new_game_is_allowed_but_same_file_as_save_is_rejected()
    {
        string path = Path.Combine(ClientRoot, "Scenarios/PlayerShipOnly/scenario.json");
        Assert.Equal(0, ScenarioLoader.LoadFromFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Scenario(path)).SaveFormatVersion);
        using var newGame = SimulationEngine.CreateFromScenarioFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(ClientRoot, "Settings.json")), global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Scenario(path));
        Assert.Equal(SessionOutcomeState.Running, newGame.CaptureSnapshot().EffectiveOutcome.State);
        Assert.Contains("incompatible_save_version", Assert.Throws<ScenarioException>(() =>
            SimulationEngine.CreateFromSaveFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(ClientRoot, "Settings.json")), path)).Message);
        Assert.Equal(18, newGame.CaptureSaveState().SaveFormatVersion);
    }

    [Theory]
    [InlineData("{\"saveFormatVersion\":15,\"gameState\":\"invalid historical body\"}")]
    [InlineData("{\"gameState\":\"invalid legacy body\"}")]
    [InlineData("{\"saveFormatVersion\":\"17\"}")]
    [InlineData("{\"saveFormatVersion\":19}")]
    public void Save_gate_runs_before_historical_payload_deserialization(string json)
    {
        Assert.Contains("incompatible_save_version", Assert.Throws<ScenarioException>(() =>
            ScenarioLoader.LoadFromJson(json, true, isSave: true)).Message);
    }

    [Fact]
    public void Current_version_file_roundtrip_preserves_the_world()
    {
        using var engine = Create();
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var parsed = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, isSave: true);
        using var restored = Create();
        restored.LoadScenario(parsed, isSave: true);
        Assert.Equal(18, parsed.SaveFormatVersion);
        Assert.Equal(JsonSerializer.Serialize(At(engine, 0).Objects), JsonSerializer.Serialize(At(restored, 0).Objects));
    }
}
