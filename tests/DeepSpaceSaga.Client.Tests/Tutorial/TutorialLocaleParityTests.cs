using System.Text;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>EP-0028-US-0002-TK-0001: every tutorial string exists in Russian and English with compatible formats (D-06).</summary>
public sealed class TutorialLocaleParityTests
{
    private static readonly string[] Prefixes = ["Tutorial.", "Settings.TutorialHints"];

    private static readonly string[] RequiredUiKeys =
    [
        "Tutorial.Panel.Title", "Tutorial.Panel.Acknowledge", "Tutorial.Panel.HintsOff", "Tutorial.Panel.HintsOn",
        "Tutorial.Panel.JournalHint", "Tutorial.Journal.Title", "Tutorial.Journal.Empty", "Tutorial.Journal.Close",
        "Settings.TutorialHints", "Settings.TutorialHints.On", "Settings.TutorialHints.Off", "Settings.TutorialHints.SaveFailed",
    ];

    private static Dictionary<string, string> TutorialKeys(string language)
    {
        var all = Localization.LoadLocaleFile(language);
        Assert.NotNull(all);
        return all.Where(p => Prefixes.Any(prefix => p.Key.StartsWith(prefix, StringComparison.Ordinal)))
            .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
    }

    [Fact]
    public void RequiredUiKeysExist()
    {
        foreach (string language in new[] { "Russian", "English" })
        {
            var keys = TutorialKeys(language);
            foreach (string key in RequiredUiKeys)
                Assert.True(keys.ContainsKey(key), $"{language}: missing {key}");
        }
    }

    [Fact]
    public void TutorialKeysMatchInBothLocales()
    {
        var ru = TutorialKeys("Russian");
        var en = TutorialKeys("English");
        Assert.NotEmpty(ru);
        Assert.Equal(ru.Keys.Order(StringComparer.Ordinal), en.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TutorialValuesAreNonBlankLocalizedAndFormatCompatible()
    {
        var ru = TutorialKeys("Russian");
        var en = TutorialKeys("English");
        foreach (string key in ru.Keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(ru[key]), "Russian blank: " + key);
            Assert.True(en.TryGetValue(key, out var english), "English missing: " + key);
            Assert.False(string.IsNullOrWhiteSpace(english), "English blank: " + key);
            Assert.NotEqual(key, ru[key]);
            Assert.NotEqual(key, english);
            Assert.Equal(CompositeFormat.Parse(ru[key]).MinimumArgumentCount, CompositeFormat.Parse(english).MinimumArgumentCount);
        }
    }
}
