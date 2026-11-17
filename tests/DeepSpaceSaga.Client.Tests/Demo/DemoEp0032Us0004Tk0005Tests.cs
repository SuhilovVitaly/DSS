using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0032Us0004Tk0005Tests
{
    [Fact]
    public void LocaleKeysAndPlaceholdersMatch()
    {
        const string root = "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/Data/Locale/";
        var ru = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(root + "Russian.json"))!;
        var en = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(root + "English.json"))!;
        var keys = en.Keys.Where(k => k.StartsWith("Demo.Start.")).Order().ToArray();
        Assert.NotEmpty(keys);
        Assert.Equal(keys, ru.Keys.Where(k => k.StartsWith("Demo.Start.")).Order());
        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(ru[key]));
            Assert.Equal(System.Text.RegularExpressions.Regex.Matches(en[key], @"\{\d+\}").Select(m => m.Value),
                System.Text.RegularExpressions.Regex.Matches(ru[key], @"\{\d+\}").Select(m => m.Value));
        }
    }
}
