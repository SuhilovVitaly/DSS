using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.EconomyBalance;

namespace DeepSpaceSaga.EconomyBalance.Tests;

public sealed class LongVoyageRunnerTests
{
    private static readonly Lazy<ImmutableArray<ClusterCaseEvidence>> Cases = new(() => new ClusterBalanceRunner().Run(
        BalanceRunTests.Settings, BalanceRunTests.Scenario, ClusterMatrixFile.Default with { Seeds = [1], ShipConfigurations = [BalanceRunTests.Starter] }));

    [Fact]
    public void ClusterRunCompletesReturnBeyondHundredDays()
    {
        var c = Assert.Single(Cases.Value);
        Assert.True(c.Outcome == "completed", c.StopReason);
        Assert.Equal(3, c.CompletedLocalCycles); Assert.Equal(1, c.CompletedRoundTrips);
        Assert.InRange(c.HorizonReachedDays, 100, 1000);
        Assert.Equal(c.Economy.Strategies.First().Origin, c.Economy.Strategies.Last().Destination);
        Assert.Equal(c.Economy.ContinuousStateHash, c.Economy.SaveLoadStateHash);
        var capped = Assert.Single(new ClusterBalanceRunner().Run(BalanceRunTests.Settings, BalanceRunTests.Scenario,
            ClusterMatrixFile.Default with { Seeds = [1], ShipConfigurations = [BalanceRunTests.Starter], MaxHorizonDays = 100 }));
        Assert.Equal("incomplete", capped.Outcome); Assert.Equal("cluster_horizon_cap_reached", capped.StopReason);
    }

    [Fact]
    public void HourlyEconomyAndLedgerAreAuthoritative()
    {
        var c = Assert.Single(Cases.Value);
        Assert.True(c.Outcome == "completed", c.StopReason);
        Assert.True(c.Economy.HourlySamples.Length >= 2401);
        Assert.Equal(Enumerable.Range(0, c.Economy.HourlySamples.Length).Select(h => h * GameCalendar.HourMs), c.Economy.HourlySamples.Select(s => s.GameTimeMs));
        Assert.All(c.Economy.HourlySamples, s =>
        {
            Assert.InRange(s.Stations.Length, 30, 60);
            Assert.All(s.Stations, st => { Assert.InRange(st.Budget, 0, st.MaximumBudget); Assert.All(st.Stocks, i => Assert.InRange(i.Stock, 0, i.Maximum!.Value)); });
            Assert.All(s.Events, e => Assert.True(e.StartedGameTimeMs <= s.GameTimeMs && e.EndsGameTimeMs > s.GameTimeMs));
        });
        Assert.All(c.Economy.Strategies, leg =>
        {
            Assert.Equal("completed", leg.Outcome); Assert.NotNull(leg.Ledger);
            Assert.Equal(leg.SellReceipt!.TotalCredits, leg.Ledger.GrossSalesCredits);
            Assert.Equal(leg.SellReceipt.RealizedCargoCostCredits, leg.Ledger.CostOfGoodsSoldCredits);
            Assert.Equal(leg.Ledger.GrossSalesCredits - leg.Ledger.CostOfGoodsSoldCredits - leg.Ledger.RouteFuelCostCredits - leg.Ledger.PortFeesAssessedCredits - leg.Ledger.EventCostsCredits + leg.Ledger.PassengerPayoutCredits - leg.Ledger.PassengerPenaltyCredits, leg.Ledger.NetProfitCredits);
        });
    }

    [Fact]
    public void LegacyMatrixUnchanged()
    {
        var legacy = Assert.Single(new EconomyBalanceRunner().Run(BalanceRunTests.Settings, BalanceRunTests.Scenario, BalanceRunTests.Matrix()));
        Assert.All(legacy.HourlySamples, s => Assert.Equal(5, s.Stations.Length));
        Assert.Equal(legacy.ContinuousStateHash, legacy.SaveLoadStateHash);
        Assert.Equal(240, Program.ReadMatrix(Program.PackagedMatrixPath, false).HorizonHours);
    }
}
