using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private enum MarketEventFlow { Production, Demand }

    private static bool MarketEventActive(StationEventRuntime evt, long time) =>
        evt.StartedGameTimeMs <= time && (evt.DurationMs is null || time - evt.StartedGameTimeMs < evt.DurationMs.Value);

    private static ulong MarketEventRoll(ulong seed, string stationId, string definitionId, long hour, string purpose) =>
        RngStreamSeedDerivation.DeriveStreamSeed(seed,
            FormattableString.Invariant($"market-event:{purpose}:{stationId}:{definitionId}:{hour}"));

    // All station candidates are immutable; restore the original list if any checked market calculation fails.
    // Revision assignment/invalidation happens only after the whole boundary succeeds.
    private void ApplyMarketEventAndHour(long time)
    {
        var before = _objects.ToArray();
        try { ApplyMarketEventBoundary(time); ApplyMarketHour(time); }
        catch (OverflowException error) { _objects.Clear(); _objects.AddRange(before); throw new ScenarioException($"Market event/hour boundary {time} overflowed; market state was not modified.", error); }
        catch { _objects.Clear(); _objects.AddRange(before); throw; }
    }

    private void ApplyMarketEventBoundary(long time)
    {
        long hour = time / GameCalendar.HourMs;
        foreach (int index in Enumerable.Range(0, _objects.Count)
            .Where(i => _objects[i].ObjectType == SpaceObjectType.Station)
            .OrderBy(i => _objects[i].InitialMotion.ObjectId, StringComparer.Ordinal))
        {
            var station = _objects[index];
            var original = station.Events.IsDefault ? ImmutableArray<StationEventRuntime>.Empty : station.Events;
            var events = original.Where(e => e.DurationMs is null || e.StartedGameTimeMs > time ||
                time - e.StartedGameTimeMs < e.DurationMs.Value).ToList();
            int slots = 2 - events.Count(e => MarketEventActive(e, time));
            if (slots > 0 && station.MarketProfileId is { } profileId)
            {
                var candidates = Enumerable.Range(0, _registry.StationMarketEvents.Count)
                    .Select(_registry.StationMarketEvents.GetDefinition)
                    .Where(e => e.EligibleMarketProfileIds.Contains(profileId, StringComparer.Ordinal) &&
                        !events.Any(active => active.DefinitionId == e.TypeId && MarketEventActive(active, time)) &&
                        MarketEventRoll(MasterSeed, station.InitialMotion.ObjectId, e.TypeId, hour, "roll") % 1000 < (ulong)e.ChancePermillePerHour)
                    .OrderByDescending(e => e.Priority).ThenBy(e => e.TypeId, StringComparer.Ordinal).Take(slots).ToArray();
                foreach (var definition in candidates)
                {
                    long hours = definition.MinDurationHours + (long)(MarketEventRoll(MasterSeed, station.InitialMotion.ObjectId,
                        definition.TypeId, hour, "duration") % (ulong)(definition.MaxDurationHours - definition.MinDurationHours + 1));
                    long duration = checked(hours * GameCalendar.HourMs);
                    _ = checked(time + duration);
                    string id = FormattableString.Invariant($"{definition.TypeId}@{station.InitialMotion.ObjectId}@{hour}");
                    if (events.Any(e => e.EventId == id)) throw new ScenarioException($"Station '{station.InitialMotion.ObjectId}' duplicate market event '{id}'.");
                    var evt = new StationEventRuntime(id, string.Empty, null, time, duration, EventPriceFactors(definition),
                        definition.TypeId, definition.DisplayNameKey, definition.DescriptionKey, definition.EffectSummaryKey,
                        EventItemEffects(definition), EventRouteEffect(definition), true);
                    station = ApplyActivationStockDeltas(station, evt);
                    events.Add(evt);
                }
            }
            // Preserve the same array when no event changed, so revision tracking stays allocation-free.
            if (!original.SequenceEqual(events)) station = station with { Events = events.ToImmutableArray() };
            _objects[index] = station;
        }
    }

    private ImmutableArray<StationEventPriceFactorRuntime> EventPriceFactors(StationMarketEventDefinition definition) =>
        definition.ItemEffects.Where(e => e.PriceMultiplierPermille != 1000)
            .OrderBy(e => e.ItemTypeId, StringComparer.Ordinal)
            .Select(e => new StationEventPriceFactorRuntime(null, _registry.ItemTypes.GetIndex(e.ItemTypeId), e.PriceMultiplierPermille))
            .ToImmutableArray();

    private static ImmutableArray<StationMarketEventItemEffectData> EventItemEffects(StationMarketEventDefinition definition) =>
        definition.ItemEffects.OrderBy(e => e.ItemTypeId, StringComparer.Ordinal)
            .Select(e => new StationMarketEventItemEffectData(e.ItemTypeId, e.ProductionMultiplierPermille,
                e.DemandMultiplierPermille, e.PriceMultiplierPermille, e.ActivationStockDelta)).ToImmutableArray();

    private static StationMarketEventRouteEffectData? EventRouteEffect(StationMarketEventDefinition definition) =>
        definition.RouteEffect is { } r ? new(r.Availability, r.MaxAffectedIncidentEdges, r.TravelTimeMultiplierPermille,
            r.FuelMultiplierPermille, r.RiskProfileId) : null;

    private SpaceObjectRuntime ApplyActivationStockDeltas(SpaceObjectRuntime station, StationEventRuntime evt)
    {
        if (!TryGetMarket(station, out var profile, out var economy)) return station;
        var stock = station.Inventory.IsDefault ? ImmutableArray.CreateBuilder<StationInventoryItemRuntime>() : station.Inventory.ToBuilder();
        foreach (var effect in evt.ItemEffects.OrderBy(e => e.ItemTypeId, StringComparer.Ordinal))
        {
            if (effect.ActivationStockDelta == 0) continue;
            if (!TryMarketLimits(profile, economy, station.StationSize, effect.ItemTypeId, out var limits))
                throw new ScenarioException($"Station '{station.InitialMotion.ObjectId}' event delta lacks target '{effect.ItemTypeId}'.");
            int slot = FindStockSlot(stock, effect.ItemTypeId);
            long previous = slot < 0 ? 0 : stock[slot].StockQuantity;
            long quantity = Math.Clamp(checked(previous + effect.ActivationStockDelta), 0, limits.MaxStock);
            if (slot < 0) { if (quantity > 0) AddStock(stock, _registry.ItemTypes.GetIndex(effect.ItemTypeId), quantity); }
            else stock[slot] = stock[slot] with { StockQuantity = quantity };
        }
        return station with { Inventory = stock.ToImmutable() };
    }

    private static long ResolveEventRate(SpaceObjectRuntime station, string itemId, long rate, MarketEventFlow flow, long time)
    {
        decimal value = rate;
        if (!station.Events.IsDefaultOrEmpty)
            foreach (var evt in station.Events.Where(e => MarketEventActive(e, time))
                .OrderBy(e => e.StartedGameTimeMs).ThenBy(e => e.EventId, StringComparer.Ordinal))
            {
                if (evt.ItemEffects.IsDefaultOrEmpty) continue;
                foreach (var effect in evt.ItemEffects)
                    if (effect.ItemTypeId == itemId)
                        value = checked(value * (flow == MarketEventFlow.Production ? effect.ProductionMultiplierPermille : effect.DemandMultiplierPermille) / 1000m);
            }
        return checked((long)decimal.Round(value, 0, MidpointRounding.AwayFromZero));
    }

    private ImmutableArray<StationMarketEventSnapshot> BuildActiveEventProjection(SpaceObjectRuntime station, long time)
    {
        if (station.Events.IsDefaultOrEmpty) return [];
        return station.Events.Where(e => MarketEventActive(e, time))
            .OrderBy(e => e.StartedGameTimeMs).ThenBy(e => e.EventId, StringComparer.Ordinal).Select(e =>
            {
                long end = e.DurationMs is { } duration ? checked(e.StartedGameTimeMs + duration) : long.MaxValue;
                var route = e.RouteEffect is { } r ? new StationMarketRouteEffectSnapshot(r.Availability, r.MaxAffectedIncidentEdges,
                    r.TravelTimeMultiplierPermille, r.FuelMultiplierPermille, r.RiskProfileId) : null;
                return new StationMarketEventSnapshot(e.EventId, e.DefinitionId ?? string.Empty, e.DisplayNameKey ?? string.Empty,
                    e.DescriptionKey ?? string.Empty, e.EffectSummaryKey ?? string.Empty, e.StartedGameTimeMs, end,
                    e.DurationMs is null ? long.MaxValue : Math.Max(0, end - time), route, e.DisplayName, e.Description);
            }).ToImmutableArray();
    }

    private ImmutableArray<StationEventRuntime> ResolveMarketEventsForLoad(SpaceObjectData station, long time, string? fingerprint, bool isSave)
    {
        if (station.Events?.Any(e => e is null || e.PriceFactors?.Any(f => f is null) == true) == true)
            throw new ScenarioException($"Station '{station.ObjectId}' contains a null event/price factor. Save was not modified.");
        var runtime = ResolveStationEvents(station);
        if (station.Events is not { Count: > 0 }) return runtime;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var activeDefinitions = new HashSet<string>(StringComparer.Ordinal);
        var resolved = runtime.ToBuilder();
        for (int i = 0; i < station.Events.Count; i++)
        {
            var evt = station.Events[i];
            void Reject(string detail) => throw new ScenarioException($"Station '{station.ObjectId}' event '{evt.EventId}': {detail}. Save was not modified.");
            if (!ids.Add(evt.EventId)) Reject("duplicate event ID");
            if (evt.StartedGameTimeMs < 0 || evt.DurationMs is <= 0 ||
                evt.DurationMs is { } legacyDuration && evt.StartedGameTimeMs > long.MaxValue - legacyDuration) Reject("invalid event interval");
            if (evt.DefinitionId is null)
            {
                if (evt.ItemEffects is not null || evt.RouteEffect is not null || evt.ActivationStockDeltaApplied ||
                    evt.DisplayNameKey is not null || evt.DescriptionKey is not null || evt.EffectSummaryKey is not null) Reject("resolved fields without definition");
                continue;
            }
            if (!isSave || fingerprint != _registry.StationMarketEventCatalogFingerprint || !_registry.StationMarketEvents.Contains(evt.DefinitionId))
                Reject("generated event requires a compatible save/catalog");
            var definition = _registry.StationMarketEvents.GetDefinition(_registry.StationMarketEvents.GetIndex(evt.DefinitionId));
            if (station.MarketProfileId is not { } profile || !definition.EligibleMarketProfileIds.Contains(profile, StringComparer.Ordinal)) Reject("ineligible profile");
            if (evt.StartedGameTimeMs < GameCalendar.HourMs || evt.StartedGameTimeMs > time ||
                evt.StartedGameTimeMs % GameCalendar.HourMs != 0 || evt.DurationMs is not { } duration ||
                duration % GameCalendar.HourMs != 0 || duration < definition.MinDurationHours * GameCalendar.HourMs ||
                duration > definition.MaxDurationHours * GameCalendar.HourMs || !evt.ActivationStockDeltaApplied) Reject("invalid generated interval or unapplied delta");
            if (evt.DurationMs is { } savedDuration && time - evt.StartedGameTimeMs >= savedDuration) Reject("generated event has already ended");
            string expectedId = FormattableString.Invariant($"{definition.TypeId}@{station.ObjectId}@{evt.StartedGameTimeMs / GameCalendar.HourMs}");
            if (evt.EventId != expectedId || !activeDefinitions.Add(evt.DefinitionId)) Reject("invalid instance ID or duplicate definition");
            var expectedItems = EventItemEffects(definition);
            var expectedPrices = EventPriceFactors(definition);
            if (evt.DisplayNameKey != definition.DisplayNameKey || evt.DescriptionKey != definition.DescriptionKey ||
                evt.EffectSummaryKey != definition.EffectSummaryKey || evt.RouteEffect != EventRouteEffect(definition) ||
                evt.ItemEffects is null || evt.ItemEffects.Any(e => e is null) ||
                !evt.ItemEffects.OrderBy(e => e.ItemTypeId, StringComparer.Ordinal).SequenceEqual(expectedItems) ||
                !runtime[i].PriceFactors.OrderBy(e => e.ItemTypeIndex).SequenceEqual(expectedPrices.OrderBy(e => e.ItemTypeIndex))) Reject("resolved payload differs from catalog");
            resolved[i] = runtime[i] with
            {
                DefinitionId = evt.DefinitionId,
                DisplayNameKey = evt.DisplayNameKey,
                DescriptionKey = evt.DescriptionKey,
                EffectSummaryKey = evt.EffectSummaryKey,
                ItemEffects = expectedItems,
                RouteEffect = evt.RouteEffect,
                ActivationStockDeltaApplied = true
            };
        }
        if (resolved.Count(e => MarketEventActive(e, time)) > 2)
            throw new ScenarioException($"Station '{station.ObjectId}' has more than two simultaneous active events. Save was not modified.");
        return resolved.ToImmutable();
    }
}
