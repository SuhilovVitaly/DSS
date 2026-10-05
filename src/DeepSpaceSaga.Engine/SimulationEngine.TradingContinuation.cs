using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record StagedTradingContinuation(StagedMarketContinuation Market, StagedVoyageContinuation Voyage, EngineIdentityCountersData Identities);

    private string BuildTradingConfigurationFingerprint(GameStateData state) =>
        TradingEconomySaveMigration.ConfigurationFingerprint(state with
        {
            CatalogCompatibility = _registry.CatalogCompatibility,
            MarketEventCatalogFingerprint = _registry.StationMarketEvents.Count > 0 ? _registry.StationMarketEventCatalogFingerprint : null,
            SpaceObjects = state.SpaceObjects.Select(o => o.MarketProfileId is { } id ? o with
            { MarketProfileFingerprint = _registry.StationMarketProfiles.GetDefinition(_registry.StationMarketProfiles.GetIndex(id)).Fingerprint } : o).ToArray()
        });

    private StagedTradingContinuation StageTradingContinuation(ScenarioFile source,
        IReadOnlyList<SpaceObjectRuntime> objects, VoyageStateData? voyage)
    {
        if (source.SaveFormatVersion >= TradingEconomySaveMigration.ManifestSaveVersion &&
            !string.Equals(source.GameState.TradingEconomyContinuation!.ConfigurationFingerprint,
                BuildTradingConfigurationFingerprint(source.GameState), StringComparison.Ordinal) ||
            source.SaveFormatVersion >= TradingEconomySaveMigration.ManifestSaveVersion &&
            !string.Equals(source.GameState.TradingEconomyContinuation!.ConfigurationFingerprint,
                TradingEconomySaveMigration.ConfigurationFingerprint(source.GameState), StringComparison.Ordinal))
            throw new ScenarioException("Incompatible trading configuration fingerprint (catalog/profile/events/map rules). Save was not modified.");
        var counters = source.GameState.EngineIdentityCounters;
        ulong highestCycle = 0;
        foreach (var cycle in objects.SelectMany(o => o.Modules).Select(m => m.ActiveCycle))
            if (cycle?.CycleId is { } id && id.StartsWith("CYC-ENGINE-", StringComparison.Ordinal) &&
                ulong.TryParse(id[11..], out ulong number)) highestCycle = Math.Max(highestCycle, number);
        if (source.SaveFormatVersion >= TradingEconomySaveMigration.ManifestSaveVersion && counters is null ||
            counters is not null && (counters.EngineCycle < highestCycle || counters.EngineCycle == ulong.MaxValue || counters.ShipEvent == ulong.MaxValue))
            throw new ScenarioException("Engine identity counters are missing, exhausted or behind active cycles. Save was not modified.");
        counters ??= new(highestCycle, 0); // Legacy cannot recover historical completed IDs.
        return new(StageMarketContinuation(source, objects), StageVoyageContinuation(source, objects, voyage), counters);
    }

    private TradingEconomyContinuationData CaptureTradingContinuation(GameStateData state) =>
        CaptureMarketContinuation(TradingEconomySaveMigration.ManifestFromPersistedFacts(state)) with
        {
            ConfigurationFingerprint = BuildTradingConfigurationFingerprint(state),
            DurableTerminalReceiptIds = _durableVoyageTerminalIds.Order(StringComparer.Ordinal).ToArray()
        };

    private void CommitTradingContinuation(StagedTradingContinuation staged)
    {
        CommitMarketContinuation(staged.Market);
        CommitVoyageContinuation(staged.Voyage);
        _nextEngineCycleId = staged.Identities.EngineCycle;
        _nextShipEventId = staged.Identities.ShipEvent;
    }

    // The comparison excludes only transient quote session IDs and sub-millimetre binary64
    // motion-origin rebasing. Economic amounts, stable IDs, clocks, receipts and counters stay exact.
    internal static GameStateData NormalizeTradingContinuationForTests(GameStateData state) => state with
    {
        SpaceObjects = state.SpaceObjects.OrderBy(o => o.ObjectId, StringComparer.Ordinal).Select(o => o with
        { PositionX = Math.Round(o.PositionX, 6), PositionY = Math.Round(o.PositionY, 6) }).ToArray(),
        CommandReceipts = state.CommandReceipts?.Select(r => r.TradeReceipt is { } trade
            ? r with { TradeReceipt = trade with { QuoteId = null } } : r).ToArray()
    };
}
