using System.Text.Json;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0007Tk0003Tests
{
    private static GameDataRegistry Registry() => SaveFixturesTests.Registry();

    [Fact]
    public void KnownPreCarrierCatalogAccepted()
    {
        var registry = Registry();
        var previous = CarrierCatalogCompatibility.Previous(registry);

        // The new torpedo SKU changed the economic digest...
        Assert.NotEqual(registry.CatalogCompatibility.Fingerprint, previous.Fingerprint);
        // ...but a structurally unchanged pre-carrier world is explicitly allowlisted.
        Assert.True(CarrierCatalogCompatibility.Accepts(registry, previous));
        Assert.True(CarrierCatalogCompatibility.Accepts(registry, registry.CatalogCompatibility));
    }

    [Fact]
    public void UnknownFingerprintRejected()
    {
        var registry = Registry();
        var unknown = new CatalogCompatibilityData(registry.CatalogVersion, EconomyTimeData.CurrentRulesVersion, new string('A', 64));
        Assert.False(CarrierCatalogCompatibility.Accepts(registry, unknown));

        // A version/rules mismatch also rejects, even with the right fingerprint.
        var wrongRules = registry.CatalogCompatibility with { RulesVersion = 999 };
        Assert.False(CarrierCatalogCompatibility.Accepts(registry, wrongRules));
    }

    [Fact]
    public void ChangedExistingCommodityNotWhitelisted()
    {
        var registry = Registry();
        // Recompute the pre-carrier digest after altering one PRE-EXISTING commodity: the structural
        // check must reject it, because the allowlist only covers the unchanged old SKUs.
        var changed = Enumerable.Range(0, registry.ItemTypes.Count)
            .Select(registry.ItemTypes.GetDefinition)
            .Where(i => i.TypeId != "item.drone-torpedo.t1")
            .Select((item, idx) => idx == 0 ? item with { BasePriceCredits = (item.BasePriceCredits ?? 0) + 1 } : item);
        var tampered = new CatalogCompatibilityData(registry.CatalogVersion, EconomyTimeData.CurrentRulesVersion,
            GameDataRegistry.EconomicCatalogFingerprint(changed));
        Assert.False(CarrierCatalogCompatibility.Accepts(registry, tampered));
    }

    [Fact]
    public void GiftedAmmoHasExplicitBasis()
    {
        // A gifted torpedo stack carries an explicit zero-cost "bootstrap" basis (no fabricated
        // profit), and that basis survives the save-schema JSON roundtrip — the contract a later
        // rearm relies on to avoid generating revenue from free starter ammo.
        var gifted = new CargoStackData("item.drone-torpedo.t1", 42, CostBasisCredits: 0, AcquisitionSources: ["bootstrap"]);
        string json = JsonSerializer.Serialize(gifted);
        var restored = JsonSerializer.Deserialize<CargoStackData>(json)!;
        Assert.Equal(0, restored.CostBasisCredits);
        Assert.Equal("bootstrap", Assert.Single(restored.AcquisitionSources!));

        // A purchased stack is distinct from a gifted one: it carries a non-zero cost basis.
        var purchased = new CargoStackData("item.drone-torpedo.t1", 42, CostBasisCredits: 2520, AcquisitionSources: ["purchased"]);
        Assert.NotEqual(gifted.CostBasisCredits, purchased.CostBasisCredits);
    }
}
