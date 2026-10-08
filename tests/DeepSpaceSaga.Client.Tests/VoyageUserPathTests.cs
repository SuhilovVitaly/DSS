using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Station;
using DeepSpaceSaga.Client.UI.Screens.Finance;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class VoyageUserPathTests
{
    private static readonly StationClusterMapSnapshot Map = new(1, "home",
        [new("home", "Home", "belt", ["A", "B"], "balanced"), new("remote", "Remote", "belt", ["C"], "industrial")],
        [new("A", "home", "market.transit"), new("B", "home", "market.mining"), new("C", "remote", "market.industrial")],
        [new("AB", "A", "B", []), new("AC", "A", "C", [])]);

    [Theory]
    [InlineData("B", "Home")]
    [InlineData("C", "Remote")]
    public async Task LocalAndRemoteVoyageUiPath(string destination, string cluster)
    {
        await using var fixture = new VoyageUiFixture();
        void Publish(string? current, string target, ulong sequence)
        {
            var s = fixture.At(current, (long)sequence * 1000, sequence);
            s = s with
            {
                ClusterMap = Map,
                SelectedObjectId = target,
                TradingRoutes = current is null ? [] : [new(current, target, "Long", 86400000, 86400000, 1000, 1000, "risk.safe", TradingRouteRisk.Safe, TradingRouteAvailability.Available)],
                Voyage = new(current is null ? VoyagePhases.InTransit : VoyagePhases.Docked, DestinationStationObjectId: current is null ? target : null,
                    RouteOptions: current is null ? [] : [new(target, target, 86400000, "Long")])
            };
            fixture.Wire.CurrentSnapshot = s; fixture.Buffer.Update(s);
        }
        Publish("A", destination, 2);
        var station = fixture.Station(); VoyageUiFixture.Render(station);
        Assert.Equal(destination, station.SelectedDestinationId);
        Assert.Contains(cluster, Assert.Single(station.RouteRows).PrimaryText);
        var undock = StationLayout.UndockButtonLocalRect();
        Assert.Equal(ScreenEvent.Undock, station.OnMouseDown(StationLayout.PanelLeft(1920) + (undock.Left + undock.Right) / 2, StationLayout.PanelTop(1080) + (undock.Top + undock.Bottom) / 2));
        Assert.NotNull(fixture.Handle.SendUndockCommand(station.SelectedDestinationId));
        Publish(null, destination, 3); VoyageUiFixture.Render(station);
        var tradeClick = VoyageUiFixture.TradeClick();
        Assert.False(station.HasValidVisit); Assert.Equal(ScreenEvent.None, station.OnMouseDown(tradeClick.X, tradeClick.Y));
        Publish(destination, "A", 4);
        var visit = fixture.Station(); VoyageUiFixture.Render(visit);
        Assert.Equal(ScreenEvent.OpenTrade, visit.OnMouseDown(tradeClick.X, tradeClick.Y));
        var trade = fixture.Trade(); trade.Model.Select("item.water"); VoyageUiFixture.Render(trade);
        Assert.True(trade.HasValidVisit); Assert.True(trade.CanConfirm);
        trade.OnMouseDown(160 + TradeLayout.Confirm.MidX, 140 + TradeLayout.Confirm.MidY);
        Assert.Equal(destination, fixture.Wire.CurrentSnapshot!.DockedStationTrade!.StationObjectId);
        Assert.True(fixture.Wire.Quotes.Last().Quantity > 0);
        Assert.Equal("item.water", fixture.Wire.Commands.Last(c => c.CommandType == TradeCommandTypes.Buy).ItemTypeId);
        Assert.Equal("A", visit.SelectedDestinationId);
        Assert.NotNull(fixture.Handle.SendUndockCommand(visit.SelectedDestinationId));
        Publish(null, "A", 5); VoyageUiFixture.Render(trade); Assert.False(trade.HasValidVisit);
        Publish("A", destination, 6); Assert.True(fixture.Station().HasValidVisit);
        Assert.Equal(new[] { destination, "A" }, fixture.Wire.Commands.Where(c => c.CommandType == NavigationComputerCommandTypes.Undock).Select(c => c.TargetObjectId));
    }

    [Theory]
    [InlineData(-123L)]
    [InlineData(0L)]
    [InlineData(123L)]
    public void FinanceValuesComeFromSnapshot(long net)
    {
        var report = FinanceScreenTests.Report("cluster-leg", net) with { OriginStationObjectId = "A", DestinationStationObjectId = "C" };
        var buffer = new SnapshotBuffer(); buffer.Update(new(1, 0, SimulationSpeed.Speed0, [], ClusterMap: Map, VoyageFinances: [report]));
        var screen = new FinanceScreen(buffer); VoyageUiFixture.Render(screen);
        Assert.Same(report, screen.SelectedReport);
        Assert.Equal(FinanceScreen.MoneyText(net), screen.VoyageRows.Single(r => r.Label == Localization.Get("Finance.NetProfit")).Value);
        Assert.Contains("C", FinanceScreen.RouteText(report));
    }
}
