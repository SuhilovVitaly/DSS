using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us4Tk1Tests
{
    [Theory]
    [InlineData("patrol")]
    [InlineData("pursuit")]
    [InlineData("last_known")]
    [InlineData("return")]
    public void PrivatePursuitAndPlayerObservationRoundtripIndependently(string phase)
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(DemoEp0035Us0010Tk0002Tests.ContactScenario(engine), true);
        var snapshot = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", snapshot.Revision,
            snapshot.PendingSensorReport!.BatchId)).Applied);
        var save = engine.CaptureSaveState();
        var campaign = save.GameState.DemoCampaign!;
        var world = campaign.PirateWorld!;
        var group = world.Groups[0] with { Phase = phase, LastObservedPlayer = new(12345, 67890), LastObservedPlayerTimeMs = 0 };
        Assert.NotNull(group.PlayerObservation);
        save = save with
        {
            GameState = save.GameState with
            {
                DemoCampaign = campaign with
                { PirateWorld = world with { Groups = world.Groups.SetItem(0, group) } },
                SpaceObjects = save.GameState.SpaceObjects.Select(s =>
                    s.ObjectId == save.GameState.PlayerShipObjectId ? s with { PositionX = 0, PositionY = 0, SpeedMps = 0 } : s).ToArray()
            }
        };
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        var restored = engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld!.Groups[0];
        Assert.Equal(JsonSerializer.Serialize(group), JsonSerializer.Serialize(restored));
        Assert.Null(engine.CaptureSnapshot().DemoCampaign!.PendingSensorReport);
        var observed = engine.CaptureSnapshot().DemoCampaign!.Encounters!.Groups.Single(g => g.Id == group.GroupId);
        Assert.Equal(PirateGroupStatuses.LastKnown, observed.Status);
        Assert.Equal(group.PlayerObservation!.X, observed.X);
        Assert.NotEqual(group.LastObservedPlayer!.X, observed.X);
    }

    [Fact]
    public void FutureObservationFailsAtomically()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var campaign = save.GameState.DemoCampaign!;
        var world = campaign.PirateWorld!;
        var bad = world.Groups[0] with { LastObservedPlayer = new(1, 2), LastObservedPlayerTimeMs = world.LastMotionTimeMs + 1 };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
        {
            GameState = save.GameState with
            { DemoCampaign = campaign with { PirateWorld = world with { Groups = world.Groups.SetItem(0, bad) } } }
        }, true));
        Assert.Equal(JsonSerializer.Serialize(world), JsonSerializer.Serialize(engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld));
    }
}
