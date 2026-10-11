using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeLocaleTests
{
    [Fact]
    public void Hovered_defense_command_exposes_localized_name_instead_of_catalog_text()
    {
        var module = new InstalledModuleSnapshot("pr", "module.countermeasure.launcher.basic", "Defense", 1,
            [DefenseCommandTypes.Fire], Commands: [new(DefenseCommandTypes.Fire, "Catalog text", "object")],
            Defense: new(false, null, DefenseState.Ready));
        var panel = new CommandsPanel(weaponCommandEnabled: (_, _) => true, weaponStatus: _ => "Ready state");
        using var bitmap = new SKBitmap(400, 2000);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, [module], 2000, "player");
        var button = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == DefenseCommandTypes.Fire);
        panel.OnMouseMove(button.Rect.MidX, button.Rect.MidY);
        panel.Render(canvas, [module], 2000, "player");
        Assert.Equal(Localization.Get("Combat.Command.defense.fire"), panel.CommandPanelRows.Single(r => r.Weapon is not null).StatusBarText);
        panel.OnMouseMove(500, 0);
        panel.Render(canvas, [module], 2000, "player");
        Assert.Equal("Ready state", panel.CommandPanelRows.Single(r => r.Weapon is not null).StatusBarText);
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
        Assert.Contains("accuracy − maneuverability", english["Combat.ChanceFormula"]);
        Assert.Contains("точность − манёвренность", russian["Combat.ChanceFormula"]);
        Assert.DoesNotContain("50", english["Combat.ChanceFormula"]);
        Assert.DoesNotContain("50", russian["Combat.ChanceFormula"]);
    }

    [Fact]
    public void Runtime_status_and_journal_use_locale_keys_without_skill_multiplier()
    {
        Assert.Equal(Localization.Get("Combat.Reason.target_out_of_range"), GameSessionScreen.DefenseReasonText("target_out_of_range"));
        Assert.Equal(Localization.Get("Combat.CommandRejected"), GameSessionScreen.DefenseReasonText("future_reason"));
        string status = GameSessionScreen.DefenseStatusText(new(true, null, DefenseState.Reloading, ReloadDueMotionTimeMs: 6000), 1000);
        Assert.Contains(Localization.Get("Combat.Reloading"), status);
        Assert.Contains(Localization.Get("Combat.AutoOn"), status);
        Assert.Contains($"{5m:0.0}", status);
        var op = new WeaponOperatorSnapshot("crew", "Alex", WeaponSkillType.CountermeasureDefense, 99, 55, 55);
        var entry = new CombatJournalEntry(1, 1000, CombatEventType.Expired, "ship", "torpedo", "pr", 0, 0,
            500, RatingBreakdown: new(op, 5), Damage: 0);
        var lines = CombatJournalPanel.Format(entry);
        Assert.Contains(lines, l => l.Contains(Localization.Get("Combat.Event.Expired")));
        Assert.Contains(lines, l => l.Contains($"{50m:0.0}%") && l.Contains(Localization.Get("Combat.NoRoll")));
        Assert.Contains(lines, l => l.Contains(Localization.Get("Combat.SkillMetadata")) && l.Contains("99"));
        Assert.DoesNotContain(lines, l => l.Contains("/50") || l.Contains("×"));
    }
}
