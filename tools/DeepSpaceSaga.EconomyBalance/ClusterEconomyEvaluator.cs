using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.EconomyBalance;

internal sealed record ClusterEconomyFinding(string Kind, string Code, ulong Seed, string ShipConfigurationId,
    string? StationId, string? RouteId, string? ItemTypeId, long GameTimeMs, string Observed, string OwnerStoryId, string Repro);
internal sealed record ClusterEconomyAssessment(string Correctness, string Balance, ImmutableArray<ClusterEconomyFinding> Findings);

internal static class ClusterEconomyEvaluator
{
    internal static ClusterEconomyAssessment Evaluate(BalanceCaseEvidence evidence)
    {
        var findings = ImmutableArray.CreateBuilder<ClusterEconomyFinding>();
        void Add(string kind, string code, long time, string observed, string owner, string? station = null, string? route = null, string? item = null) =>
            findings.Add(new(kind, code, evidence.Seed, evidence.ShipConfigurationId, station, route, item, time, observed, owner,
                $"--cluster-matrix: seed={evidence.Seed};ship={evidence.ShipConfigurationId};route={route};gameTimeMs={time}"));
        const string markets = "EP-0001-US-0002", events = "EP-0001-US-0007", voyage = "EP-0001-US-0014", ledger = "EP-0001-US-0011";
        var samples = evidence.HourlySamples.OrderBy(s => s.GameTimeMs).ToArray();
        if (samples.Length == 0 || !samples.Select(s => s.GameTimeMs).SequenceEqual(Enumerable.Range(0, samples.Length).Select(i => i * GameCalendar.HourMs)))
            Add("violation", "missing_hourly_evidence", 0, "hourly coverage incomplete", markets);
        if (evidence.ContinuousStateHash != evidence.SaveLoadStateHash || string.IsNullOrWhiteSpace(evidence.ContinuousStateHash))
            Add("violation", "continuation_mismatch", samples.LastOrDefault()?.GameTimeMs ?? 0, "saved continuation differs", voyage);
        foreach (var group in samples.SelectMany(s => s.Stations.Select(st => (Sample: s, Station: st))).GroupBy(x => x.Station.StationId))
        {
            foreach (var entry in group)
            {
                var st = entry.Station;
                if (st.Budget < 0 || st.Budget > st.MaximumBudget) Add("violation", "budget_bounds", entry.Sample.GameTimeMs, $"{st.Budget}/{st.MaximumBudget}", markets, st.StationId);
                foreach (var stock in st.Stocks)
                    if (stock.Stock < 0 || stock.Maximum is null || stock.Stock > stock.Maximum) Add("violation", "stock_bounds", entry.Sample.GameTimeMs, $"{stock.Stock}/{stock.Maximum}", markets, st.StationId, item: stock.ItemTypeId);
            }
            var emptyBudget = group.Where(x => x.Station.Budget == 0).ToArray();
            if (emptyBudget.Length > 0) Add("observation", "budget_exhaustion", emptyBudget[0].Sample.GameTimeMs, $"sampledHours={emptyBudget.Length}", markets, group.Key);
            foreach (var item in group.SelectMany(x => x.Station.Stocks.Select(i => (Time: x.Sample.GameTimeMs, Stock: i))).GroupBy(x => x.Stock.ItemTypeId))
            {
                int run = 0, longest = 0; long began = 0;
                foreach (var s in item.OrderBy(x => x.Time)) { if (s.Stock.Stock == 0) { run++; if (run > longest) { longest = run; began = s.Time - (run - 1) * GameCalendar.HourMs; } } else run = 0; }
                if (longest > 0) Add("observation", "stock_starvation", began, $"longestSampledHours={longest}", markets, group.Key, item: item.Key);
            }
        }
        foreach (var evt in samples.SelectMany(s => s.Events).DistinctBy(e => e.EventId))
        {
            var sample = samples.First(s => s.Events.Any(e => e.EventId == evt.EventId));
            foreach (var route in sample.Routes.Where(r => r.Origin == evt.StationId && r.TravelTimeMs > evt.EndsGameTimeMs - evt.StartedGameTimeMs))
                Add("observation", "event_shorter_than_voyage", sample.GameTimeMs, $"eventHours={(evt.EndsGameTimeMs - evt.StartedGameTimeMs) / (double)GameCalendar.HourMs};routeDays={route.TravelTimeMs / (double)GameCalendar.DayMs}", events, evt.StationId, route.Origin + "/" + route.Destination);
        }
        foreach (var flow in samples.FirstOrDefault()?.CargoFlows ?? [])
        {
            if (!samples[0].Stations.Any(s => s.StationId == flow.Destination && s.Stocks.Any(i => i.ItemTypeId == flow.ItemTypeId)))
                Add("violation", "absent_consumer", 0, "resolved flow has no destination assortment", markets, flow.Destination, flow.Origin + "/" + flow.Destination, flow.ItemTypeId);
            if (!samples[0].CargoFlows.Any(f => f.Origin == flow.Destination && f.Destination == flow.Origin))
                Add("observation", "absent_direct_return_flow", 0, "return may require another station", voyage, route: flow.Origin + "/" + flow.Destination);
        }
        foreach (var leg in evidence.Strategies)
        {
            string route = leg.Origin + "/" + leg.Destination;
            if (leg.Outcome != "completed") Add("observation", "incomplete_return", leg.StateGameTimeMs, leg.Reason ?? leg.Outcome, voyage, route: route, item: leg.ItemTypeId);
            if (leg.Ledger is not { } f) { Add("observation", "missing_ledger", leg.StateGameTimeMs, "not assessed", ledger, route: route); continue; }
            if (f.CostOfGoodsSoldCredits is null || f.NetProfitCredits is null) Add("observation", "unknown_cost_basis", leg.StateGameTimeMs, "profit unknown", ledger, route: route, item: leg.ItemTypeId);
            else if ((Int128)f.GrossSalesCredits - f.CostOfGoodsSoldCredits - f.RouteFuelCostCredits - f.PortFeesAssessedCredits - f.EventCostsCredits + f.PassengerPayoutCredits - f.PassengerPenaltyCredits != f.NetProfitCredits)
                Add("violation", "ledger_mismatch", leg.StateGameTimeMs, $"net={f.NetProfitCredits}", ledger, route: route, item: leg.ItemTypeId);
            if (f.NetProfitCredits < 0) Add("observation", "negative_profit", leg.StateGameTimeMs, $"netCredits={f.NetProfitCredits}", ledger, route: route, item: leg.ItemTypeId);
            if (leg.SellReceipt is { } receipt && (receipt.TotalCredits != f.GrossSalesCredits || receipt.RealizedCargoCostCredits != f.CostOfGoodsSoldCredits))
                Add("violation", "receipt_ledger_mismatch", leg.SellGameTimeMs, "receipt amounts differ", ledger, route: route, item: leg.ItemTypeId);
        }
        Add("not-assessed", "missing_profitability_threshold", 0, "EP-0001 hourly distance margin bands do not define cluster day-distance profitability", "EP-0001-US-0013");
        return new(findings.Any(f => f.Kind == "violation") ? "violations" : "passed", "not-assessed", findings.OrderBy(f => f.Kind, StringComparer.Ordinal).ThenBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.GameTimeMs).ThenBy(f => f.StationId, StringComparer.Ordinal).ThenBy(f => f.RouteId, StringComparer.Ordinal).ThenBy(f => f.ItemTypeId, StringComparer.Ordinal).ToImmutableArray());
    }
}
