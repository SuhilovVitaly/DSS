using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Pure version-aware normalization: only persisted identities/cursors, never registry prices or RNG.</summary>
internal static class TradingEconomySaveMigration
{
    internal const int ManifestSaveVersion = 15;

    private static ScenarioException Invalid(string detail) => new($"Trading economy save: {detail}. Save was not modified.");

    internal static ScenarioFile Normalize(ScenarioFile source)
    {
        int version = source.SaveFormatVersion;
        if (version < 0 || version > SaveFormat.CurrentSaveFormatVersion) throw Invalid("unsupported saveFormatVersion");
        var state = source.GameState;
        if (version == 0)
        {
            if (state.TradingEconomyContinuation is not null || state.VoyageLedgers is { Count: > 0 } || state.VoyageFuelSettlements is { Count: > 0 })
                throw Invalid("scenario cannot contain trading continuation history");
            return source;
        }
        // Explicit merged history: 1-4 motion/journal; 5 economy; 6 split clocks; 7 catalog;
        // 8 market profiles; 9 bounded markets; 10 combat; 11 defense; 12 solar;
        // 13 cargo acquisition metadata; 14 market knowledge; 15 complete trading continuation.
        if (version <= 8 && (state.TradingMap is not null || state.StationResourceFields is not null ||
            state.VoyageState is not null || state.LastVoyageFuelSettlement is not null ||
            state.SpaceObjects.Any(o => o.MarketBudgetCredits is not null)))
            throw Invalid("partial trading state under a pre-trading version");
        if (version < 14 && state.MarketKnowledge is { Count: > 0 })
            throw Invalid("market knowledge requires save14 or later");
        if (version >= 14 && state.MarketKnowledge is null) throw Invalid("missing marketKnowledge");
        if (version >= 5 && state.EconomyTime is null) throw Invalid("missing economyTime");
        if (version >= 6 && state.SimulationTimeMs is null) throw Invalid("missing simulationTimeMs");
        if (version >= 7 && state.CatalogCompatibility is null) throw Invalid("missing catalogCompatibility");
        if (version >= ManifestSaveVersion && state.MasterSeed is null) throw Invalid("missing masterSeed");

        var manifest = state.TradingEconomyContinuation;
        if (version < ManifestSaveVersion)
        {
            if (manifest is not null || state.VoyageLedgers is { Count: > 0 } || state.VoyageFuelSettlements is { Count: > 0 })
                throw Invalid("continuation state under a legacy version");
            manifest = ManifestFromPersistedFacts(state);
        }
        else if (manifest is null) throw Invalid("missing tradingEconomyContinuation");
        ValidateManifest(manifest, state.GameTimeMs);
        // A legacy file keeps its original schema/payload; its derived manifest is internal only.
        // The next authoritative capture writes the new version, avoiding current fields under old versions.
        return version < ManifestSaveVersion ? source with { MigratedTradingEconomyContinuation = manifest } : source;
    }

    internal static void ValidateManifest(TradingEconomyContinuationData manifest, long gameTimeMs)
    {
        if (manifest.SchemaVersion != 1 || manifest.ConfigurationFingerprint is not { Length: 64 } fingerprint ||
            fingerprint.Any(c => !(c is >= '0' and <= '9' or >= 'A' and <= 'F')))
            throw Invalid("incompatible continuation schema/fingerprint");
        if (manifest.LastProcessedMarketGameTimeMs < 0 || manifest.LastProcessedMarketGameTimeMs > gameTimeMs ||
            manifest.NextMarketRevision < 0 || manifest.NextMarketEventSequence < 0)
            throw Invalid("invalid continuation cursors");
        var receipts = manifest.DurableTerminalReceiptIds ?? [];
        if (receipts.Any(string.IsNullOrWhiteSpace) || receipts.Distinct(StringComparer.Ordinal).Count() != receipts.Count ||
            !receipts.SequenceEqual(receipts.Order(StringComparer.Ordinal)))
            throw Invalid("terminal receipt IDs must be unique and ordinal-sorted");
    }

    internal static TradingEconomyContinuationData ManifestFromPersistedFacts(GameStateData state)
    {
        long highest = Math.Max(state.SpaceObjects.Select(o => o.MarketRevision ?? 0).DefaultIfEmpty().Max(),
            (state.CommandReceipts ?? []).Where(r => r.Status == CommandResultStatus.Executed).Select(r => r.TradeReceipt?.ResultMarketRevision ?? 0).DefaultIfEmpty().Max());
        return new(1, ConfigurationFingerprint(state), state.GameTimeMs,
            highest == long.MaxValue ? long.MaxValue : checked(highest + 1),
            checked(state.GameTimeMs / GameCalendar.HourMs + 1), ImmutableArray<string>.Empty);
    }

    internal static string ConfigurationFingerprint(GameStateData state)
    {
        var payload = new StringBuilder();
        void Add(string label, string value) => payload.Append(label).Append(':').Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':').Append(value).Append('\n');
        Add("continuation-schema", "1");
        Add("catalog", JsonSerializer.Serialize(state.CatalogCompatibility));
        foreach (var profile in state.SpaceObjects.Where(o => o.MarketProfileId is not null)
            .Select(o => (o.MarketProfileId, o.MarketProfileFingerprint)).Distinct()
            .OrderBy(p => p.MarketProfileId, StringComparer.Ordinal).ThenBy(p => p.MarketProfileFingerprint, StringComparer.Ordinal))
            Add("profile", JsonSerializer.Serialize(new { Id = profile.MarketProfileId, Fingerprint = profile.MarketProfileFingerprint }));
        Add("events", state.MarketEventCatalogFingerprint ?? "");
        if (state.TradingMap is { } map)
        {
            Add("map-schema", map.SchemaVersion.ToString(CultureInfo.InvariantCulture));
            var rules = map.Rules with
            {
                Stations = map.Rules.Stations.OrderBy(s => s.ObjectId, StringComparer.Ordinal).Select(s => s with { Name = "" }).ToArray(),
                RiskProfiles = map.Rules.RiskProfiles.OrderBy(r => r.RiskProfileId, StringComparer.Ordinal).ToArray(),
                Templates = map.Rules.Templates.OrderBy(t => t.TemplateId, StringComparer.Ordinal).Select(t => t with
                {
                    Links = t.Links.Select(l => StringComparer.Ordinal.Compare(l.FromStationObjectId, l.ToStationObjectId) <= 0 ? l : l with
                    { FromStationObjectId = l.ToStationObjectId, ToStationObjectId = l.FromStationObjectId })
                        .OrderBy(l => l.FromStationObjectId, StringComparer.Ordinal).ThenBy(l => l.ToStationObjectId, StringComparer.Ordinal).ThenBy(l => l.RiskProfileId, StringComparer.Ordinal).ToArray(),
                    Offsets = t.Offsets.OrderBy(o => o.StationObjectId, StringComparer.Ordinal).ToArray()
                }).ToArray()
            };
            Add("map-rules", JsonSerializer.Serialize(rules));
        }
        else Add("map-schema", "none");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())));
    }
}
