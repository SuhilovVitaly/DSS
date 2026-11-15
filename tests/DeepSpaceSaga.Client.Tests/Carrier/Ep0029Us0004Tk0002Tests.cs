using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0004Tk0002Tests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static JsonDocument Frame(string id) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(ClientRoot, "Data", "TacticalBattle", "Frames", $"{id}.json")));

    private static JsonDocument TorpedoDoc() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(ClientRoot, "Data", "Items", "Ammunition", "items-torpedoes.json")));

    [Fact]
    public void CapacitiesAreSixTwelveSix()
    {
        Assert.Equal(6, Frame("light").RootElement.GetProperty("ammoCapacity").GetInt32());
        Assert.Equal(12, Frame("heavy").RootElement.GetProperty("ammoCapacity").GetInt32());
        Assert.Equal(6, Frame("thug").RootElement.GetProperty("ammoCapacity").GetInt32());
    }

    [Fact]
    public void TorpedoIsDiscreteCargo()
    {
        using var doc = TorpedoDoc();
        var item = doc.RootElement.GetProperty("itemTypes")[0];
        Assert.Equal("item.drone-torpedo.t1", item.GetProperty("typeId").GetString());
        Assert.Equal(1, item.GetProperty("unitMassKg").GetInt64());
        Assert.Equal("Piece", item.GetProperty("tradeUnit").GetString());
        Assert.Equal("Cargo", item.GetProperty("storageKind").GetString());
        Assert.Equal(60, item.GetProperty("basePriceCredits").GetInt64());
        Assert.Equal(42, 42 * item.GetProperty("unitMassKg").GetInt64()); // 42 pcs = 42 kg
    }

    [Fact]
    public void NoLegacyAmmoPrefix()
    {
        using var doc = TorpedoDoc();
        string typeId = doc.RootElement.GetProperty("itemTypes")[0].GetProperty("typeId").GetString()!;
        Assert.DoesNotContain("item.ammo.torpedo", typeId, StringComparison.Ordinal);
        Assert.Equal("item.drone-torpedo.t1", typeId);
    }
}
