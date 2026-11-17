using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0003Tk0002Tests
{
    [Fact]
    public void TwoRealFramesAndFiniteEquipmentOffers()
    {
        using var services = JsonDocument.Parse(File.ReadAllText(Root() + "/Data/Demo/services.json"));
        using var layouts = JsonDocument.Parse(File.ReadAllText(Root() + "/Data/TacticalBattle/layouts.json"));
        var frames = services.RootElement.GetProperty("droneOffers").EnumerateArray().ToArray();
        Assert.Equal(2, frames.Length);
        foreach (var offer in frames)
        {
            Assert.True(layouts.RootElement.TryGetProperty(offer.GetProperty("catalogId").GetString()!, out _));
            Assert.True(offer.GetProperty("price").GetInt64() > 0);
            Assert.InRange(offer.GetProperty("stock").GetInt32(), 1, 3);
        }
        Assert.Equal(2, services.RootElement.GetProperty("upgradeOffers").GetArrayLength());
    }
    private static string Root([System.Runtime.CompilerServices.CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../src/DeepSpaceSaga.Client"));
}
