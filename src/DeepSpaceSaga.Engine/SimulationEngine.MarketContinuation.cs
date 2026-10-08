using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record StagedMarketContinuation(long LastProcessedMarketGameTimeMs,
        long NextMarketRevision, long NextMarketEventSequence);

    private static ScenarioException InvalidMarketContinuation(string detail) =>
        new($"Market continuation: {detail}. Save was not modified.");

    private StagedMarketContinuation StageMarketContinuation(ScenarioFile source, IReadOnlyList<SpaceObjectRuntime> stagedObjects)
    {
        var state = source.GameState;
        // Owning validators already staged geometry/resource references and resolved events before this call.
        // Reuse the market's stock/budget/production invariants; do not introduce shadow market state.
        ValidateMarketWorld(stagedObjects);
        if (state.TradingMap is { } map)
            foreach (var endpoint in map.Edges.SelectMany(e => new[] { e.FromStationObjectId, e.ToStationObjectId }))
                if (!stagedObjects.Any(o => o.InitialMotion.ObjectId == endpoint && o.ObjectType == SpaceObjectType.Station))
                    throw InvalidMarketContinuation($"missing map station '{endpoint}'");
        long highest = Math.Max(stagedObjects.Where(o => o.ObjectType == SpaceObjectType.Station && o.MarketProfileId is not null)
            .Select(o => o.MarketRevision).DefaultIfEmpty().Max(),
            (state.CommandReceipts ?? []).Where(r => r.Status == CommandResultStatus.Executed)
                .Select(r => r.TradeReceipt?.ResultMarketRevision ?? 0).DefaultIfEmpty().Max());
        long nextRevision = highest == long.MaxValue ? long.MaxValue : checked(highest + 1);
        long nextEvent = checked(state.GameTimeMs / GameCalendar.HourMs + 1);
        if (source.SaveFormatVersion >= TradingEconomySaveMigration.ManifestSaveVersion)
        {
            var manifest = state.TradingEconomyContinuation!;
            if (manifest.LastProcessedMarketGameTimeMs != state.GameTimeMs ||
                manifest.NextMarketRevision != nextRevision || manifest.NextMarketEventSequence != nextEvent)
                throw InvalidMarketContinuation($"market cursor {manifest.LastProcessedMarketGameTimeMs}/{state.GameTimeMs}, revision {manifest.NextMarketRevision}/{nextRevision}, event {manifest.NextMarketEventSequence}/{nextEvent} disagree with their owners");
            foreach (var station in stagedObjects.Where(o => o.ObjectType == SpaceObjectType.Station && o.MarketProfileId is not null))
            {
                var saved = state.SpaceObjects.Single(o => o.ObjectId == station.InitialMotion.ObjectId);
                if (saved.MarketRevision != station.MarketRevision)
                    throw InvalidMarketContinuation($"station '{saved.ObjectId}' revision conflicts with saved receipts or is missing");
            }
        }
        return new(state.GameTimeMs, nextRevision, nextEvent);
    }

    private TradingEconomyContinuationData CaptureMarketContinuation(TradingEconomyContinuationData manifest) => manifest with
    {
        LastProcessedMarketGameTimeMs = _processedWorldTimeMs,
        NextMarketEventSequence = checked(_processedWorldTimeMs / GameCalendar.HourMs + 1)
    };

    private void CommitMarketContinuation(StagedMarketContinuation staged)
    {
        // This existing cursor owns (previous,target] scheduling. There is no second hourly cursor/allocator.
        _processedWorldTimeMs = staged.LastProcessedMarketGameTimeMs;
        ResetQuoteSession();
    }
}
