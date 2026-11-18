using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0003Tk0005Tests
{
    private static Dictionary<string, string> Text(string language) => JsonSerializer.Deserialize<Dictionary<string, string>>(
        File.ReadAllText(AppContext.BaseDirectory + "/Data/Locale/" + language + ".json"))!
        .Where(p => p.Key.StartsWith("loot.", StringComparison.Ordinal)).ToDictionary(p => p.Key, p => p.Value);

    [Fact]
    public void KeysExistInBothLocales()
    {
        var en = Text("English"); var ru = Text("Russian");
        Assert.NotEmpty(en); Assert.Equal(en.Keys.Order(), ru.Keys.Order());
        foreach (var (key, value) in en)
            Assert.Equal(Regex.Matches(value, @"\{\d+\}").Select(m => m.Value), Regex.Matches(ru[key], @"\{\d+\}").Select(m => m.Value));
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void ReasonsHaveHumanText(string language)
    {
        var text = Text(language);
        foreach (var field in typeof(BattleLootReasons).GetFields())
        {
            var reason = (string)field.GetValue(null)!;
            Assert.True(text.TryGetValue("loot." + reason, out var value));
            Assert.NotEqual(reason, value); Assert.DoesNotContain("loot_", value);
        }
    }

    [Fact]
    public void ExitWarningHasRemainingCount()
    {
        Assert.Contains("{0}", Text("English")["loot.exit_warning"]);
        Assert.Contains("permanently", Text("English")["loot.exit_warning"]);
        Assert.Contains("безвозвратно", Text("Russian")["loot.exit_warning"]);
    }
}
