using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeLocaleTests
{

    [Fact]
    public void Both_locales_cover_phase_three_keys_in_source_and_output()
    {
        var english = Localization.LoadLocaleFile("English")!;
        var russian = Localization.LoadLocaleFile("Russian")!;
        var keys = english.Keys.Where(k => k.StartsWith("Combat.", StringComparison.Ordinal)).Order().ToArray();
        Assert.True(keys.Length >= 60);
        Assert.Equal(keys, russian.Keys.Where(k => k.StartsWith("Combat.", StringComparison.Ordinal)).Order().ToArray());
        foreach (var language in new[] { "English", "Russian" })
        {
            var output = Localization.LoadLocaleFile(language)!;
            string sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "DeepSpaceSaga.Client", "Data", "Locale", language + ".json"));
            using var source = JsonDocument.Parse(File.ReadAllText(sourcePath));
            Assert.Equal(source.RootElement.EnumerateObject().Count(), source.RootElement.EnumerateObject().Select(p => p.Name).Distinct().Count());
            Assert.All(keys, key =>
            {
                Assert.False(string.IsNullOrWhiteSpace(output[key]));
                Assert.Equal(source.RootElement.GetProperty(key).GetString(), output[key]);
            });
            foreach (var type in Enum.GetValues<CombatEventType>()) Assert.Contains("Combat.Event." + type, keys);
            foreach (var phase in Enum.GetValues<HostileCombatPhase>()) Assert.Contains("Combat.Phase." + phase, keys);
            foreach (string reason in new[] { "unknown_object", "unknown_module", "module_unavailable", "busy", "no_weapon_operator",
                "missing_target", "unknown_target", "target_not_hostile", "target_out_of_range", "invalid_time", "projectile_id_exhausted" })
                Assert.Contains("Combat.Reason." + reason, keys);
        }
    }
}
