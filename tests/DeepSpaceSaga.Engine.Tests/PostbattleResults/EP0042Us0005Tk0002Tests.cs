using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Tests.Demo;

namespace DeepSpaceSaga.Engine.Tests.PostbattleResults;

public sealed class EP0042Us0005Tk0002Tests
{
    [Fact]
    public void BattleResultsExit_LeftoversAndSharedCabinCount()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var initial = engine.CaptureSnapshot();
        var loot = initial.DemoCampaign!.Rewards!.Loot!;
        int cabins = initial.CarrierOperations!.CabinReservation!.FreeCabins;
        Assert.Equal(cabins, loot.FreePrisonerCabins);
        Assert.True(cabins > 0);
        Assert.True(engine.ExecuteDemoCampaign(new OpenBattleResultsCommand("open", Revision(), loot.BattleId)).Applied);
        Assert.Equal(cabins, engine.CaptureSnapshot().DemoCampaign!.BattleResults!.Capacity!.FreePrisonerCabins);
        var source = loot.Objects.First(source => source.Pilot is { Alive: true, CaptureReason: null });
        var arrest = new ArrestPilotCommand("arrest", Revision(), loot.BattleId, source.ObjectId);
        var response = engine.ExecuteDemoCampaign(arrest);
        Assert.True(response.Applied);
        Assert.Equal(cabins - 1, response.Rewards!.Loot!.FreePrisonerCabins);
        Assert.Equal(response, engine.ExecuteDemoCampaign(arrest));
        Assert.Equal(cabins - 1, engine.CaptureSnapshot().CarrierOperations!.CabinReservation!.FreeCabins);
        Assert.Equal(BattleLootReasons.ConfirmDiscardRequired, engine.ExecuteDemoCampaign(
            new AcknowledgeBattleResultsCommand("warn", Revision(), loot.BattleId)).Reason);
        Assert.Equal(DemoCampaignReasons.BattleActive, engine.ExecuteDemoCampaign(
            new JettisonCargoCommand("blocked", Revision(), loot.BattleId, "item.scrap", 1)).Reason);
        Assert.True(engine.ExecuteDemoCampaign(new AcknowledgeBattleResultsCommand("leave", Revision(), loot.BattleId, true)).Applied);
        Assert.False(engine.ExecuteDemoCampaign(arrest with { CommandId = "after", ExpectedRevision = Revision() }).Applied);
        Assert.Equal(LootPhases.Closed, engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Phase);

        long Revision() => engine.CaptureSnapshot().DemoCampaign!.Revision;
    }
}
