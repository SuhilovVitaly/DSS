using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0011Tk0002Tests
{
    internal static DemoEncountersDefinition Definition() => new(2, 4, new(0, 0, 50000), new(200, 8000, 4000, 3000),
        [new("lair", 6, 1.1, "carrier.standard", "module.sensor.pirate-start"),
         new("tancred", 10, 0.8, "carrier.standard", "module.sensor.pirate-start")],
        ["light.sword.shield", "light.spear", "heavy.sword.shield", "heavy.spear.shield"]);

    [Fact]
    public void SignatureCounts_AreSixAndTen()
    {
        var definition = Definition();
        definition.Validate();
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DemoEp0033Us0001Tk0003Tests.Root() + "/Settings.json", out _, out _);
        definition.ValidateReferences(registry);
        Assert.Equal(new[] { 6, 10 }, definition.Profiles.Select(p => p.DroneCount));
    }

    [Fact]
    public void InvalidProfileAndPool_Rejected()
    {
        var definition = Definition();
        Assert.Throws<ScenarioException>(() => (definition with { Profiles = definition.Profiles.SetItem(0, definition.Profiles[0] with { DroneCount = 7 }) }).Validate());
        Assert.Throws<ScenarioException>(() => (definition with { DroneLayouts = definition.DroneLayouts.SetItem(0, "unknown") }).Validate());
        Assert.Throws<ScenarioException>(() => (definition with { Pursuit = definition.Pursuit with { StartSafeRadius = 7999 } }).Validate());
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DemoEp0033Us0001Tk0003Tests.Root() + "/Settings.json", out _, out _);
        Assert.Throws<ScenarioException>(() => (definition with
        {
            Profiles = definition.Profiles.SetItem(0,
            definition.Profiles[0] with { CarrierLayoutId = "missing" })
        }).ValidateReferences(registry));
    }

    [Fact]
    public void PrivateComposition_RejectsDuplicateIdentityAndWrongCount()
    {
        var drones = Enumerable.Range(0, 6).Select(i => new PirateDroneData("d" + i, "light.sword.shield")).ToImmutableArray();
        var composition = new PirateCompositionData("g", "c", "lair", 1, drones);
        var valid = new DemoCampaignData { PirateCompositions = [composition] };
        DemoCampaignData.Validate(valid, 0);
        Assert.Throws<ScenarioException>(() => DemoCampaignData.Validate(valid with { PirateCompositions = [composition, composition] }, 0));
        Assert.Throws<ScenarioException>(() => DemoCampaignData.Validate(valid with { PirateCompositions = [composition with { Drones = drones.RemoveAt(0) }] }, 0));
    }
}
