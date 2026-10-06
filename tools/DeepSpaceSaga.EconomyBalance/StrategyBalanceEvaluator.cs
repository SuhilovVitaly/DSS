using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.EconomyBalance;

internal sealed record BalanceStrategyIdentity(long StateGameTimeMs, string Origin, string Destination, string ItemTypeId);

internal sealed record BalanceLeader(long StateGameTimeMs, bool EventState, string RouteId, string ItemTypeId,
    long NetProfitCredits, long MarginPermille, ImmutableArray<string> TiedRouteItems);

/// <summary>Financial identities audit published facts; no amounts become gameplay inputs.</summary>
internal static class StrategyBalanceEvaluator
{
    private static string Key(BalanceStrategyEvidence s) => s.Origin + "/" + s.Destination + "/" + s.ItemTypeId;
    private static BalanceStrategyIdentity StateKey(BalanceStrategyEvidence s) => new(s.StateGameTimeMs, s.Origin, s.Destination, s.ItemTypeId);
    internal static long? Margin(BalanceLedgerEvidence? ledger)
    {
        if (ledger?.CostOfGoodsSoldCredits is not > 0 || ledger.NetProfitCredits is null) return null;
        decimal rounded = decimal.Round((decimal)ledger.NetProfitCredits.Value * 1000 / ledger.CostOfGoodsSoldCredits.Value, 0, MidpointRounding.AwayFromZero);
        return rounded < long.MinValue || rounded > long.MaxValue ? null : (long)rounded;
    }
    private static bool Formula(BalanceStrategyEvidence s)
    {
        if (s.Ledger is not { } l || string.IsNullOrWhiteSpace(l.VoyageId) || l.RouteId != s.Origin + "/" + s.Destination || l.ItemTypeId != s.ItemTypeId ||
            l.GrossSalesCredits < 0 || l.CostOfGoodsSoldCredits < 0 || l.RouteFuelCostCredits < 0 || l.PortFeesAssessedCredits < 0 ||
            l.EventCostsCredits < 0 || l.PassengerPayoutCredits < 0 || l.PassengerPenaltyCredits < 0) return false;
        if (l.CostOfGoodsSoldCredits is null) return l.NetProfitCredits is null;
        Int128 net = (Int128)l.GrossSalesCredits - l.CostOfGoodsSoldCredits.Value - l.RouteFuelCostCredits - l.PortFeesAssessedCredits - l.EventCostsCredits + l.PassengerPayoutCredits - l.PassengerPenaltyCredits;
        return l.NetProfitCredits is { } published && net == published;
    }
    private static bool Binding(BalanceStrategyEvidence s)
    {
        if (string.IsNullOrWhiteSpace(s.Origin) || string.IsNullOrWhiteSpace(s.Destination) || s.Origin == s.Destination || string.IsNullOrWhiteSpace(s.ItemTypeId) ||
            s.ExecutedBuyQuantity <= 0 || s.ExecutedSellQuantity <= 0 || s.ExecutedBuyQuantity > s.RequestedQuantity || s.ExecutedSellQuantity > s.ExecutedBuyQuantity ||
            s.RequestedQuantity > s.BatchCeilingQuantity || s.ActualCapacityKg < 0 || s.AnalyticalCapacityKg < 0 || s.ReservedArrivalFeeCredits < 0 ||
            s.BuyReceipt is not { } buy || s.SellReceipt is not { } sell || s.BuyQuote is not { } bq || s.SellQuote is not { } sq) return false;
        return buy.TotalCredits >= 0 && sell.TotalCredits >= 0 && sell.ExecutedQuantity <= sq.MaximumQuantity && sell.ExecutedQuantity <= sell.RequestedQuantity && buy.StationObjectId == s.Origin && sell.StationObjectId == s.Destination && buy.ItemTypeId == s.ItemTypeId && sell.ItemTypeId == s.ItemTypeId &&
            buy.ExecutedQuantity == s.ExecutedBuyQuantity && sell.ExecutedQuantity == s.ExecutedSellQuantity && buy.RequestedQuantity == s.RequestedQuantity &&
            bq.StationId == s.Origin && sq.StationId == s.Destination && bq.ItemTypeId == s.ItemTypeId && sq.ItemTypeId == s.ItemTypeId &&
            bq.CommandType == TradeCommandTypes.Buy && sq.CommandType == TradeCommandTypes.Sell && bq.RequestedQuantity == s.RequestedQuantity &&
            sq.RequestedQuantity == sell.RequestedQuantity && bq.DisabledReason is null && sq.DisabledReason is null &&
            buy.ExecutedQuantity == buy.RequestedQuantity && buy.ExecutedQuantity == bq.ExecutableQuantity && sell.ExecutedQuantity == sq.ExecutableQuantity &&
            buy.TotalCredits == bq.TotalCredits && sell.TotalCredits == sq.TotalCredits &&
            s.RequestedQuantity <= bq.MaximumQuantity && buy.QuotedMarketRevision is > 0 && buy.ResultMarketRevision > buy.QuotedMarketRevision &&
            sell.QuotedMarketRevision is > 0 && sell.ResultMarketRevision > sell.QuotedMarketRevision && buy.QuotedMarketRevision == bq.Revision && sell.QuotedMarketRevision == sq.Revision &&
            s.Ledger is { } l && l.GrossSalesCredits == sell.TotalCredits && l.CostOfGoodsSoldCredits == sell.RealizedCargoCostCredits;
    }
    private static BalanceRoute? Route(BalanceCaseEvidence e, BalanceStrategyEvidence s)
    {
        if (e.HourlySamples.IsDefaultOrEmpty) return null;
        var matches = e.HourlySamples.Where(h => h is not null && h.GameTimeMs == s.StateGameTimeMs && !h.Routes.IsDefault)
            .SelectMany(h => h.Routes).Where(r => r is not null && r.Origin == s.Origin && r.Destination == s.Destination).ToArray();
        return matches.Length == 1 && matches[0].DistanceClass == s.DistanceClass && matches[0].RiskProfileId == s.RiskProfileId ? matches[0] : null;
    }
    private static ImmutableArray<BalanceStrategyEvidence> Candidates(BalanceCaseEvidence e) => e.Strategies.IsDefaultOrEmpty ? [] :
        e.Strategies.Where(s => s is not null).GroupBy(StateKey).Where(g => g.Count() == 1).Select(g => g.Single())
            .Where(s => s.ShipConfigurationId == e.ShipConfigurationId && s.Outcome == "completed" && s.ExecutedBuyQuantity == s.ExecutedSellQuantity &&
                Formula(s) && Binding(s) && Margin(s.Ledger) is not null && Route(e, s)?.Availability is TradingRouteAvailability.Available or TradingRouteAvailability.Restricted)
            .OrderBy(s => s.StateGameTimeMs).ThenBy(Key, StringComparer.Ordinal).ToImmutableArray();

