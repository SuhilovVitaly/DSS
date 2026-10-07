using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    // A geography adapter for the existing EP-0001 evaluator, never a second saved economy.
    // Membership and directions are stable; distances and estimates are sampled anew.
    private TradingMapStateData? CurrentVoyageMap() => BuildClusterVoyageMap(_tradingMap, _clusterMap, _objects, _processedSimulationTimeMs);

    private TradingMapStateData? BuildClusterVoyageMap(TradingMapStateData? legacy, StationClusterMapSnapshot? clusters,
        IReadOnlyList<SpaceObjectRuntime> objects, long motionTime)
    {
        if (legacy is null || clusters is null) return legacy;
        var byId = objects.ToDictionary(o => o.InitialMotion.ObjectId, StringComparer.Ordinal);
        var members = clusters.Stations.ToDictionary(s => s.ObjectId, StringComparer.Ordinal);
        static (string A, string B) Pair(string a, string b) => string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
        var pairs = clusters.Links.Select(l => Pair(l.FromStationId, l.ToStationId))
            .Concat(legacy.Edges.Where(e => members.ContainsKey(e.FromStationObjectId) && members.ContainsKey(e.ToStationObjectId))
                .Select(e => Pair(e.FromStationObjectId, e.ToStationObjectId)))
            .Distinct().OrderBy(p => p.A, StringComparer.Ordinal).ThenBy(p => p.B, StringComparer.Ordinal);
        var ship = objects.First(o => o.ObjectType == SpaceObjectType.PlayerShip);
        double speed = GetMaxSpeedKmS(ship) ?? legacy.Rules.ReferenceSpeedMps / 1000;
        var edges = pairs.Select(p =>
        {
            // Orbit epochs remain absolute when a saved world's motion origins are rebased.
            var a = RuntimeMotion.At(byId[p.A], motionTime);
            var b = RuntimeMotion.At(byId[p.B], motionTime);
            double km = double.Hypot(a.X - b.X, a.Y - b.Y) / 10;
            long estimate = checked(Math.Max(1, (long)Math.Ceiling(km / speed * 300 * 1000)));
            var original = legacy.Edges.FirstOrDefault(e => Connects(e, p.A, p.B));
            return new TradingMapEdgeData(p.A, p.B, km, estimate,
                estimate <= legacy.Rules.ShortMaxGameTimeMs ? "Short" : estimate <= legacy.Rules.MediumMaxGameTimeMs ? "Medium" : "Long",
                original?.FuelMultiplierPermille ?? 1000, original?.RiskProfileId ?? "risk.safe");
        }).ToArray();
        return legacy with
        {
            Rules = legacy.Rules with
            {
                Stations = clusters.Stations.Select(s => new TradingMapStationData(s.ObjectId, s.MarketProfileId,
                byId[s.ObjectId].Name ?? s.ObjectId, byId[s.ObjectId].StationSize.ToString())).ToArray()
            },
            Edges = edges,
            CargoFlows = clusters.Links.Where(l => !l.ItemTypeIds.IsDefaultOrEmpty)
                .Select(l => new TradingMapCargoFlowData(l.FromStationId, l.ToStationId, l.ItemTypeIds)).ToArray()
        };
    }
}
