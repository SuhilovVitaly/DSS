using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0035Us0011Tk0001Tests
{
    [Fact]
    public void Signature_RoundtripWithoutComposition()
    {
        var signature = new PirateSignatureObservation("group", PirateSignatureTypes.Lair, "Demo.Pirate.Lair", 100);
        Assert.Equal(signature, JsonSerializer.Deserialize<PirateSignatureObservation>(JsonSerializer.Serialize(signature)));
    }

    [Fact]
    public void PublicSnapshot_ContainsNoDroneClassList()
    {
        var group = new PirateGroupSnapshot("g", 0, 0, 0, 0, 100, true, PirateGroupStatuses.Observed, 6,
            [new("g", PirateSignatureTypes.Tancred, "Demo.Pirate.Tancred", 100)]);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(group));
        Assert.Equal(6, json.RootElement.GetProperty("ApproximateDroneCount").GetInt32());
        Assert.Equal("tancred", json.RootElement.GetProperty("Signatures")[0].GetProperty("SignatureTypeId").GetString());
        foreach (string hidden in new[] { "Drones", "Layouts", "Equipment", "RngState", "ActualDroneCount" })
            Assert.False(json.RootElement.TryGetProperty(hidden, out _));
    }
}
