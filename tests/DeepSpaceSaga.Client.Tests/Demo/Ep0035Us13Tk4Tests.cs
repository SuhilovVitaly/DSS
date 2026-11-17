using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class Ep0035Us13Tk4Tests
{
    [Fact]
    public void ShippedLegacyScenarioAndTwoLairWorldDefinitionHaveCompatibleExplicitContent()
    {
        using var scenarios = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data/TacticalBattle/scenarios.json")));
        var sides = scenarios.RootElement.GetProperty("initial").GetProperty("sides").EnumerateArray().ToArray();
        Assert.Equal(new[] { "A", "B" }, sides.Select(s => s.GetProperty("id").GetString()));
        Assert.All(sides, s => Assert.Equal("carrier.standard", s.GetProperty("carrier").GetProperty("layout").GetString()));
        using var encounters = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data/Demo/encounters.json")));
        Assert.Equal(new[] { "lair", "lair" }, encounters.RootElement.GetProperty("squadMembers").EnumerateArray().Select(e => e.GetString()));
        using var layouts = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data/TacticalBattle/layouts.json")));
        foreach (string id in new[] { "light.sword.shield", "light.spear", "heavy.sword.shield", "heavy.spear.shield" })
        {
            var layout = layouts.RootElement.GetProperty(id);
            Assert.Equal(2, layout.GetProperty("engines").GetArrayLength());
            Assert.Equal("decoy.t1", layout.GetProperty("module").GetString());
        }
    }
}
