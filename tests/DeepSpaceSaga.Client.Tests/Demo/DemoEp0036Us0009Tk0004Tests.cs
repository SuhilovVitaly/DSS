using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0009Tk0004Tests
{
    [Fact]
    public void KeysAndReasonsHaveMatchingParameters()
    {
        Dictionary<string, string> Read(string language) => JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(AppContext.BaseDirectory + "/Data/Locale/" + language + ".json"))!;
        var en = Read("English"); var ru = Read("Russian");
        var keys = en.Keys.Where(k => k.StartsWith("Prisoners.", StringComparison.Ordinal)).Concat(
            typeof(PrisonerCustodyReasons).GetFields().Select(f => "loot." + f.GetValue(null)));
        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(en[key]));
            Assert.False(string.IsNullOrWhiteSpace(ru[key]));
            Assert.Equal(Regex.Matches(en[key], @"\{\d+\}").Select(m => m.Value), Regex.Matches(ru[key], @"\{\d+\}").Select(m => m.Value));
        }
        Assert.Contains("no wages", en["Prisoners.Upkeep"]);
        Assert.Contains("без жалованья", ru["Prisoners.Upkeep"]);
    }
}
