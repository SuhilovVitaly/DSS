using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.EconomyBalance;

internal sealed record BalanceViolation(string Code, ulong Seed, string ShipConfigurationId,
    string? StationObjectId, string? RouteId, string? ItemTypeId, long? GameTimeMs, string Expected, string Observed)
{
    internal static ImmutableArray<BalanceViolation> Ordered(IEnumerable<BalanceViolation> source) => source.Distinct()
        .OrderBy(v => v.Code, StringComparer.Ordinal).ThenBy(v => v.Seed)
        .ThenBy(v => v.ShipConfigurationId, StringComparer.Ordinal).ThenBy(v => v.StationObjectId, StringComparer.Ordinal)
        .ThenBy(v => v.RouteId, StringComparer.Ordinal).ThenBy(v => v.ItemTypeId, StringComparer.Ordinal).ThenBy(v => v.GameTimeMs)
        .ThenBy(v => v.Expected, StringComparer.Ordinal).ThenBy(v => v.Observed, StringComparer.Ordinal).ToImmutableArray();
}

/// <summary>Pure ten-day acceptance predicates over detached authoritative evidence.</summary>
internal static class MarketHealthEvaluator
{
    private const long Hour = 3_600_000;
    private const long Horizon = 240 * Hour;
    private static string N(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
    internal static bool Influenced(BalanceHourlySample sample, string station, string item) =>
        !sample.Events.IsDefaultOrEmpty && sample.Events.Any(e => e is not null && e.StationId == station &&
            e.StartedGameTimeMs <= sample.GameTimeMs && e.EndsGameTimeMs > sample.GameTimeMs &&
            !e.InfluencedItems.IsDefaultOrEmpty && e.InfluencedItems.Contains(item, StringComparer.Ordinal));

    internal static ImmutableArray<BalanceViolation> Evaluate(BalanceCaseEvidence evidence)
    {
        if (evidence is null) return [new("invalid_evidence", 0, "missing", null, null, null, null, "nonnull case", "null")];
        var result = new List<BalanceViolation>();
        void Add(string code, string expected, string observed, long? time = null, string? station = null, string? route = null, string? item = null) =>
            result.Add(new(code, evidence.Seed, evidence.ShipConfigurationId, station, route, item, time, expected, observed));
        if (string.IsNullOrWhiteSpace(evidence.ShipConfigurationId)) Add("invalid_evidence", "nonempty ship configuration", "empty ID");
        if (string.IsNullOrWhiteSpace(evidence.ContinuousStateHash) || evidence.ContinuousStateHash != evidence.SaveLoadStateHash)
            Add("state_hash_mismatch", "equal nonempty continuous/save-load hashes", evidence.ContinuousStateHash + "/" + evidence.SaveLoadStateHash);
        if (evidence.HourlySamples.IsDefaultOrEmpty)
        {
            Add("invalid_evidence", "241 hourly samples including0 and240h", "no samples");
            return BalanceViolation.Ordered(result);
        }
        var samples = evidence.HourlySamples.Where(s => s is not null).OrderBy(s => s.GameTimeMs).ToArray();
        var expectedTimes = Enumerable.Range(0, 241).Select(h => h * Hour).ToArray();
        if (samples.Length != evidence.HourlySamples.Length || !samples.Select(s => s.GameTimeMs).SequenceEqual(expectedTimes))
            Add("invalid_evidence", "unique consecutive whole-hour samples0..240h", "sampleCount=" + N(samples.Length),
                expectedTimes.Where(t => !samples.Any(s => s.GameTimeMs == t)).Select(t => (long?)t).FirstOrDefault());
        if (!evidence.Strategies.IsDefaultOrEmpty && evidence.Strategies.Any(s => s is null || s.ShipConfigurationId != evidence.ShipConfigurationId))
            Add("invalid_evidence", "strategy configuration matches case", "missing/mismatched strategy configuration");
        foreach (var sample in samples)
        {
            long time = sample.GameTimeMs;
            var stations = sample.Stations.IsDefault ? [] : sample.Stations.Where(s => s is not null).ToArray();
            var stationIds = stations.Where(s => !string.IsNullOrWhiteSpace(s.StationId)).Select(s => s.StationId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (stations.Length != 5 || stationIds.Length != 5)
                Add("station_count", "five unique station IDs", string.Join(',', stationIds), time);
            if (stations.Length != stationIds.Length || sample.Stations.IsDefault || stations.Length != sample.Stations.Length)
                Add("invalid_evidence", "nonempty unique station keys", "duplicate/empty/default station entries", time);
            var adjacency = stationIds.ToDictionary(id => id, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
            var routes = sample.Routes.IsDefault ? [] : sample.Routes.Where(r => r is not null).ToArray();
            var routeKeys = new HashSet<(string, string)>();
            foreach (var route in routes.OrderBy(r => r.Origin, StringComparer.Ordinal).ThenBy(r => r.Destination, StringComparer.Ordinal))
            {
                string id = route.Origin + "/" + route.Destination;
                if (string.IsNullOrWhiteSpace(route.Origin) || string.IsNullOrWhiteSpace(route.Destination) || route.Origin == route.Destination ||
                    !adjacency.ContainsKey(route.Origin) || !adjacency.ContainsKey(route.Destination) || !routeKeys.Add((route.Origin, route.Destination)) ||
                    !Enum.IsDefined(route.Availability) || !Enum.IsDefined(route.Risk) || route.FuelMultiplierPermille <= 0 || route.TravelTimeMs <= 0)
                { Add("invalid_evidence", "unique valid effective route with known endpoints", id, time, route: id); continue; }
                if (route.Availability == TradingRouteAvailability.Unavailable) continue;
                adjacency[route.Origin].Add(route.Destination); adjacency[route.Destination].Add(route.Origin);
            }
            if (sample.Routes.IsDefault || routes.Length != (sample.Routes.IsDefault ? 0 : sample.Routes.Length))
                Add("invalid_evidence", "declared nonnull routes", "default/null route entries", time);
            if (stationIds.Length > 0)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal) { stationIds[0] };
                var queue = new Queue<string>(); queue.Enqueue(stationIds[0]);
                while (queue.TryDequeue(out var id))
                    foreach (string neighbor in adjacency[id].Order(StringComparer.Ordinal)) if (seen.Add(neighbor)) queue.Enqueue(neighbor);
                var unreachable = stationIds.Where(id => !seen.Contains(id)).ToArray();
                if (unreachable.Length > 0) Add("route_disconnected", "all stations reachable over Available/Restricted edges", string.Join(',', unreachable), time);
            }
            if (sample.Events.IsDefault || !sample.Events.IsDefaultOrEmpty && (sample.Events.Any(e => e is null ||
                    !stationIds.Contains(e.StationId, StringComparer.Ordinal) || string.IsNullOrWhiteSpace(e.EventId) ||
                    e.EndsGameTimeMs <= e.StartedGameTimeMs || e.StartedGameTimeMs > time || e.EndsGameTimeMs <= time || e.InfluencedItems.IsDefault) ||
                    sample.Events.Where(e => e is not null).Select(e => e.EventId).Distinct(StringComparer.Ordinal).Count() != sample.Events.Length))
                Add("invalid_evidence", "unique active events with known station and declared item scope", "invalid event entries", time);
            foreach (var station in stations.OrderBy(s => s.StationId, StringComparer.Ordinal))
            {
                string id = station.StationId;
                if (station.MaximumBudget < 0 || station.Revision < 1)
                    Add("invalid_evidence", "nonnegative budget cap and positive revision", "cap=" + N(station.MaximumBudget) + ",revision=" + N(station.Revision), time, id);
                if (station.Budget < 0) Add("budget_below_zero", "budget>=0", N(station.Budget), time, id);
                if (station.Budget > station.MaximumBudget) Add("budget_above_max", "budget<=" + N(station.MaximumBudget), N(station.Budget), time, id);
                var stocks = station.Stocks.IsDefault ? [] : station.Stocks.Where(s => s is not null).ToArray();
                if (station.Stocks.IsDefault || stocks.Length != station.Stocks.Length || stocks.Any(s => string.IsNullOrWhiteSpace(s.ItemTypeId)) ||
                    stocks.Select(s => s.ItemTypeId).Distinct(StringComparer.Ordinal).Count() != stocks.Length)
                    Add("invalid_evidence", "declared nonnull unique item rows", "invalid stock entries", time, id);
                foreach (var stock in stocks.OrderBy(s => s.ItemTypeId, StringComparer.Ordinal))
                {
                    if (stock.Target is not > 0 || stock.Maximum is null || stock.Maximum < stock.Target || stock.BuyMaximum < 0 || stock.SellMaximum < 0)
                        Add("invalid_evidence", "positive target, maximum>=target, nonnegative buy/sell bounds",
                            "target=" + N(stock.Target) + ",max=" + N(stock.Maximum) + ",buy=" + N(stock.BuyMaximum) + ",sell=" + N(stock.SellMaximum), time, id, item: stock.ItemTypeId);
                    if (stock.Stock < 0) Add("stock_below_zero", "stock>=0", N(stock.Stock), time, id, item: stock.ItemTypeId);
                    if (stock.Maximum is { } max && stock.Stock > max)
                        Add("stock_above_max", "stock<=" + N(max), N(stock.Stock), time, id, item: stock.ItemTypeId);
                }
                var normal = stocks.Where(s => !Influenced(sample, id, s.ItemTypeId)).ToArray();
                // An event exempts its own positions; unrelated rows still must keep the market usable.
                if (normal.Length > 0 || stocks.Length == 0)
                {
                    if (!normal.Any(s => s.BuyMaximum > 0)) Add("station_cannot_buy", "at least one normal buyable position", "0", time, id);
                    if (!normal.Any(s => s.SellMaximum > 0)) Add("station_cannot_sell", "at least one normal sellable position", "0", time, id);
                }
            }
        }
        var flows = samples.SelectMany(s => s.CargoFlows.IsDefault ? [] : s.CargoFlows.Where(f => f is not null))
            .Distinct().OrderBy(f => f.Origin, StringComparer.Ordinal).ThenBy(f => f.Destination, StringComparer.Ordinal).ThenBy(f => f.ItemTypeId, StringComparer.Ordinal);
        var referenceFlows = samples.FirstOrDefault(s => !s.CargoFlows.IsDefaultOrEmpty)?.CargoFlows.Where(f => f is not null).ToHashSet() ?? [];
        if (samples.Any(s => s.CargoFlows.IsDefaultOrEmpty || !referenceFlows.SetEquals(s.CargoFlows.Where(f => f is not null)) ||
                s.CargoFlows.Distinct().Count() != s.CargoFlows.Length || s.CargoFlows.Any(f => f is null ||
                string.IsNullOrWhiteSpace(f.Origin) || string.IsNullOrWhiteSpace(f.Destination) || string.IsNullOrWhiteSpace(f.ItemTypeId) || f.Origin == f.Destination)))
            Add("invalid_evidence", "declared cargo flows with distinct known endpoints and item", "invalid flow entries");
        foreach (var flow in flows)
        {
            var eligible = new List<(long Time, long Stock)>();
            foreach (var sample in samples.Where(s => s.GameTimeMs > 0 && s.GameTimeMs <= Horizon).GroupBy(s => s.GameTimeMs).Select(g => g.First()))
            {
                var station = sample.Stations.IsDefault ? null : sample.Stations.FirstOrDefault(s => s is not null && s.StationId == flow.Origin);
                var stock = station is null || station.Stocks.IsDefault ? null : station.Stocks.FirstOrDefault(s => s is not null && s.ItemTypeId == flow.ItemTypeId);
                if (stock is null || sample.Stations.IsDefault || !sample.Stations.Any(s => s is not null && s.StationId == flow.Destination))
                { Add("invalid_evidence", "mandatory producer/item and destination present", "missing flow row/endpoint", sample.GameTimeMs, flow.Origin, flow.Origin + "/" + flow.Destination, flow.ItemTypeId); continue; }
                if (!Influenced(sample, flow.Origin, flow.ItemTypeId)) eligible.Add((sample.GameTimeMs, stock.Stock));
            }
            long zero = eligible.LongCount(s => s.Stock == 0);
            if (eligible.Count == 0 || zero * 4 > eligible.Count)
                Add("zero_stock_ratio", "zeroStock<=25% of eligible post-boundary samples", eligible.Count == 0 ? "no eligible samples" : N(zero) + "/" + N(eligible.Count),
                    station: flow.Origin, route: flow.Origin + "/" + flow.Destination, item: flow.ItemTypeId);
            for (int i = 0; i + 24 <= eligible.Count; i++)
            {
                if (eligible.Skip(i).Take(24).Any(s => s.Stock > 0)) continue;
                Add("supply_not_recovered", "positive stock within each24 eligible-hour window", N(eligible[i].Time) + ".." + N(eligible[i + 23].Time),
                    eligible[i].Time, flow.Origin, flow.Origin + "/" + flow.Destination, flow.ItemTypeId);
                break;
            }
        }
        return BalanceViolation.Ordered(result);
    }
}
