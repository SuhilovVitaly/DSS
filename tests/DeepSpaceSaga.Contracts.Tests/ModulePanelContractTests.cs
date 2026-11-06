using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class ModulePanelContractTests
{
    [Fact]
    public void World_frames_and_module_rows_do_not_publish_retired_combat_members()
    {
        var module = new InstalledModuleSnapshot("engine", "module.engine.basic", "Engine", 1, [],
            StructureCurrent: 4, StructureMax: 6);
        var frame = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [new("own", 0, 0, 0, 0)],
            PlayerShipObjectId: "own", InstalledModules: [module]);
        string json = JsonSerializer.Serialize(frame);
        foreach (string field in new[] { "HullCombat", "OwnProjectile", "CombatReports", "CombatJournal", "Encounter",
            "Outcome", "LauncherCombat", "Defense", "DefensePreview", "Operator" })
            Assert.DoesNotContain($"\"{field}\"", json);
        var copy = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json)!;
        Assert.Equal("own", copy.PlayerShipObjectId);
        Assert.Equal(4, Assert.Single(copy.InstalledModules).StructureCurrent);
    }
}
