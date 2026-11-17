using System.Text.Json;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0011Tk0004Tests
{
    [Fact]
    public void Profiles_HaveSixAndTenSlotsAndOnlyApprovedPool()
    {
        var content = DemoCampaignDefinitions.Load(DemoEp0033Us0001Tk0003Tests.Root()).Encounters!;
        Assert.Equal(6, content.Profiles.Single(p => p.SignatureTypeId == "lair").DroneCount);
        Assert.Equal(10, content.Profiles.Single(p => p.SignatureTypeId == "tancred").DroneCount);
        Assert.Equal(new[] { "light.sword.shield", "light.spear", "heavy.sword.shield", "heavy.spear.shield" }, content.DroneLayouts);
    }

    [Theory]
    [InlineData("English", "about {0} drones")]
    [InlineData("Russian", "около {0} дронов")]
    public void ReportText_OneApproximateValue(string language, string expected)
    {
        using var locale = JsonDocument.Parse(File.ReadAllText(Path.Combine(DemoEp0033Us0001Tk0003Tests.Root(), "Data", "Locale", language + ".json")));
        Assert.Equal(expected, locale.RootElement.GetProperty("Demo.Pirate.ApproximateDrones").GetString());
        Assert.False(string.IsNullOrWhiteSpace(locale.RootElement.GetProperty("Demo.Pirate.Lair").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(locale.RootElement.GetProperty("Demo.Pirate.Tancred").GetString()));
    }
}
