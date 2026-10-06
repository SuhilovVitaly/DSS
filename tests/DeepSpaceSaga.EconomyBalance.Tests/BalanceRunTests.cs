using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.EconomyBalance;

namespace DeepSpaceSaga.EconomyBalance.Tests;

public sealed class BalanceRunTests
{
    internal static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    internal static string Settings => Path.Combine(Root, "src/DeepSpaceSaga.Client/Settings.json");
    internal static string Scenario => Path.Combine(Root, "src/DeepSpaceSaga.Client/Scenarios/Docked/scenario.json");
    internal static BalanceShipConfiguration Starter => new("starter", 1000, 1000);
    internal static BalanceShipConfiguration Upgrade => new("cargo-upgrade", 2000, 1000);
    internal static BalanceMatrix Matrix(int hours = 2) => new([1], [Starter], hours * 3_600_000L, 3_600_000, hours / 2 * 3_600_000L);
    private static ImmutableArray<BalanceCaseEvidence> Run(BalanceMatrix? matrix = null, string? scenario = null) =>
        new EconomyBalanceRunner().Run(Settings, scenario ?? Scenario, matrix ?? Matrix());

    [Fact]
    public void Matrix_rejects_duplicate_seed_id_nonpositive_multiplier_or_misaligned_time()
    {
        foreach (var bad in new[] { Matrix() with { Seeds = [] }, Matrix() with { Seeds = [1, 1] },
            Matrix() with { ShipConfigurations = [Starter, Starter] }, Matrix() with { ShipConfigurations = [Starter with { CargoCapacityMultiplierPermille = 0 }] },
            Matrix() with { ShipConfigurations = [Starter with { FuelEfficiencyMultiplierPermille = 999 }] },
            Matrix() with { HorizonGameTimeMs = 0 }, Matrix() with { HorizonGameTimeMs = 7_200_001 },
            Matrix() with { SampleIntervalGameTimeMs = 0 }, Matrix() with { SaveLoadCheckpointGameTimeMs = 0 },
            Matrix() with { SaveLoadCheckpointGameTimeMs = 7_200_000 }, Matrix() with { SaveLoadCheckpointGameTimeMs = 3_600_001 } })
            Assert.Throws<BalanceConfigurationException>(() => new EconomyBalanceRunner().Run("absent", "absent", bad));
    }

    [Fact]
    public void One_seed_two_configs_collect_0_through_240_hour_samples_in_canonical_order()
    {
        var cases = Run(Matrix(240) with { ShipConfigurations = [Starter, Upgrade] });
        Assert.Equal(new[] { "cargo-upgrade", "starter" }, cases.Select(c => c.ShipConfigurationId));
        foreach (var c in cases)
        {
            Assert.Equal(241, c.HourlySamples.Length);
            Assert.Equal(Enumerable.Range(0, 241).Select(h => h * 3_600_000L), c.HourlySamples.Select(s => s.GameTimeMs));
            Assert.All(c.HourlySamples, s => Assert.Equal(5, s.Stations.Length));
            Assert.Equal(c.ContinuousStateHash, c.SaveLoadStateHash);
        }
        Assert.Equal(BalanceCanonical.Json(cases[0].HourlySamples), BalanceCanonical.Json(cases[1].HourlySamples));
    }

    [Fact]
    public void Passive_and_strategy_branches_are_isolated_clones()
    {
        var single = Run(); var both = Run(Matrix() with { ShipConfigurations = [Upgrade, Starter] });
        Assert.Equal(single[0], both.Single(c => c.ShipConfigurationId == "starter"));
        var c = single[0];
        Assert.All(c.Strategies, s => Assert.Equal(c.ShipConfigurationId, s.ShipConfigurationId));
        Assert.Equal(c.ContinuousStateHash, c.SaveLoadStateHash);
    }

