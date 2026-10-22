using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeLocaleTests
{
    [Fact]
    public async Task Hovered_point_command_uses_actual_localized_screen_label()
    {
        await using var world = await PointCombatUiFixture.Create();
        const string id = "MOD-PLAYER-COUNTERMEASURE-01";
        var row = world.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == id);
        if (!row.Opened) { world.Screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY); world.Render(); }
        var button = world.Screen.CommandsPanel.AllCommandButtons.Single(b => b.Weapon?.ModuleId == id && b.CommandTypeId == DefenseCommandTypes.Launch);
        world.Screen.OnMouseMove(button.Rect.MidX, button.Rect.MidY); world.Render();
        string expected = Localization.Get("Combat.Aim") + " [" + world.Screen.CombatInputs.CountermissileAim + "]";
        Assert.Equal(expected, world.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == id).StatusBarText);
        Assert.Null(world.Screen.LastPointCommandId); Assert.Equal(0, world.Engine.ReceivedCommandCount);
    }

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

    [Fact]
    public void Runtime_status_and_journal_use_locale_keys_without_skill_multiplier()
    {
        Assert.Equal(Localization.Get("Combat.Reason.target_out_of_range"), GameSessionScreen.DefenseReasonText("target_out_of_range"));
        Assert.Equal(Localization.Get("Combat.CommandRejected"), GameSessionScreen.DefenseReasonText("future_reason"));
        var op = new WeaponOperatorSnapshot("crew", "Alex", WeaponSkillType.CountermeasureDefense, 99, 55, 55);
        var entry = new CombatJournalEntry(1, 1000, CombatEventType.Expired, "ship", "torpedo", "pr", 0, 0,
            500, RatingBreakdown: new(op, 5), Damage: 0);
        var lines = CombatJournalPanel.Format(entry);
        Assert.Contains(lines, l => l.Contains(Localization.Get("Combat.Event.Expired")));
        Assert.DoesNotContain(lines, l => l.Contains($"{50m:0.0}%") || l.Contains(Localization.Get("Combat.NoRoll")));
        Assert.DoesNotContain(lines, l => l.Contains(Localization.Get("Combat.SkillMetadata")) || l.Contains("99"));
        Assert.DoesNotContain(lines, l => l.Contains("/50") || l.Contains("×"));
    }
}
