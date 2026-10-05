using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests;

public sealed class StationMarketEventContentTests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private static GameDataRegistry Load(string root) => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(root, "Settings.json"), out _, out _);
    private const string All = "hydroponic,industrial,mining,scientific-military,transit";
    private static readonly (string Id, string Key, string Profiles, int Priority, int Chance, int Min, int Max, string Effects)[] Table =
    [
        ("reactor-accident", "ReactorAccident", All, 90, 4, 6, 18, "energy-cells:500:1600:1500:0"),
        ("decompression", "Decompression", All, 80, 5, 4, 12, "food-rations:1000:1500:1300:0 steel:1000:1300:1200:0 water:1000:1600:1350:0"),
        ("hydroponics-failure", "HydroponicsFailure", "hydroponic", 85, 4, 8, 24, "food-rations:300:1600:1450:0 protein-mass:300:1000:1400:0"),
        ("pirate-blockade", "PirateBlockade", All, 100, 3, 6, 18, "electronics:700:1300:1250:0 food-rations:700:1300:1250:0 steel:700:1300:1250:0 water:700:1300:1250:0"),
        ("cargo-convoy", "CargoConvoy", All, 40, 8, 4, 8, "food-rations:1000:1000:800:24 steel:1000:1000:850:16 water:1000:1000:800:24"),
        ("quarantine", "Quarantine", All, 95, 3, 8, 24, "energy-cells:1000:1200:1150:0 food-rations:1000:1500:1300:0 water:1000:1400:1250:0"),
        ("repair-boom", "RepairBoom", "industrial,mining,transit", 60, 6, 8, 20, "electronics:1000:1500:1350:0 iron-ore:1000:1600:1350:0 magnesium-ore:1000:1500:1300:0 steel:1300:1000:1150:0"),
        ("scientific-contract", "ScientificContract", "scientific-military", 70, 5, 6, 16, "electronics:500:1000:1450:0 energy-cells:1000:1500:1350:0 silicon:1000:1700:1500:0"),
    ];

    [Fact]
    public void Packaged_settings_load_exact_eight_events_and_reviewed_semantic_table()
    {
        foreach (string root in new[] { ClientRoot, AppContext.BaseDirectory })
        {
            Assert.True(File.Exists(Path.Combine(root, "Data", "Markets", "station-market-events.json")));
            var registry = Load(root);
            Assert.Equal(8, registry.StationMarketEvents.Count);
            foreach (var expected in Table)
            {
                var actual = registry.StationMarketEvents.GetDefinition(registry.StationMarketEvents.GetIndex("event." + expected.Id));
                Assert.Equal((expected.Priority, expected.Chance, expected.Min, expected.Max),
                    (actual.Priority, actual.ChancePermillePerHour, actual.MinDurationHours, actual.MaxDurationHours));
                Assert.Equal(expected.Profiles.Split(',').Select(p => "market." + p), actual.EligibleMarketProfileIds);
                Assert.Equal(expected.Effects, string.Join(" ", actual.ItemEffects.Select(e =>
                    $"{e.ItemTypeId[5..]}:{e.ProductionMultiplierPermille}:{e.DemandMultiplierPermille}:{e.PriceMultiplierPermille}:{e.ActivationStockDelta}")));
                Assert.Equal($"TradeUX.Event.{expected.Key}.Name", actual.DisplayNameKey);
                Assert.Equal($"TradeUX.Event.{expected.Key}.Description", actual.DescriptionKey);
                Assert.Equal($"TradeUX.Event.{expected.Key}.Effect", actual.EffectSummaryKey);
                Assert.All(actual.ItemEffects, e => Assert.True(registry.ItemTypes.Contains(e.ItemTypeId)));
                Assert.All(actual.EligibleMarketProfileIds, p => Assert.True(registry.StationMarketProfiles.Contains(p)));
                Assert.DoesNotContain(actual.ItemEffects, e => e.ItemTypeId == "item.fuel");
                if (expected.Id is "pirate-blockade" or "quarantine")
                {
                    var route = Assert.IsType<StationMarketEventRouteEffectDefinition>(actual.RouteEffect);
                    bool blockade = expected.Id == "pirate-blockade";
                    Assert.Equal((blockade ? "Restricted" : "Unavailable", 1, blockade ? 1500 : 1000,
                        blockade ? 1250 : 1000, "risk." + expected.Id),
                        (route.Availability, route.MaxAffectedIncidentEdges, route.TravelTimeMultiplierPermille,
                         route.FuelMultiplierPermille, route.RiskProfileId));
                }
                else Assert.Null(actual.RouteEffect);
            }
        }
    }

    [Fact]
    public void Both_locales_explain_every_event_and_have_matching_placeholders()
    {
        var locales = new[] { "English.json", "Russian.json" }.Select(name =>
            JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(ClientRoot, "Data", "Locale", name)))!).ToArray();
        string[] keys = Table.SelectMany(e => new[] { "Name", "Description", "Effect" }.Select(s => $"TradeUX.Event.{e.Key}.{s}"))
            .Concat(["TradeUX.ActiveEvents", "TradeUX.EventRemainingHours", "TradeUX.EventPermanent"]).ToArray();
        Assert.Equal(27, keys.Length);
        foreach (string key in keys)
        {
            foreach (var locale in locales)
            {
                Assert.True(locale.TryGetValue(key, out string? value));
                Assert.False(string.IsNullOrWhiteSpace(value));
                Assert.DoesNotContain("event.", value!);
                Assert.DoesNotContain('\n', value!);
                Assert.DoesNotContain('\r', value!);
            }
            Assert.Equal(Regex.Matches(locales[0][key], @"\{\d+\}").Select(m => m.Value),
                Regex.Matches(locales[1][key], @"\{\d+\}").Select(m => m.Value));
        }
        foreach (var locale in locales)
        {
            Assert.Single(Regex.Matches(locale["TradeUX.EventRemainingHours"], @"\{0\}"));
            Assert.Contains("42", string.Format(locale["TradeUX.EventRemainingHours"], 42));
            foreach (var expected in Table)
                Assert.Equal(3, new[] { "Name", "Description", "Effect" }.Select(s => locale[$"TradeUX.Event.{expected.Key}.{s}"]).Distinct().Count());
        }
    }
}
