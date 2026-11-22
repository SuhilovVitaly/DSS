using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0001-TK-0002: the demo New Game references its own derived starter manifest with the original
/// crew/wing identities plus an engineer (7 crew, 1 free cabin, no reserve).
/// Shared starter content and other scenarios are unchanged. Reads the shipped content copied to the test
/// output and loads it through the production loader/engine.
/// </summary>
public sealed class DemoEp0034Us0001Tk0002Tests
{
    private static string BaseDir => AppContext.BaseDirectory;
    private static string SettingsPath => Path.Combine(BaseDir, "Settings.json");
    private static string DemoScenarioPath => Path.Combine(BaseDir, "Scenarios", "Demo", "scenario.json");
    private static string DemoManifestPath => Path.Combine(BaseDir, "Data", "Ships", "demo-starter-carrier.json");
    private static string SharedManifestPath => Path.Combine(BaseDir, "Data", "Ships", "starter-carrier.json");

    [Fact]
    public void Content_LoadsAndReferencesResolve()
    {
        using var scenario = JsonDocument.Parse(File.ReadAllText(DemoScenarioPath));
        Assert.Equal("Data/Ships/demo-starter-carrier.json",
            scenario.RootElement.GetProperty("scenarioMetadata").GetProperty("starterCarrierManifest").GetString());

        var demo = StarterCarrierManifest.Load(DemoManifestPath);
        var shared = StarterCarrierManifest.Load(SharedManifestPath);
        Assert.Equal(shared.ShipClassId, demo.ShipClassId);
        Assert.Equal(shared.HullLayout!.Cells.Select(c => (c.X, c.Y)), demo.HullLayout!.Cells.Select(c => (c.X, c.Y)));
        // Original identities remain; EP36 adds exactly the dedicated demo engineer.
        Assert.Equal(shared.Crew!.Select(c => c.CrewId), demo.Crew!.Where(c => c.CrewId != "CHR-PLAYER-ENGINEER").Select(c => c.CrewId));
        Assert.Single(demo.Crew!, c => c.CrewId == "CHR-PLAYER-ENGINEER");
        Assert.Equal(shared.CarrierState!.OwnedDrones!.Select(d => (d.DroneId, d.CrewId)),
            demo.CarrierState!.OwnedDrones!.Select(d => (d.DroneId, d.CrewId)));
        // Legacy manifest field remains accepted; runtime ignores this old empty-cabin reserve.
        Assert.Equal(shared.CarrierState.ReservedCabins, demo.CarrierState.ReservedCabins);

        Assert.Equal(shared.Modules!.Count, demo.Modules!.Count);
        Assert.Equal(2, demo.Modules.Count(module => module.ModuleTypeId == "living.quarters.mk1.four-cabins"));
        foreach (var original in shared.Modules)
        {
            var copy = demo.Modules.Single(m => m.ModuleId == original.ModuleId);
            Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(copy));
        }
    }

    [Fact]
    public void DemoLimits_AreSatisfied()
    {
        // The production engine validates hull cells, overlaps and module footprints on load.
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var snapshot = engine.CaptureSnapshot();
        var cabins = snapshot.CarrierOperations!.CabinReservation!;
        Assert.Equal(8, cabins.TotalCabins);
        Assert.Equal(7, cabins.OccupiedCabins);
        Assert.Equal(0, cabins.ReservedCabins);
        Assert.Equal(1, cabins.FreeCabins);
        Assert.Equal(7, snapshot.CarrierOperations.Crew.Length);

        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
        var living = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("living.quarters.mk1.four-cabins"));
        // Mass is bounded by the catalog module (no free weightless cabins) and adds no power draw.
        Assert.Equal(2000, living.MassKg);
        Assert.Equal(0, living.PowerConsumptionW);

        // Life support: the hold feeds the full complement (crew + passengers/prisoners) for at least one day.

        long rations = snapshot.InstalledModules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeId == "item.food-rations").Sum(c => c.Quantity);
        Assert.True(rations >= 2L * cabins.TotalCabins, $"rations {rations} cannot feed {cabins.TotalCabins} people for a day");
    }

    [Fact]
    public void LegacyScenario_DoesNotReceiveDemoGrant()
    {
        // Shared starter keeps exactly two living quarters (8 cabins = 6 crew + 2 free; no demo engineer).
        var shared = StarterCarrierManifest.Load(SharedManifestPath);
        Assert.DoesNotContain(shared.Modules!, m => m.ModuleId == "MOD-PLAYER-LIVING-QUARTERS-03");

        foreach (var folder in new[] { "Tutorial", "Default" })
        {
            string path = Path.Combine(BaseDir, "Scenarios", folder, "scenario.json");
            using var scenario = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal("Data/Ships/starter-carrier.json",
                scenario.RootElement.GetProperty("scenarioMetadata").GetProperty("starterCarrierManifest").GetString());
            using var engine = SimulationEngine.CreateFromScenarioFile(SettingsPath, path);
            engine.SetSpeed(SimulationSpeed.Speed0);
            var cabins = engine.CaptureSnapshot().CarrierOperations!.CabinReservation!;
            Assert.Equal(8, cabins.TotalCabins);
            Assert.Equal(2, cabins.FreeCabins);
            Assert.DoesNotContain(engine.CaptureSnapshot().CarrierOperations!.Crew, c => c.Role == CrewRole.Engineer);
        }
    }
}
