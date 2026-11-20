using System.Text.Json;
using System.Text.RegularExpressions;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0002Tk0004Tests
{
    [Fact]
    public void BattleResultsLocalization_RuEnKeysAndPlaceholders()
    {
        static Dictionary<string, string> Read(string language)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Data", "Locale", language + ".json");
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;
        }

        var en = Read("English");
        var ru = Read("Russian");
        static string[] Keys(Dictionary<string, string> locale) => locale.Keys
            .Where(key => key.StartsWith("BattleResults.", StringComparison.Ordinal) || key.StartsWith("TacticalBattle.Results", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(Keys(en), Keys(ru));
        Assert.Contains("BattleResults.CollectAll", Keys(en));
        Assert.Contains("BattleResults.FreeCabins", Keys(en));
        Assert.Equal("Результаты боя", ru["TacticalBattle.Results"]);
        Assert.Equal("На карту", ru["TacticalBattle.ToMap"]);
        foreach (string key in Keys(en))
            Assert.Equal(Regex.Matches(en[key], @"\{\d+\}").Select(match => match.Value).Order(),
                Regex.Matches(ru[key], @"\{\d+\}").Select(match => match.Value).Order());
    }
}