    internal static ImmutableArray<BalanceLeader> Leaders(BalanceCaseEvidence e) => Candidates(e).GroupBy(s => s.StateGameTimeMs).OrderBy(g => g.Key).Select(g =>
    {
        var ranked = g.OrderByDescending(s => s.Ledger!.NetProfitCredits).ThenByDescending(s => Margin(s.Ledger))
            .ThenBy(s => s.Origin + "/" + s.Destination, StringComparer.Ordinal).ThenBy(s => s.ItemTypeId, StringComparer.Ordinal).ToArray();
        var winner = ranked[0]; long margin = Margin(winner.Ledger)!.Value;
        return new BalanceLeader(g.Key, winner.EventState, winner.Origin + "/" + winner.Destination, winner.ItemTypeId,
            winner.Ledger!.NetProfitCredits!.Value, margin, ranked.Where(s => s.Ledger!.NetProfitCredits == winner.Ledger.NetProfitCredits && Margin(s.Ledger) == margin)
                .Select(Key).Order(StringComparer.Ordinal).ToImmutableArray());
    }).ToImmutableArray();

    internal static ImmutableArray<BalanceViolation> EvaluateCase(BalanceCaseEvidence evidence)
    {
        if (evidence is null) return [new("missing_comparable_strategy", 0, "missing", null, null, null, null, "nonnull case", "null")];
        var violations = new List<BalanceViolation>();
        void Add(string code, string expected, string observed, BalanceStrategyEvidence? s = null, long? time = null) => violations.Add(new(code, evidence.Seed,
            evidence.ShipConfigurationId, s?.Origin, s is null ? null : s.Origin + "/" + s.Destination, s?.ItemTypeId, s?.StateGameTimeMs ?? time, expected, observed));
        var strategies = evidence.Strategies.IsDefault ? [] : evidence.Strategies.Where(s => s is not null).ToArray();
        if (strategies.Length != (evidence.Strategies.IsDefault ? 0 : evidence.Strategies.Length))
            Add("ledger_formula_mismatch", "nonnull strategies", "null strategy entry");
        foreach (var duplicate in strategies.GroupBy(StateKey).Where(g => g.Count() > 1))
            Add("ledger_formula_mismatch", "one strategy per state/route/item", "duplicate=" + BalanceCanonical.Json(duplicate.Key), duplicate.First());
        foreach (var s in strategies)
        {
            if (s.ShipConfigurationId != evidence.ShipConfigurationId || s.Ledger is not null && !Formula(s) ||
                s.Outcome is "completed" or "partial" && (!Binding(s) || Route(evidence, s) is null))
                Add("ledger_formula_mismatch", "one matching receipt-bound ledger with nonnegative components and exact published net identity", BalanceCanonical.Json(new { s.Outcome, s.Ledger, s.BuyReceipt, s.SellReceipt }), s);
            if (s.Ledger is { CostOfGoodsSoldCredits: > 0, NetProfitCredits: not null } && Margin(s.Ledger) is null)
                Add("ledger_formula_mismatch", "rounded margin representable as Int64", BalanceCanonical.Json(s.Ledger), s);
            AuditPostings(s); AuditReplays(s);
            if (!s.RoundTripLegs.IsDefaultOrEmpty)
            {
                var legs = s.RoundTripLegs.Where(l => l is not null).ToArray();
                var ids = legs.Where(l => l.Ledger is not null).Select(l => l.Ledger!.VoyageId).ToArray();
                if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
                    Add("duplicate_ledger_posting", "distinct accepted voyage IDs in one replay-proof world", string.Join(',', ids), s);
                foreach (var leg in legs)
                {
                    if (leg.Outcome is "completed" or "partial" && (!Formula(leg) || !Binding(leg)))
                        Add("ledger_formula_mismatch", "valid financial/receipt identity on every round-trip leg", BalanceCanonical.Json(leg.Ledger), leg);
                    AuditPostings(leg); AuditReplays(leg);
                }
            }
        }
        void AuditPostings(BalanceStrategyEvidence s)
        {
            if (s.Ledger is null) return;
            if (s.PostingIds.IsDefaultOrEmpty || s.PostingIds.Any(string.IsNullOrWhiteSpace) || s.PostingIds.Distinct(StringComparer.Ordinal).Count() != s.PostingIds.Length)
                Add("duplicate_ledger_posting", "nonempty unique monetary posting IDs per ledger", s.PostingIds.IsDefault ? "default" : string.Join(',', s.PostingIds), s);
        }
        void AuditReplays(BalanceStrategyEvidence s)
        {
            if (s.Replays.IsDefaultOrEmpty) return;
            foreach (var replay in s.Replays)
            {
                if (replay is null) { Add("stale_quote_or_command_reapplied", "nonnull replay evidence", "null", s); continue; }
                if (string.IsNullOrWhiteSpace(replay.CommandId) || string.IsNullOrWhiteSpace(replay.StaleQuoteReason) || replay.PostingsBefore < 0 ||
                    replay.PostingsBefore != replay.PostingsAfter || replay.RevisionBefore != replay.RevisionAfter || replay.NetBefore != replay.NetAfter)
                    Add("stale_quote_or_command_reapplied", "old command/quote changes no revision, postings or net", BalanceCanonical.Json(replay), s);
                if (replay.RevisionBefore == replay.RevisionAfter && replay.NetBefore != replay.NetAfter)
                    Add("unchanged_market_created_profit", "unchanged revision implies zero new realized profit", BalanceCanonical.Json(replay), s);
            }
        }
        bool ValidRoundTrips(BalanceStrategyEvidence s)
        {
            if (s.CompletedRoundTrips != 3 || s.RoundTripStopReason is not null || s.RoundTripLegs.IsDefault || s.RoundTripLegs.Length != 6) return false;
            if (s.RoundTripLegs.Any(l => l is null || l.ShipConfigurationId != s.ShipConfigurationId || l.StateGameTimeMs != s.StateGameTimeMs || l.Outcome is not ("completed" or "partial") || l.Ledger?.NetProfitCredits is null ||
                    !Formula(l) || !Binding(l) || l.Replays.IsDefault || l.Replays.Length != 2 || l.BuyGameTimeMs < s.StateGameTimeMs || l.SellGameTimeMs < l.BuyGameTimeMs ||
                    l.Replays.Any(r => r is null || string.IsNullOrWhiteSpace(r.CommandId) || r.PostingsBefore != r.PostingsAfter ||
                        r.RevisionBefore != r.RevisionAfter || r.NetBefore != r.NetAfter || string.IsNullOrWhiteSpace(r.StaleQuoteReason)))) return false;
            if (s.RoundTripLegs[0].Ledger != s.Ledger || s.RoundTripLegs.Select(l => l.Ledger!.VoyageId).Distinct(StringComparer.Ordinal).Count() != 6 ||
                s.RoundTripLegs.SelectMany(l => l.Replays).Select(r => r.CommandId).Distinct(StringComparer.Ordinal).Count() != 12) return false;
            var revisions = new Dictionary<string, long>(StringComparer.Ordinal);
            for (int i = 0; i < s.RoundTripLegs.Length; i++)
            {
                var leg = s.RoundTripLegs[i];
                if (leg.Origin != (i % 2 == 0 ? s.Origin : s.Destination) || leg.Destination != (i % 2 == 0 ? s.Destination : s.Origin) ||
                    i > 0 && leg.BuyGameTimeMs < s.RoundTripLegs[i - 1].SellGameTimeMs) return false;
                foreach (var receipt in new[] { leg.BuyReceipt!, leg.SellReceipt! })
                {
                    if (revisions.TryGetValue(receipt.StationObjectId!, out long previous) && receipt.QuotedMarketRevision < previous) return false;
                    revisions[receipt.StationObjectId!] = receipt.ResultMarketRevision!.Value;
                }
            }
            return true;
        }
        if (!strategies.Any(ValidRoundTrips)) Add("missing_comparable_strategy", "three real round trips with six receipt/ledger/replay-bound legs", "no complete replay proof");
        var candidates = Candidates(evidence);
        string Observed(IEnumerable<BalanceStrategyEvidence> source) => BalanceCanonical.Json(source.OrderBy(s => s.StateGameTimeMs).ThenBy(Key, StringComparer.Ordinal)
            .Select(s => new { s.StateGameTimeMs, Route = Key(s), Margin = Margin(s.Ledger), s.Ledger }));
        foreach (string distanceClass in new[] { "Short", "Medium", "Long" })
        {
            var subset = candidates.Where(s => s.DistanceClass == distanceClass && (distanceClass != "Short" || Route(evidence, s)?.Risk == TradingRouteRisk.Safe)).ToArray();
            if (subset.Length == 0) Add("missing_comparable_strategy", "known positive-COGS completed " + distanceClass + " strategy", Observed(strategies.Where(s => s.DistanceClass == distanceClass)));
            if (distanceClass == "Short" && !subset.Any(s => Margin(s.Ledger) is >= 50 and <= 150))
                Add("short_margin_band", "at least one safe Short margin50..150 permille", Observed(subset));
            if (distanceClass == "Medium" && !subset.Any(s => Margin(s.Ledger) is >= 150 and <= 350))
                Add("medium_margin_band", "at least one Medium margin150..350 permille", Observed(subset));
        }
        var states = strategies.Select(s => s.StateGameTimeMs).Distinct().Order();
        foreach (long state in states)
        {
            var positive = candidates.Where(s => s.StateGameTimeMs == state && s.Ledger!.NetProfitCredits > 0).ToArray();
            var margins = positive.Select(s => Margin(s.Ledger)!.Value).Order().ToArray();
            if (margins.Length < 2) { Add("missing_comparable_strategy", "at least two positive comparable strategies per state", Observed(positive), time: state); continue; }
            decimal median = margins.Length % 2 == 1 ? margins[margins.Length / 2] : ((decimal)margins[margins.Length / 2 - 1] + margins[margins.Length / 2]) / 2;
            if (margins[^1] > 2 * median)
                Add("route_margin_dominance", "maximum margin<=2*median", FormattableString.Invariant($"max={margins[^1]};median={median};candidates=") + Observed(positive), time: state);
        }
        var leaders = Leaders(evidence);
        if (leaders.Length > 1 && leaders.Select(l => l.RouteId + "/" + l.ItemTypeId).Distinct(StringComparer.Ordinal).Count() == 1)
            Add("route_always_best", "no route/item wins every checked state", BalanceCanonical.Json(leaders));
        var normal = leaders.Where(l => !l.EventState).ToArray(); var events = leaders.Where(l => l.EventState).ToArray();
        if (normal.Length > 0 && events.Length > 0 && !normal.Any(n => events.Any(ev => n.RouteId != ev.RouteId || n.ItemTypeId != ev.ItemTypeId)))
            Add("event_did_not_change_leader", "at least one normal/event leader change", BalanceCanonical.Json(leaders));
        return BalanceViolation.Ordered(violations);
    }

