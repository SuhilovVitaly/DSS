using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TradingContinuationReviewTests
{
    private static void Refuse(TradingVoyageFixture f, ScenarioFile bad)
    {
        string before = ScenarioLoader.Serialize(f.Save());
        var parsed = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(bad), true);
        Assert.Contains("Save was not modified", Assert.Throws<ScenarioException>(() => f.Engine.LoadScenario(parsed, true)).Message);
        Assert.Equal(before, ScenarioLoader.Serialize(f.Save()));
    }
    [Theory]
    [InlineData("ledger")]
    [InlineData("posting")]
    [InlineData("cargo")]
    [InlineData("settlement")]
    public void Null_entry_in_continuation_uses_atomic_save_diagnostic(string part)
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.FlyTo(f.Destination);
        var save = f.Save(); var state = save.GameState; var ledger = state.VoyageLedgers![0];
        var bad = part switch
        {
            "ledger" => state with { VoyageLedgers = [null!] },
            "posting" => state with { VoyageLedgers = [ledger with { Postings = [null!] }] },
            "cargo" => state with
            {
                VoyageLedgers = [ledger with { Finance = ledger.Finance with
                { UnsoldCargo = ImmutableArray.CreateRange(new VoyageCargoRemainderSnapshot[] { null! }) } }]
            },
            "settlement" => state with { VoyageFuelSettlements = [null!] },
            _ => throw new InvalidOperationException()
        };
        Refuse(f, save with { GameState = bad });
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unrealized_carried_quantity_cannot_exceed_actual_ship_cargo(bool arrived)
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem, 3);
        if (arrived) f.FlyTo(f.Destination);
        else f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: f.Destination);
        var save = f.Save(); var ledger = save.GameState.VoyageLedgers![0];
        var bad = ledger with
        {
            Finance = ledger.Finance with
            {
                UnsoldCargo = ledger.Finance.UnsoldCargo.Select(c => c.ItemTypeId == f.OutboundItem
            ? c with { Quantity = long.MaxValue } : c).ToImmutableArray()
            }
        };
        Refuse(f, save with { GameState = save.GameState with { VoyageLedgers = [bad] } });
    }
    [Fact]
    public void Awaiting_realization_is_bound_to_actual_docked_destination()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1); f.FlyTo(f.Destination);
        var save = f.Save(); var ledger = save.GameState.VoyageLedgers![0];
        string other = save.GameState.SpaceObjects.First(o => o.ObjectType == SpaceObjectType.Station && o.ObjectId != f.Origin && o.ObjectId != f.Destination).ObjectId;
        Refuse(f, save with
        {
            GameState = save.GameState with
            {
                VoyageLedgers = [ledger with
            { Finance = ledger.Finance with { DestinationStationObjectId = other } }]
            }
        });
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Last_fuel_receipt_cannot_be_missing_or_point_to_an_older_retained_voyage(bool missing)
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1); f.FlyTo(f.Destination);
        var old = f.Snapshot.LastVoyageFuelSettlement;
        f.FlyTo(f.Origin);
        var save = f.Save();
        Refuse(f, save with { GameState = save.GameState with { LastVoyageFuelSettlement = missing ? null : old } });
    }
}
