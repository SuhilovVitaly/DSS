using System.Text.Json;
using System.Text.RegularExpressions;

namespace DeepSpaceSaga.Client.Tests;

public sealed class WorldLocaleParityTests
{
    [Fact]
    public void DeployedAndSourceLocaleKeysAndPlaceholdersMatchWithoutRetiredCombatText()
    {
        string source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client/Data/Locale"));
        Dictionary<string, string> Read(string language)
        {
            var disk = File.ReadAllText(Path.Combine(source, language + ".json"));
            Assert.Equal(disk, File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data/Locale", language + ".json")));
            using var doc = JsonDocument.Parse(disk);
            return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);
        }

        var en = Read("English");
        var ru = Read("Russian");
        Assert.Equal(en.Keys.Order(StringComparer.Ordinal), ru.Keys.Order(StringComparer.Ordinal));
        Assert.DoesNotContain(en.Keys, key => key.StartsWith("Combat.", StringComparison.Ordinal) || key.StartsWith("CombatReports.", StringComparison.Ordinal));
        Assert.Contains("World.Panel.Engine", en.Keys);
        Assert.Contains("Session.LoadIncompatible", en.Keys);
        Assert.Contains("TacticalBattle.BattleTitle", en.Keys);
        foreach (var (key, text) in en)
        {
            Assert.Equal(Placeholders(text), Placeholders(ru[key]));
            Assert.False(string.IsNullOrWhiteSpace(ru[key]), key);
        }

        static string[] Placeholders(string value) =>
            Regex.Matches(value, @"\{[^}]+\}").Select(m => m.Value).Order(StringComparer.Ordinal).ToArray();
    }
}