    [Fact]
    public void Strategy_evidence_uses_quote_receipt_route_and_matching_authoritative_ledger()
    {
        var c = Run()[0];
        var completed = c.Strategies.Where(s => s.Outcome == "completed").ToArray();
        Assert.True(completed.Length > 0, string.Join("; ", c.Strategies.GroupBy(s => s.Reason).Select(g => $"{g.Key}:{g.Count()}")));
        Assert.True(c.Strategies.Any(s => s.CompletedRoundTrips == 3), string.Join("; ", c.Strategies.Where(s => s.RoundTripLegs.Length > 0)
            .Select(s => $"{s.ItemTypeId}: trips={s.CompletedRoundTrips}, reason={s.RoundTripStopReason}, legs=" + string.Join(',', s.RoundTripLegs.Select(l => l.Outcome + ":" + l.Reason)))));
        var proof = c.Strategies.First(s => s.CompletedRoundTrips == 3);
        Assert.Equal(6, proof.RoundTripLegs.Length);
        Assert.Equal(6, proof.RoundTripLegs.Select(s => s.Ledger!.VoyageId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(proof.RoundTripLegs, s => Assert.Equal(2, s.Replays.Length));
        foreach (var s in completed)
        {
            Assert.NotNull(s.BuyQuote); Assert.NotNull(s.SellQuote); Assert.NotNull(s.Ledger);
            Assert.Equal(s.Origin, s.BuyReceipt!.StationObjectId); Assert.Equal(s.Destination, s.SellReceipt!.StationObjectId);
            Assert.Equal(s.Origin + "/" + s.Destination, s.Ledger.RouteId);
            Assert.Equal(s.SellReceipt.TotalCredits, s.Ledger.GrossSalesCredits);
            Assert.Equal(s.SellReceipt.RealizedCargoCostCredits, s.Ledger.CostOfGoodsSoldCredits);
            Assert.True(s.SellReceipt.ResultMarketRevision > s.SellReceipt.QuotedMarketRevision);
            Assert.True(s.RequestedQuantity <= s.BuyQuote.MaximumQuantity);
            Assert.All(s.Replays, r => { Assert.Equal(r.RevisionBefore, r.RevisionAfter); Assert.Equal(r.PostingsBefore, r.PostingsAfter); Assert.Equal(r.NetBefore, r.NetAfter); Assert.NotNull(r.StaleQuoteReason); });
        }
    }

    [Fact]
    public void Continuous_and_midpoint_save_load_hashes_match()
    {
        Assert.All(Run(Matrix(24)), c => Assert.Equal(c.ContinuousStateHash, c.SaveLoadStateHash));
    }

    [Fact]
    public void Repeated_and_reordered_runs_are_sequence_equal()
    {
        var matrix = Matrix() with { Seeds = [2, 1], ShipConfigurations = [Starter, Upgrade] };
        var first = Run(matrix); var repeated = Run(matrix);
        var reversed = Run(matrix with { Seeds = matrix.Seeds.Reverse().ToImmutableArray(), ShipConfigurations = matrix.ShipConfigurations.Reverse().ToImmutableArray() });
        Assert.True(first.SequenceEqual(repeated)); Assert.True(first.SequenceEqual(reversed));
    }

    [Fact]
    public void Rejected_partial_and_unknown_cogs_results_remain_contextual_evidence()
    {
        var c = Run()[0];
        Assert.Contains(c.Strategies, s => s.Outcome == "rejected" && s.Reason is not null);
        var source = c.Strategies.First(s => s.Ledger is not null);
        foreach (string outcome in new[] { "rejected", "partial", "unknown" })
        {
            var evidence = source with { Outcome = outcome, Ledger = source.Ledger! with { CostOfGoodsSoldCredits = null, NetProfitCredits = null } };
            var restored = JsonSerializer.Deserialize<BalanceStrategyEvidence>(BalanceCanonical.Json(evidence))!;
            Assert.Equal(outcome, restored.Outcome); Assert.Null(restored.Ledger!.NetProfitCredits);
            Assert.Equal(source.Origin, restored.Origin); Assert.Equal(source.StateGameTimeMs, restored.StateGameTimeMs);
        }
    }

    [Fact]
    public void Shipped_default_free_flight_is_docked_through_commands_before_buy()
    {
        var cases = Run(scenario: Path.Combine(Root, "src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json"));
        Assert.All(cases[0].Strategies.Where(s => s.BuyReceipt is { ExecutedQuantity: > 0 }), s =>
        { Assert.Equal(s.Origin, s.BuyReceipt!.StationObjectId); Assert.True(s.BuyGameTimeMs > s.StateGameTimeMs); });
        Assert.Contains(cases[0].Strategies, s => s.BuyReceipt is { ExecutedQuantity: > 0 });
    }
}
