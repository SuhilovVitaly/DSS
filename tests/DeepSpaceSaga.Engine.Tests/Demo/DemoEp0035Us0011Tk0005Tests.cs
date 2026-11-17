using System.Text.Json;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0011Tk0005Tests
{
    [Fact]
    public void FirstSensorContact_MaterializesOnce_AndLoadDoesNotReroll()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var group = save.GameState.DemoCampaign!.PirateWorld!.Groups[0];
        Assert.Empty(save.GameState.DemoCampaign.PirateCompositions);
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
                { PositionX = group.X + 2200, PositionY = group.Y, SpeedMps = 0, Orbit = null, IsDocked = false, DockedStationObjectId = null }).ToArray()
            }
        }, true);
        engine.CaptureSnapshot();
        var materialized = engine.CaptureSaveState();
        var members = materialized.GameState.DemoCampaign!.PirateCompositions.Where(c => c.GroupId == group.GroupId).ToArray();
        Assert.Equal(group.Members.Length, members.Length);
        Assert.All(members, m => Assert.Equal(m.SignatureTypeId == "lair" ? 6 : 10, m.Drones.Length));
        string before = JsonSerializer.Serialize(members);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(materialized), true, true), true);
        engine.CaptureSnapshot();
        Assert.Equal(before, JsonSerializer.Serialize(engine.CaptureSaveState().GameState.DemoCampaign!.PirateCompositions.Where(c => c.GroupId == group.GroupId).ToArray()));
        Assert.DoesNotContain("LayoutId", JsonSerializer.Serialize(engine.CaptureSnapshot().DemoCampaign!.Encounters));
    }

    [Fact]
    public void NamedStreams_AllowUnforcedMixAndKeepStableIdentity()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var group = engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld!.Groups[0];
        group = group with { Members = [group.Members[0] with { SignatureTypeId = "lair" }] };
        string first = JsonSerializer.Serialize(SimulationEngine.GeneratePirateComposition(123, group));
        Assert.Equal(first, JsonSerializer.Serialize(SimulationEngine.GeneratePirateComposition(123, group)));
        var classes = Enumerable.Range(0, 512).Select(seed => SimulationEngine.GeneratePirateComposition((ulong)seed, group)[0].Drones
            .Select(d => d.LayoutId.StartsWith("heavy", StringComparison.Ordinal)).ToArray()).ToArray();
        Assert.Contains(classes, row => row.All(heavy => heavy));
        Assert.Contains(classes, row => row.All(heavy => !heavy));
        Assert.Contains(classes, row => row.Any(heavy => heavy) && row.Any(heavy => !heavy));
    }
}
