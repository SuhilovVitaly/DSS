using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TradingEconomySaveSchemaTests
{
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    internal static ScenarioFile WithoutNewContinuation(ScenarioFile source, int version) => source with
    {
        SaveFormatVersion = version,
        GameState = source.GameState with
        {
            TradingEconomyContinuation = null,
            VoyageLedgers = null,
            VoyageFuelSettlements = null,
            EngineIdentityCounters = null,
            MarketKnowledge = version < 14 ? null : source.GameState.MarketKnowledge
        }
    };

    private static ScenarioFile Legacy(ScenarioFile current, int version)
    {
        var state = current.GameState with
        {
            TradingEconomyContinuation = null,
            VoyageLedgers = null,
            VoyageFuelSettlements = null,
            EngineIdentityCounters = null,
            MarketKnowledge = version < 14 ? null : current.GameState.MarketKnowledge
        };
        if (version <= 8) state = state with
        {
            TradingMap = null,
            StationResourceFields = null,
            VoyageState = null,
            LastVoyageFuelSettlement = null,
            SpaceObjects = state.SpaceObjects.Select(o => o with { MarketBudgetCredits = null }).ToArray()
        };
        if (version < 7) state = state with { CatalogCompatibility = null };
        if (version < 6) state = state with { SimulationTimeMs = null };
        if (version < 5) state = state with { EconomyTime = null };
        return current with { SaveFormatVersion = version, GameState = state };
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("schema")]
    [InlineData("short-hash")]
    [InlineData("lowercase-hash")]
    [InlineData("negative-time")]
    [InlineData("future-time")]
    [InlineData("negative-revision")]
    [InlineData("negative-event")]
    [InlineData("duplicate-receipt")]
    [InlineData("unordered-receipt")]
    [InlineData("blank-receipt")]
    [InlineData("missing-seed")]
    public void Current_trading_save_requires_complete_manifest(string defect)
    {
        using var f = TradingVoyageFixture.Create();
        var save = f.Save();
        Assert.Equal(15, SaveFormat.CurrentSaveFormatVersion);
        var manifest = save.GameState.TradingEconomyContinuation!;
        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal(64, manifest.ConfigurationFingerprint.Length);
        Assert.Equal(save.GameState.GameTimeMs, manifest.LastProcessedMarketGameTimeMs);
        var changed = defect switch
        {
            "missing" => null,
            "schema" => manifest with { SchemaVersion = 2 },
            "short-hash" => manifest with { ConfigurationFingerprint = "AB" },
            "lowercase-hash" => manifest with { ConfigurationFingerprint = new string('a', 64) },
            "negative-time" => manifest with { LastProcessedMarketGameTimeMs = -1 },
            "future-time" => manifest with { LastProcessedMarketGameTimeMs = save.GameState.GameTimeMs + 1 },
            "negative-revision" => manifest with { NextMarketRevision = -1 },
            "negative-event" => manifest with { NextMarketEventSequence = -1 },
            "duplicate-receipt" => manifest with { DurableTerminalReceiptIds = ["a", "a"] },
            "unordered-receipt" => manifest with { DurableTerminalReceiptIds = ["b", "a"] },
            "blank-receipt" => manifest with { DurableTerminalReceiptIds = [""] },
            _ => manifest
        };
        var state = save.GameState with { TradingEconomyContinuation = changed };
        if (defect == "missing-seed") state = state with { MasterSeed = null };
        string before = Json(f.Save());
        var error = Assert.Throws<ScenarioException>(() => f.Engine.LoadScenario(save with { GameState = state }, true));
        Assert.Contains("Save was not modified", error.Message);
        Assert.Equal(before, Json(f.Save()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void Legacy_version_classes_normalize_without_repricing(int version)
    {
        using var f = TradingVoyageFixture.Create();
        var source = Legacy(f.Save(), version);
        var before = Json(source);
        var normalized = TradingEconomySaveMigration.Normalize(source);
        Assert.Equal(before, Json(normalized));
        Assert.Equal(version, normalized.SaveFormatVersion);
        Assert.Null(normalized.GameState.TradingEconomyContinuation);
        Assert.NotNull(normalized.MigratedTradingEconomyContinuation);
        Assert.Equal(Json(normalized.MigratedTradingEconomyContinuation),
            Json(TradingEconomySaveMigration.Normalize(normalized).MigratedTradingEconomyContinuation));
        Assert.Equal(before, Json(TradingEconomySaveMigration.Normalize(normalized)));
    }

    [Theory]
    [InlineData("map")]
    [InlineData("budget")]
    [InlineData("knowledge")]
    [InlineData("manifest")]
    public void Partial_new_economy_state_under_legacy_version_is_rejected(string defect)
    {
        using var f = TradingVoyageFixture.Create();
        var current = f.Save();
        var old = Legacy(current, 8);
        var state = old.GameState;
        state = defect switch
        {
            "map" => state with { TradingMap = current.GameState.TradingMap },
            "budget" => state with { SpaceObjects = current.GameState.SpaceObjects },
            "knowledge" => state with { MarketKnowledge = current.GameState.MarketKnowledge },
            _ => state with { TradingEconomyContinuation = current.GameState.TradingEconomyContinuation }
        };
        Assert.Contains("Save was not modified", Assert.Throws<ScenarioException>(() =>
            TradingEconomySaveMigration.Normalize(old with { GameState = state })).Message);
    }

    [Fact]
    public void Migration_is_idempotent_and_preserves_financial_payload()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem, 3);
        f.Send("MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Undock, target: f.Destination);
        var current = f.Save();
        var once = TradingEconomySaveMigration.Normalize(current);
        Assert.Equal(Json(current), Json(TradingEconomySaveMigration.Normalize(once)));
        var legacy = Legacy(current, 14);
        var migrated = TradingEconomySaveMigration.Normalize(legacy);
        Assert.Equal(Json(legacy.GameState), Json(migrated.GameState));
        Assert.Equal(current.GameState.SpaceObjects.Select(o => Json(o)), migrated.GameState.SpaceObjects.Select(o => Json(o)));
        Assert.Equal(Json(current.GameState.VoyageState), Json(migrated.GameState.VoyageState));
        Assert.Equal(Json(current.GameState.CommandReceipts), Json(migrated.GameState.CommandReceipts));
    }

    [Fact]
    public void Future_or_incompatible_manifest_is_rejected_before_load()
    {
        using var f = TradingVoyageFixture.Create();
        var save = f.Save();
        Assert.Throws<ScenarioException>(() => TradingEconomySaveMigration.Normalize(save with { SaveFormatVersion = 16 }));
        var scenario = Legacy(save, 0);
        Assert.Same(scenario, TradingEconomySaveMigration.Normalize(scenario));
        Assert.Throws<ScenarioException>(() => TradingEconomySaveMigration.Normalize(scenario with
        { GameState = scenario.GameState with { TradingEconomyContinuation = save.GameState.TradingEconomyContinuation } }));
    }

    [Fact]
    public void Fingerprint_is_ordinal_canonical_configuration_only_and_revision_maximum_is_preserved()
    {
        using var f = TradingVoyageFixture.Create();
        var state = f.Save().GameState;
        string fingerprint = TradingEconomySaveMigration.ConfigurationFingerprint(state);
        Assert.Equal(fingerprint, TradingEconomySaveMigration.ConfigurationFingerprint(state with
        { PlayerTokens = 17, SpaceObjects = state.SpaceObjects.Reverse().Select(o => o with { Credits = 18, MarketRevision = long.MaxValue }).ToArray() }));
        var map = state.TradingMap!;
        var reordered = map with
        {
            Rules = map.Rules with
            {
                Stations = map.Rules.Stations.Reverse().Select(s => s with { Name = "Renamed" }).ToArray(),
                RiskProfiles = map.Rules.RiskProfiles.Reverse().ToArray(),
                Templates = map.Rules.Templates.Reverse().Select(t => t with
                {
                    Links = t.Links.Reverse().Select(l => l with { FromStationObjectId = l.ToStationObjectId, ToStationObjectId = l.FromStationObjectId }).ToArray(),
                    Offsets = t.Offsets.Reverse().ToArray()
                }).ToArray()
            }
        };
        Assert.Equal(fingerprint, TradingEconomySaveMigration.ConfigurationFingerprint(state with { TradingMap = reordered }));
        Assert.NotEqual(fingerprint, TradingEconomySaveMigration.ConfigurationFingerprint(state with
        { TradingMap = map with { Rules = map.Rules with { ClearanceKm = map.Rules.ClearanceKm + 1 } } }));
        Assert.NotEqual(fingerprint, TradingEconomySaveMigration.ConfigurationFingerprint(state with
        { SpaceObjects = state.SpaceObjects.Select(o => o.MarketProfileId is null ? o : o with { MarketProfileFingerprint = "changed" }).ToArray() }));
        Assert.NotEqual(fingerprint, TradingEconomySaveMigration.ConfigurationFingerprint(state with { MarketEventCatalogFingerprint = "changed" }));
        Assert.Equal(long.MaxValue, TradingEconomySaveMigration.ManifestFromPersistedFacts(state with
        { SpaceObjects = state.SpaceObjects.Select(o => o.ObjectType == SpaceObjectType.Station ? o with { MarketRevision = long.MaxValue } : o).ToArray() }).NextMarketRevision);
    }
}
