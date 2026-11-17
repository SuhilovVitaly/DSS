using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

[Collection("Demo process debug")]
public sealed class DemoEp0035Us0008Tk0002Tests
{
    [Fact]
    public void Reveal_DoesNotAcquireOrGenerate_AndStaleCommandDoesNotChangeFog()
    {
        bool original = SimulationEngine.ConfigureFogOfWar().FogOfWar;
        try
        {
            SimulationEngine.ConfigureFogOfWar(true);
            using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
            var before = engine.CaptureSnapshot().DemoCampaign!;
            Assert.Empty(before.Encounters!.Groups);
            Assert.True(engine.ExecuteDemoCampaign(new SetFogOfWarRequest("reveal", before.Revision, false)).Applied);
            var revealed = engine.CaptureSnapshot().DemoCampaign!;
            Assert.False(revealed.FogOfWar!.Enabled);
            Assert.Equal(4, revealed.Encounters!.Groups.Length);
            Assert.All(revealed.Encounters.Groups, g => { Assert.True(g.IsDebugVisible); Assert.False(g.IsDetected); Assert.Empty(g.Signatures); });
            var save = engine.CaptureSaveState();
            Assert.Empty(save.GameState.DemoCampaign!.PirateCompositions);
            Assert.All(save.GameState.DemoCampaign.PirateWorld!.Groups, g => Assert.False(g.CompositionMaterialized));
            Assert.Equal(DemoCampaignReasons.StaleRevision, engine.ExecuteDemoCampaign(new SetFogOfWarRequest("stale", before.Revision, true)).Reason);
            Assert.False(SimulationEngine.ConfigureFogOfWar().FogOfWar);
            string json = ScenarioLoader.Serialize(save);
            Assert.DoesNotContain("fogOfWar", json, StringComparison.OrdinalIgnoreCase);
            engine.LoadScenario(ScenarioLoader.LoadFromJson(json, true, true), true);
            Assert.False(engine.CaptureSnapshot().DemoCampaign!.FogOfWar!.Enabled);
            Assert.True(engine.ExecuteDemoCampaign(new SetFogOfWarRequest("hide", engine.CaptureSnapshot().DemoCampaign!.Revision, true)).Applied);
            Assert.Empty(engine.CaptureSnapshot().DemoCampaign!.Encounters!.Groups);
        }
        finally { SimulationEngine.ConfigureFogOfWar(original); }
    }
}
