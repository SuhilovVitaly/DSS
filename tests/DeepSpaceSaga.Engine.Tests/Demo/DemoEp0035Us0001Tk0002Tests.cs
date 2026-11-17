using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0001Tk0002Tests
{
    [Fact]
    public void EncounterContent_LoadsWithRealCatalogAndFixedGeometry()
    {
        var root = DemoEp0033Us0001Tk0003Tests.Root();
        var definitions = DemoCampaignDefinitions.Load(root);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(root + "/Settings.json", out _, out _);
        definitions.ValidateReferences(registry);
        var encounters = definitions.Encounters!;
        Assert.Equal(4, encounters.GroupCount);
        Assert.Equal((45, 45, 10), (encounters.LairWeight, encounters.TancredWeight, encounters.SquadWeight));
        Assert.Equal(200, encounters.Pursuit.CaptureRadius);
        Assert.Equal(8000, encounters.Pursuit.StartSafeRadius);
        Assert.Equal(4000, encounters.Pursuit.SpawnStationRadius);
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        Assert.Equal(2640, engine.CaptureSnapshot().DemoCampaign!.Sensor!.RadiusWorldUnits);
    }
}
