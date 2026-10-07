using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.EconomyBalance;

namespace DeepSpaceSaga.EconomyBalance.Tests;

public sealed class LongVoyageDiagnosticsTests
{
    [Fact]
    public void HoursDaysMismatchIsReported()
    {
        var c = MarketHealthEvaluatorTests.Healthy();
        c = c with
        {
            HourlySamples = c.HourlySamples.Select(s => s with
            {
                Routes = [s.Routes[0] with { TravelTimeMs = 20 * GameCalendar.DayMs }],
                Events = s.GameTimeMs < 6 * GameCalendar.HourMs ? [new("A", "short", "event", 0, 6 * GameCalendar.HourMs, ["ore"], true)] : []
            }).ToImmutableArray()
        };
        var f = Assert.Single(ClusterEconomyEvaluator.Evaluate(c).Findings, f => f.Code == "event_shorter_than_voyage");
        Assert.Equal("A/B", f.RouteId); Assert.Equal("eventHours=6;routeDays=20", f.Observed);
        Assert.Equal("EP-0001-US-0007", f.OwnerStoryId); Assert.Contains("seed=1", f.Repro);
    }

    [Fact]
    public void NegativeProfitIsNotRewritten()
    {
        var known = Leg(-50, 100); var unknown = Leg(null, null);
        var c = MarketHealthEvaluatorTests.Healthy() with { Strategies = [known, unknown] };
        var assessment = ClusterEconomyEvaluator.Evaluate(c);
        Assert.Contains(assessment.Findings, f => f.Code == "negative_profit" && f.Observed == "netCredits=-50");
        Assert.Contains(assessment.Findings, f => f.Code == "unknown_cost_basis");
        Assert.Equal(-50, c.Strategies[0].Ledger!.NetProfitCredits); Assert.Null(c.Strategies[1].Ledger!.CostOfGoodsSoldCredits);
    }

    [Fact]
    public void MissingThresholdIsNotPass()
    {
        var good = ClusterEconomyEvaluator.Evaluate(MarketHealthEvaluatorTests.Healthy());
        Assert.Equal("passed", good.Correctness); Assert.Equal("not-assessed", good.Balance);
        Assert.Contains(good.Findings, f => f.Code == "missing_profitability_threshold" && f.Kind == "not-assessed");
        var bad = MarketHealthEvaluatorTests.Healthy();
        bad = bad with { HourlySamples = bad.HourlySamples.SetItem(1, bad.HourlySamples[1] with { Stations = bad.HourlySamples[1].Stations.SetItem(0, bad.HourlySamples[1].Stations[0] with { Budget = -1 }) }) };
        var result = ClusterEconomyEvaluator.Evaluate(bad);
        Assert.Equal("violations", result.Correctness); Assert.Equal("not-assessed", result.Balance);
        Assert.Contains(result.Findings, f => f.Code == "budget_bounds" && f.GameTimeMs == GameCalendar.HourMs);
    }

    private static BalanceStrategyEvidence Leg(long? net, long? cost) => new(0, false, "A", "B", "Long", "risk.safe", "ore", "starter",
        100, 100, 1, 1, 1, 0, 1, cost is null ? "unknown" : "completed", null, null, null, null, null,
        new("v", "A/B", "ore", 150, cost, 0, 100, 0, 0, 0, net), [], []);
}
