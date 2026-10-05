using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class StationMarketEventCatalogTests
{
    internal static StationMarketEventDefinition Valid() => new("event.reactor-accident", "Event.Name", "Event.Description",
        "Event.Effect", 10, 5, 2, 4, ["market.mining"], [new("item.water", 1000, 1000, 1200)]);

    internal static GameDataRegistry Registry(IEnumerable<StationMarketEventDefinition> events, bool complete = false)
    {
        var source = QuotedTradeExecutionTests.RealRegistry();
        return GameDataRegistry.Create([], [], Enumerable.Range(0, source.ItemTypes.Count).Select(source.ItemTypes.GetDefinition), [],
            stationMarketProfiles: Enumerable.Range(0, source.StationMarketProfiles.Count).Select(source.StationMarketProfiles.GetDefinition),
            stationMarketEvents: events, requireCompleteMarketEventSet: complete);
    }

    [Fact]
    public void Shipping_catalog_requires_exact_eight_canonical_event_ids()
    {
        Assert.Throws<ContentException>(() => Registry([Valid()], true));
        var definitions = StationMarketEventIds.All.Select((id, i) => Valid() with
        { TypeId = id, DisplayNameKey = $"Name.{i}", DescriptionKey = $"Description.{i}", EffectSummaryKey = $"Effect.{i}" }).ToArray();
        Assert.Equal(8, Registry(definitions, true).StationMarketEvents.Count);
        Assert.Throws<ContentException>(() => Registry(definitions.Append(definitions[0]), true));
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("item")]
    [InlineData("duplicate-item")]
    [InlineData("duplicate-profile")]
    [InlineData("priority")]
    [InlineData("chance")]
    [InlineData("duration")]
    [InlineData("multiplier")]
    [InlineData("delta")]
    [InlineData("fuel")]
    [InlineData("production")]
    [InlineData("demand")]
    [InlineData("key")]
    [InlineData("no-effect")]
    public void Event_catalog_rejects_unknown_references_duplicates_and_ranges(string defect)
    {
        var value = Valid();
        value = defect switch
        {
            "profile" => value with { EligibleMarketProfileIds = ["unknown"] },
            "item" => value with { ItemEffects = [value.ItemEffects[0] with { ItemTypeId = "unknown" }] },
            "duplicate-item" => value with { ItemEffects = [value.ItemEffects[0], value.ItemEffects[0]] },
            "duplicate-profile" => value with { EligibleMarketProfileIds = ["market.mining", "market.mining"] },
            "priority" => value with { Priority = 1001 },
            "chance" => value with { ChancePermillePerHour = -1 },
            "duration" => value with { MaxDurationHours = 169 },
            "multiplier" => value with { ItemEffects = [value.ItemEffects[0] with { PriceMultiplierPermille = 4001 }] },
            "delta" => value with { ItemEffects = [value.ItemEffects[0] with { ActivationStockDelta = 1000001 }] },
            "fuel" => value with { ItemEffects = [value.ItemEffects[0] with { ItemTypeId = "item.fuel" }] },
            "production" => value with { ItemEffects = [value.ItemEffects[0] with { ProductionMultiplierPermille = 900 }] },
            "demand" => value with { ItemEffects = [new("item.iron-ore", 1000, 900, 1200)] },
            "key" => value with { DescriptionKey = value.DisplayNameKey },
            "no-effect" => value with { ItemEffects = [value.ItemEffects[0] with { PriceMultiplierPermille = 1000 }] },
            _ => value,
        };
        Assert.Throws<ContentException>(() => Registry([value]));
    }

    [Theory]
    [InlineData("event.reactor-accident", false)]
    [InlineData("event.pirate-blockade", true)]
    [InlineData("event.quarantine", true)]
    public void Only_blockade_and_quarantine_accept_one_edge_route_effect(string id, bool allowed)
    {
        var value = Valid() with { TypeId = id, RouteEffect = new(StationRouteAvailabilityEffects.Restricted, 1, 1500, 1200, "risk.quarantine") };
        if (allowed) Assert.Equal(1, Registry([value]).StationMarketEvents.Count);
        else Assert.Throws<ContentException>(() => Registry([value]));
        Assert.Throws<ContentException>(() => Registry([value with { RouteEffect = value.RouteEffect! with { MaxAffectedIncidentEdges = 2 } }]));
    }

    [Fact]
    public void Fingerprint_is_canonical_and_changes_with_semantics()
    {
        var value = Valid();
        var first = Registry([value]);
        Assert.Equal("809A124F9B8939EFF221D64B76C96081700129756524D7340BBECD7580EF7F8D", first.StationMarketEventCatalogFingerprint);
        var expanded = value with
        {
            EligibleMarketProfileIds = ["market.mining", "market.transit"],
            ItemEffects = [value.ItemEffects[0], new("item.steel", 1000, 1000, 1100)]
        };
        Assert.Equal(Registry([expanded]).StationMarketEventCatalogFingerprint,
            Registry([expanded with { EligibleMarketProfileIds = expanded.EligibleMarketProfileIds.Reverse().ToImmutableArray(),
                ItemEffects = expanded.ItemEffects.Reverse().ToImmutableArray() }]).StationMarketEventCatalogFingerprint);
        Assert.NotEqual(first.StationMarketEventCatalogFingerprint, Registry([value with { Priority = 11 }]).StationMarketEventCatalogFingerprint);
        Assert.NotEqual(first.StationMarketEventCatalogFingerprint,
            Registry([value with { ItemEffects = [value.ItemEffects[0] with { ActivationStockDelta = 1 }] }]).StationMarketEventCatalogFingerprint);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("version")]
    public void Declared_event_file_is_strict(string defect)
    {
        var json = new JsonObject { ["schemaVersion"] = 1, ["events"] = JsonSerializer.SerializeToNode(new[] { Valid() }) };
        if (defect == "unknown") json["extra"] = true;
        if (defect == "missing") json["events"]![0]!.AsObject().Remove("chancePermillePerHour");
        if (defect == "null") json["events"]![0] = null;
        if (defect == "version") json["schemaVersion"] = 2;
        string path = Path.Combine(Path.GetTempPath(), $"dss-event-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, json.ToJsonString());
            Assert.Throws<ContentException>(() => EngineContentLoader.LoadStationMarketEvents(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Generated_and_legacy_event_save_shapes_roundtrip()
    {
        var value = new StationEventData("instance", "Legacy name", "Description", 3600000, 7200000,
            [new(null, "item.water", 1200)], "event.reactor-accident", "Name", "Description", "Effect",
            [new("item.water", 1000, 1000, 1200, 24)], new("Restricted", 1, 1500, 1200), true);
        var restored = JsonSerializer.Deserialize<StationEventData>(JsonSerializer.Serialize(value))!;
        Assert.Equal(value, restored with { PriceFactors = value.PriceFactors, ItemEffects = value.ItemEffects });
        Assert.Equal(value.PriceFactors, restored.PriceFactors);
        Assert.Equal(value.ItemEffects, restored.ItemEffects);
        var legacy = new StationEventData("legacy", "Authored", null, 0, null, []);
        Assert.Null(JsonSerializer.Deserialize<StationEventData>(JsonSerializer.Serialize(legacy))!.DefinitionId);
        Assert.True(SaveFormat.CurrentSaveFormatVersion >= 12);
    }
}
