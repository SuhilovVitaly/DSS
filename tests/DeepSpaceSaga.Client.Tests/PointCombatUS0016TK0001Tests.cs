using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0016TK0001Tests
{
    private static string ClientRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Fact]
    public void Combat_locale_key_sets_match_and_have_no_fallback_literals()
    {
        var en = Localization.LoadLocaleFile("English")!; var ru = Localization.LoadLocaleFile("Russian")!;
        Assert.Equal(en.Keys.Where(k => k.StartsWith("Combat.", StringComparison.Ordinal)).Order(), ru.Keys.Where(k => k.StartsWith("Combat.", StringComparison.Ordinal)).Order());
        foreach (string language in new[] { "English", "Russian" })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(ClientRoot, "Data", "Locale", language + ".json")));
            var properties = document.RootElement.EnumerateObject().ToArray();
            Assert.Equal(properties.Length, properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count());
        }
        foreach (var file in Directory.EnumerateFiles(Path.Combine(ClientRoot, "UI", "Screens", "GameSession"), "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(file), "CombatText\\(\"([^\"\\n]+)\"\\s*,"))
            {
                string key = "Combat." + match.Groups[1].Value;
                Assert.True(en.TryGetValue(key, out var english) && !string.IsNullOrWhiteSpace(english), file + ": " + key);
                Assert.True(ru.TryGetValue(key, out var russian) && !string.IsNullOrWhiteSpace(russian), file + ": " + key);
            }
        }
        Assert.Equal(Localization.Get("Combat.Unknown"), Localization.Combat("future-ui-key"));
        Assert.Equal(Localization.Get("Combat.CommandRejected"), Localization.Combat("Reason.future-code"));
        Assert.Equal(Localization.Get("Combat.Forecast.Unknown"), Localization.Combat("Forecast.future-status"));
        Assert.Equal("custom-profile-id", Localization.Combat("Profile.custom-profile-id"));
        foreach (var status in Enum.GetValues<CombatForecastStatus>())
        {
            Assert.True(en.ContainsKey("Combat.Forecast." + status)); Assert.True(ru.ContainsKey("Combat.Forecast." + status));
        }
        foreach (var reason in new[] { "engine_disabled", "invalid_evade_side", "invalid_turn_payload", "unknown_threat", "combat_speed_locked" })
        { Assert.True(en.ContainsKey("Combat.Reason." + reason)); Assert.True(ru.ContainsKey("Combat.Reason." + reason)); }
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Generic_errors_and_profiles_use_the_requested_dictionary(string language)
    {
        var locale = Localization.LoadLocaleFile(language)!;
        Assert.Equal(locale["Combat.CommandRejected"], Localization.Combat("Reason.future", key => locale[key]));
        Assert.Equal("custom-profile", Localization.Combat("Profile.custom-profile", key => locale[key]));
    }

    [Fact]
    public void Locale_scope_restores_current_language_and_preserves_custom_data_names()
    {
        string before = Localization.Get("Combat.Aim");
        using (Localization.UseLocaleForTests("English"))
        {
            Assert.Equal("Aim", Localization.Combat("Aim"));
            using (Localization.UseLocaleForTests("Russian")) Assert.Equal("Прицел", Localization.Combat("Aim"));
            Assert.Equal("Aim", Localization.Combat("Aim"));
            Assert.Equal("User custom module name", Localization.CombatModuleName("custom.module", "User custom module name"));
        }
        Assert.Equal(before, Localization.Get("Combat.Aim"));
    }
}
