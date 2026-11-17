using System.Text.Json;
using System.Text.RegularExpressions;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0005Tk0004Tests
{
    [Fact]
    public void StoryKeysExistInBothLocales()
    {
        var (en, ru) = Locales();
        var keys = en.Keys.Where(k => k.StartsWith("Demo.Upkeep.")).Order().ToArray();
        Assert.NotEmpty(keys);
        Assert.Equal(keys, ru.Keys.Where(k => k.StartsWith("Demo.Upkeep.")).Order());
        Assert.All(keys, k => Assert.False(string.IsNullOrWhiteSpace(ru[k])));
    }
    [Fact]
    public void FormatArgumentsMatch()
    {
        var (en, ru) = Locales();
        foreach (var key in en.Keys.Where(k => k.StartsWith("Demo.Upkeep.")))
            Assert.Equal(Regex.Matches(en[key], @"\{\d+\}").Select(m => m.Value),
                Regex.Matches(ru[key], @"\{\d+\}").Select(m => m.Value));
    }
    [Fact]
    public void NoRawReasonCodeLeaks()
    {
        var (en, ru) = Locales();
        foreach (var key in en.Keys.Where(k => k.StartsWith("Demo.Upkeep.service_")))
        { Assert.DoesNotContain("service_", en[key]); Assert.DoesNotContain("service_", ru[key]); }
    }
    private static (Dictionary<string, string>, Dictionary<string, string>) Locales(
        [System.Runtime.CompilerServices.CallerFilePath] string file = "")
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../src/DeepSpaceSaga.Client/Data/Locale"));
        return (JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(root, "English.json")))!,
            JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(root, "Russian.json")))!);
    }
}
