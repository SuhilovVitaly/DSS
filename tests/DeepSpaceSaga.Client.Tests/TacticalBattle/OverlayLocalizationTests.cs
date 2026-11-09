using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class OverlayLocalizationTests
{
    [Fact]
    public void New_ui_keys_have_identical_placeholder_contracts()
    {
        Dictionary<string, string> Read(string name) => JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Locale", name + ".json")))!;
        var ru = Read("Russian"); var en = Read("English");
        foreach (var pair in ru.Where(p => p.Key.StartsWith("TacticalBattle.Ui.", StringComparison.Ordinal)))
        {
            Assert.True(en.ContainsKey(pair.Key));
            Assert.Equal(System.Text.RegularExpressions.Regex.Matches(pair.Value, @"\{\d+\}").Select(m => m.Value).Order(),
                System.Text.RegularExpressions.Regex.Matches(en[pair.Key], @"\{\d+\}").Select(m => m.Value).Order());
        }
        Assert.True(ru.Count(p => p.Key.StartsWith("TacticalBattle.Ui.", StringComparison.Ordinal)) > 80);
    }
}