    internal static ImmutableArray<BalanceViolation> EvaluateCorpus(ImmutableArray<BalanceCaseEvidence> evidence)
    {
        var cases = evidence.IsDefault ? [] : evidence.Where(e => e is not null).OrderBy(e => e.Seed).ThenBy(e => e.ShipConfigurationId, StringComparer.Ordinal).ToArray();
        var violations = new List<BalanceViolation>();
        var pairs = new List<object>(); bool crossover = false;
        foreach (var starter in cases.Where(e => e.ShipConfigurationId == "starter"))
        {
            var upgrades = cases.Where(e => e.Seed == starter.Seed && e.ShipConfigurationId == "cargo-upgrade").ToArray();
            if (upgrades.Length != 1) continue;
            var upgraded = Candidates(upgrades[0]).Where(s => s.DistanceClass == "Long").ToDictionary(StateKey);
            foreach (var s in Candidates(starter).Where(s => s.DistanceClass == "Long"))
                if (upgraded.TryGetValue(StateKey(s), out var u))
                {
                    pairs.Add(new { starter.Seed, s.StateGameTimeMs, RouteItem = Key(s), Starter = s.Ledger, Upgrade = u.Ledger, s.RequestedQuantity, UpgradedQuantity = u.RequestedQuantity, u.BatchCeilingQuantity });
                    crossover |= s.Ledger!.NetProfitCredits <= 0 && u.Ledger!.NetProfitCredits > 0;
                }
        }
        if (!crossover) violations.Add(new("long_upgrade_crossover", 0, "corpus", null, null, null, null,
            "same seed/state/Long route/item: starter net<=0 and cargo-upgrade net>0", BalanceCanonical.Json(pairs)));
        if (cases.Length == 0 || cases.GroupBy(c => (c.Seed, c.ShipConfigurationId)).Any(g => g.Count() != 1) || cases.Length != (evidence.IsDefault ? 0 : evidence.Length))
            violations.Add(new("missing_comparable_strategy", 0, "corpus", null, null, null, null, "unique nonnull seed/config cases", "duplicate/null case entries"));
        bool changed = cases.Any(c => { var leaders = Leaders(c); return leaders.Where(l => !l.EventState).Any(n => leaders.Where(l => l.EventState).Any(ev => n.RouteId != ev.RouteId || n.ItemTypeId != ev.ItemTypeId)); });
        if (!changed) violations.Add(new("event_did_not_change_leader", 0, "corpus", null, null, null, null,
            "at least one case has an observed normal/event leader change", "no qualifying leader change"));
        return BalanceViolation.Ordered(violations);
    }
}
