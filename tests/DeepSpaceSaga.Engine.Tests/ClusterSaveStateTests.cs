using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ClusterSaveStateTests
{
    internal static void SameFacts(ClusterVoyageFixture a, ClusterVoyageFixture b)
    {
        var left = SimulationEngine.NormalizeTradingContinuationForTests(a.Save().GameState);
        var right = SimulationEngine.NormalizeTradingContinuationForTests(b.Save().GameState);
        Assert.Equal(left.GameTimeMs, right.GameTimeMs); Assert.Equal(left.MotionTimeMs, right.MotionTimeMs);
        Assert.Equal(left.PlayerTokens, right.PlayerTokens);
        Assert.Equal(JsonSerializer.Serialize(left.ClusterMap), JsonSerializer.Serialize(right.ClusterMap));
        Assert.Equal(JsonSerializer.Serialize(left.VoyageState), JsonSerializer.Serialize(right.VoyageState));
        Assert.Equal(JsonSerializer.Serialize(left.VoyageLedgers), JsonSerializer.Serialize(right.VoyageLedgers));
        Assert.Equal(JsonSerializer.Serialize(left.CommandReceipts), JsonSerializer.Serialize(right.CommandReceipts));
        Assert.Equal(JsonSerializer.Serialize(left.TradingEconomyContinuation), JsonSerializer.Serialize(right.TradingEconomyContinuation));
        foreach (var obj in left.SpaceObjects)
        {
            var actual = right.SpaceObjects.Single(o => o.ObjectId == obj.ObjectId);
            Assert.Equal(obj.PositionX, actual.PositionX, 6); Assert.Equal(obj.PositionY, actual.PositionY, 6);
            Assert.Equal(JsonSerializer.Serialize(obj.Inventory), JsonSerializer.Serialize(actual.Inventory));
            Assert.Equal(obj.MarketBudgetCredits, actual.MarketBudgetCredits); Assert.Equal(obj.MarketRevision, actual.MarketRevision);
            Assert.Equal(obj.Credits, actual.Credits); Assert.Equal(obj.PortFeeDebt, actual.PortFeeDebt);
            Assert.Equal(obj.FirstPortFeeGameTimeMs, actual.FirstPortFeeGameTimeMs); Assert.Equal(obj.NextPortFeeDueGameTimeMs, actual.NextPortFeeDueGameTimeMs);
            Assert.Equal(JsonSerializer.Serialize(obj.Events), JsonSerializer.Serialize(actual.Events));
            Assert.Equal(JsonSerializer.Serialize(obj.Modules), JsonSerializer.Serialize(actual.Modules));
        }
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(42UL)]
    public void ClusterJsonRoundTripAndContinuation(ulong seed)
    {
        using var continuous = new ClusterVoyageFixture(seed, true, initialCredits: 1000000);
        continuous.Trade(TradeCommandTypes.Buy, continuous.OutboundItem);
        ClusterVoyageFixture? resumed = null;
        try
        {
            continuous.FlyTo(continuous.Destination, midpoint => { resumed = midpoint.Reload(); SameFacts(midpoint, resumed); });
            resumed!.FinishFlightTo(resumed.Destination); SameFacts(continuous, resumed);
            continuous.Trade(TradeCommandTypes.Sell, continuous.OutboundItem); resumed.Trade(TradeCommandTypes.Sell, resumed.OutboundItem);
            continuous.Advance(100 * 288000L); resumed.Advance(100 * 288000L); SameFacts(continuous, resumed);
            using var docked = resumed.Reload(); SameFacts(resumed, docked);
            continuous.Trade(TradeCommandTypes.Buy, continuous.ReturnItem); docked.Trade(TradeCommandTypes.Buy, docked.ReturnItem);
            continuous.FlyTo(continuous.Origin); docked.FlyTo(docked.Origin);
            continuous.Trade(TradeCommandTypes.Sell, continuous.ReturnItem); docked.Trade(TradeCommandTypes.Sell, docked.ReturnItem);
            SameFacts(continuous, docked); Assert.Equal(continuous.Origin, docked.Player.DockedStationObjectId);
        }
        finally { resumed?.Dispose(); }
    }

    [Fact]
    public void ReloadAfterCompletedLegKeepsActiveFuelTerms()
    {
        using var continuous = new ClusterVoyageFixture(1, true);
        continuous.Trade(TradeCommandTypes.Buy, continuous.OutboundItem);
        continuous.FlyTo(continuous.Destination); continuous.Trade(TradeCommandTypes.Sell, continuous.OutboundItem);
        continuous.Trade(TradeCommandTypes.Buy, continuous.ReturnItem);
        ClusterVoyageFixture? resumed = null;
        try
        {
            continuous.FlyTo(continuous.Origin, midpoint => { resumed = midpoint.Reload(); SameFacts(midpoint, resumed); });
            resumed!.FinishFlightTo(resumed.Origin); SameFacts(continuous, resumed);
        }
        finally { resumed?.Dispose(); }
    }

    [Fact]
    public void NoMarketResetOnLoad()
    {
        using var engine = ClusterResourcePlacementTests.Create();
        engine.CaptureSnapshotForTests(100 * 86400000L, SimulationSpeed.Speed0, 100 * 288000L);
        var saved = engine.CaptureSaveStateForTests(100 * 86400000L, SimulationSpeed.Speed0, 100 * 288000L);
        using var loaded = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true), true);
        Assert.Equal(JsonSerializer.Serialize(engine.CaptureMarketDiagnosticsForTests()), JsonSerializer.Serialize(loaded.CaptureMarketDiagnosticsForTests()));
        Assert.Equal(JsonSerializer.Serialize(saved.GameState.ClusterMap), JsonSerializer.Serialize(loaded.CaptureSaveState().GameState.ClusterMap));
        Assert.Equal(JsonSerializer.Serialize(saved.GameState.StationResourceFields), JsonSerializer.Serialize(loaded.CaptureSaveState().GameState.StationResourceFields));
    }

    [Theory]
    [InlineData("member")]
    [InlineData("duplicate-member")]
    [InlineData("profile")]
    [InlineData("belt")]
    [InlineData("endpoint")]
    [InlineData("graph")]
    [InlineData("field")]
    [InlineData("anchor")]
    [InlineData("offset")]
    [InlineData("null-cluster")]
    [InlineData("null-endpoint")]
    [InlineData("null-start")]
    [InlineData("no-return")]
    public void InvalidMembershipAndFieldReferencesRejected(string corruption)
    {
        using var engine = ClusterResourcePlacementTests.Create();
        var save = engine.CaptureSaveState(); var map = save.GameState.ClusterMap!;
        var first = map.Clusters[0]; var binding = map.ResourceBindings[0];
        var invalid = corruption switch
        {
            "member" => map with { Stations = map.Stations.RemoveAt(0) },
            "duplicate-member" => map with { Stations = map.Stations.Add(map.Stations[0]) },
            "profile" => map with { Stations = map.Stations.SetItem(0, map.Stations[0] with { MarketProfileId = "market.transit" == map.Stations[0].MarketProfileId ? "market.mining" : "market.transit" }) },
            "belt" => map with { Clusters = map.Clusters.SetItem(0, first with { BeltId = "missing" }) },
            "endpoint" => map with { Links = map.Links.SetItem(0, map.Links[0] with { ToStationId = "missing" }) },
            "graph" => map with { Links = map.Links.Where(l => l.FromStationId != first.StationIds[0]).ToImmutableArray() },
            "field" => map with { ResourceBindings = map.ResourceBindings.SetItem(0, binding with { FieldId = "missing" }) },
            "anchor" => map with { ResourceBindings = map.ResourceBindings.SetItem(0, binding with { AnchorStationId = "missing" }) },
            "offset" => map with { ResourceBindings = map.ResourceBindings.SetItem(0, binding with { OffsetX = binding.OffsetX + 1 }) },
            "null-cluster" => map with { Clusters = map.Clusters.SetItem(0, null!) },
            "null-endpoint" => map with { Links = map.Links.SetItem(0, map.Links[0] with { ToStationId = null! }) },
            "no-return" => map with { Links = map.Links.Where(l => !first.StationIds.Contains(l.ToStationId) || first.StationIds.Contains(l.FromStationId)).ToImmutableArray() },
            _ => map with { StartClusterId = null! }
        };
        string before = ScenarioLoader.Serialize(save);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = save.GameState with { ClusterMap = invalid } }, true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }
}
