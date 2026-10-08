using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private ImmutableArray<StationMarketKnowledgeSnapshot> _marketKnowledge = [];

    private static bool MarketIsAvailable(SpaceObjectRuntime station, bool accessDenied) =>
        !station.IsDestroyed && !accessDenied;

    private bool MarketIsAvailable(SpaceObjectRuntime station) => MarketIsAvailable(station,
        _dialogue.Progress.StationAccessStates.TryGetValue(station.InitialMotion.ObjectId, out var access) && access.AccessDenied);

    private StationMarketKnowledgeSnapshot ObserveStationMarket(SpaceObjectRuntime station, long time, bool available)
    {
        if (!TryGetMarket(station, out var profile, out var economy))
            throw new ScenarioException($"Station '{station.InitialMotion.ObjectId}' has no bounded market to observe.");
        var bands = economy.StockTargets.OrderBy(t => t.ItemTypeId, StringComparer.Ordinal).Select(target =>
        {
            int item = _registry.ItemTypes.GetIndex(target.ItemTypeId);
            TryMarketLimits(profile, economy, station, item, out var limits);
            long stock = station.Inventory.IsDefaultOrEmpty ? 0 : station.Inventory
                .Where(row => row.ItemTypeIndex == item).Sum(row => row.StockQuantity);
            return new StationMarketStockBandSnapshot(target.ItemTypeId, MarketBand(stock, limits.Target, economy));
        }).ToImmutableArray();
        // US-0015 owns a positive Int64 revision. Do not introduce a second counter for the UInt64 wire DTO.
        return new(station.InitialMotion.ObjectId, profile.DisplayName, available, time,
            checked((ulong)station.MarketRevision), false, bands);
    }

    private ImmutableArray<StationMarketKnowledgeSnapshot> InitializeOrLoadMarketKnowledge(
        GameStateData state, IReadOnlyList<SpaceObjectRuntime> objects, int version, bool loadingSave)
    {
        if (state.MarketKnowledge is null)
        {
            if (version >= 14) throw new ScenarioException("Save14 requires marketKnowledge.");
            // Legacy saves contain no evidence of past remote observations. A real local visit can refresh later.
            if (loadingSave) return [];
            return objects.Where(o => o.IsKnown && TryGetMarket(o, out _, out _))
                .OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal)
                .Select(o => ObserveStationMarket(o, state.GameTimeMs, MarketIsAvailable(o,
                    state.DialogueState?.Progress.StationAccessStates.TryGetValue(o.InitialMotion.ObjectId, out var access) == true && access.AccessDenied)))
                .ToImmutableArray();
        }

        var result = ImmutableArray.CreateBuilder<StationMarketKnowledgeSnapshot>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var observed in state.MarketKnowledge)
        {
            if (observed is null || string.IsNullOrWhiteSpace(observed.StationObjectId) || !ids.Add(observed.StationObjectId))
                throw new ScenarioException("Market knowledge requires unique station IDs.");
            var station = objects.FirstOrDefault(o => o.InitialMotion.ObjectId == observed.StationObjectId);
            if (station is null || !station.IsKnown || !TryGetMarket(station, out var profile, out var economy))
                throw new ScenarioException($"Unknown market knowledge station '{observed.StationObjectId}'.");
            if (!string.Equals(observed.StationRole, profile.DisplayName, StringComparison.Ordinal) ||
                observed.ObservedAtGameTimeMs < 0 || observed.ObservedAtGameTimeMs > state.GameTimeMs ||
                observed.ObservedMarketRevision > checked((ulong)station.MarketRevision))
                throw new ScenarioException($"Invalid market observation for '{observed.StationObjectId}'.");
            if (observed.StockBands is null) throw new ScenarioException("Market knowledge requires stock bands.");
            var items = new HashSet<string>(StringComparer.Ordinal);
            var bands = ImmutableArray.CreateBuilder<StationMarketStockBandSnapshot>();
            foreach (var band in observed.StockBands)
            {
                if (band is null || string.IsNullOrWhiteSpace(band.ItemTypeId) || !items.Add(band.ItemTypeId) ||
                    !_registry.ItemTypes.Contains(band.ItemTypeId) || !Enum.IsDefined(band.StockState) ||
                    !TryMarketLimits(profile, economy, station.StationSize, band.ItemTypeId, out _))
                    throw new ScenarioException($"Invalid market stock band for '{observed.StationObjectId}'.");
                bands.Add(new(band.ItemTypeId, band.StockState));
            }
            if (!items.SetEquals(economy.StockTargets.Select(t => t.ItemTypeId)))
                throw new ScenarioException($"Incomplete market stock bands for '{observed.StationObjectId}'.");
            result.Add(new(observed.StationObjectId, observed.StationRole, observed.IsAvailable,
                observed.ObservedAtGameTimeMs, observed.ObservedMarketRevision, false,
                bands.OrderBy(b => b.ItemTypeId, StringComparer.Ordinal).ToImmutableArray()));
        }
        return result.OrderBy(o => o.StationObjectId, StringComparer.Ordinal).ToImmutableArray();
    }

    private void RefreshDockedMarketKnowledge(long time)
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is not { IsDocked: true, DockedStationObjectId: { } stationId }) return;
        var station = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == stationId);
        if (station is null || !station.IsKnown || !TryGetMarket(station, out _, out _)) return;
        var current = ObserveStationMarket(station, time, MarketIsAvailable(station));
        var previous = _marketKnowledge.FirstOrDefault(o => o.StationObjectId == stationId);
        if (previous is not null && previous.ObservedMarketRevision == current.ObservedMarketRevision &&
            previous.IsAvailable == current.IsAvailable && previous.StationRole == current.StationRole &&
            previous.StockBands.SequenceEqual(current.StockBands)) return;
        _marketKnowledge = _marketKnowledge.Where(o => o.StationObjectId != stationId).Append(current)
            .OrderBy(o => o.StationObjectId, StringComparer.Ordinal).ToImmutableArray();
    }

    private ImmutableArray<StationMarketKnowledgeSnapshot> BuildStationMarketKnowledgeProjection(long time)
    {
        RefreshDockedMarketKnowledge(time);
        return _marketKnowledge.Select(observed =>
        {
            var station = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == observed.StationObjectId);
            bool stale = station is null || observed.ObservedMarketRevision != checked((ulong)station.MarketRevision) ||
                observed.IsAvailable != MarketIsAvailable(station);
            return observed with { IsStale = stale };
        }).ToImmutableArray();
    }

    private IReadOnlyList<StationMarketKnowledgeData> CaptureMarketKnowledge(long time)
    {
        RefreshDockedMarketKnowledge(time);
        return _marketKnowledge.Select(o => new StationMarketKnowledgeData(o.StationObjectId, o.StationRole,
            o.IsAvailable, o.ObservedAtGameTimeMs, o.ObservedMarketRevision,
            o.StockBands.Select(b => new StationMarketStockBandData(b.ItemTypeId, b.StockState)).ToImmutableArray())).ToImmutableArray();
    }
}
