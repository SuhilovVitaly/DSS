using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class CombatJournalPanelTests
{
    [Theory]
    [InlineData(640)]
    [InlineData(1280)]
    [InlineData(1920)]
    public void Collapsing_changes_only_journal_height(int width)
    {
        var panel = new CombatJournalPanel();
        using var bitmap = new SKBitmap(width, 720);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, width, 720, []);
        var collapsed = panel.Bounds;
        panel.Click(collapsed.MidX, collapsed.MidY);
        panel.Render(canvas, width, 720, []);
        Assert.Equal(collapsed.Width, panel.Bounds.Width);
        Assert.Equal(collapsed.Left, panel.Bounds.Left);
        Assert.True(panel.Bounds.Height > collapsed.Height);
    }

    [Fact]
    public void Single_wrapped_entry_can_scroll_to_its_hidden_tail()
    {
        var entry = new CombatJournalEntry(1, 1234, CombatEventType.Hit,
            new string('W', 300), "target", "projectile", 0, 0, Damage: 150);
        var panel = new CombatJournalPanel();
        using var bitmap = new SKBitmap(640, 360);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, 640, 360, [entry]);
        panel.Click(panel.Bounds.MidX, 60);
        canvas.Clear();
        panel.Render(canvas, 640, 360, [entry]);
        byte[] before = bitmap.Bytes;
        for (int i = 0; i < 100; i++) panel.Scroll(panel.Bounds.MidX, 100, 1);
        canvas.Clear();
        panel.Render(canvas, 640, 360, [entry]);
        Assert.False(before.SequenceEqual(bitmap.Bytes), "Scrolling must reveal the tail even when there is only one event.");
        Assert.Equal(entry, Assert.Single(panel.Entries));
    }

    [Fact]
    public void Collapse_preserves_entries_and_formats_same_chance_roll()
    {
        var entry = new CombatJournalEntry(1, 1234, CombatEventType.Miss, "ship", "torpedo", "pr", 0, 0, 320, 801);
        var panel = new CombatJournalPanel();
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, 1280, 720, [entry]);
        Assert.False(panel.Expanded);
        Assert.False(panel.Contains(640, 200));
        Assert.True(panel.Click(640, 60));
        panel.Render(canvas, 1280, 720, [entry]);
        Assert.True(panel.Contains(640, 200));
        Assert.True(panel.Scroll(640, 200, 1));
        panel.Click(640, 60);
        panel.Render(canvas, 1280, 720, [entry]);
        Assert.Equal(entry, Assert.Single(panel.Entries));
        Assert.Contains(CombatJournalPanel.Format(entry), s => s.Contains("32.0%") && s.Contains("801/1000"));
    }

    [Fact]
    public void Torpedo_and_interception_rolls_show_the_new_attack_defense_breakdown()
    {
        var torpedo = new CombatJournalEntry(1, 1000, CombatEventType.TorpedoHitRoll,
            "ship-a", "ship-b", "torpedo", 0, 0, ChanceTenths: 480, Roll: 479,
            Attack: 72, Defense: 74);
        var hitLines = CombatJournalPanel.Format(torpedo);
        Assert.Contains(hitLines, s => s.Contains("72") && s.Contains("74"));
        Assert.Contains(hitLines, s => s.Contains("48.0%") && s.Contains("479/1000"));

        var op = new WeaponOperatorSnapshot("crew", "Operator", WeaponSkillType.CountermeasureDefense, 20, 58, 58);
        var intercept = torpedo with
        {
            Type = CombatEventType.Intercept,
            RatingBreakdown = new(op, 50),
            Accuracy = 58,
            TargetManeuverability = 50,
            ChanceTenths = 580,
            Roll = 501,
            Attack = null,
            Defense = null
        };
        var interceptLines = CombatJournalPanel.Format(intercept);
        Assert.Contains(interceptLines, s => s.Contains("58") && s.Contains("50") && !s.Contains('−'));
        Assert.Contains(interceptLines, s => s.Contains("base + defense") || s.Contains("база + защита"));
        Assert.Contains(interceptLines, s => s.Contains("58.0%") && s.Contains("501/1000"));
    }

    [Fact]
    public void Module_damage_destruction_and_ship_death_keep_ids_values_and_cause()
    {
        var hit = new CombatJournalEntry(1, 1000, CombatEventType.ModuleHit,
            "ship-a", "ship-b", "torpedo", 0, 0, HitModuleId: "engine-1",
            ArmorBefore: 20, ArmorAfter: 0, StructureBefore: 40, StructureAfter: 25);
        var lines = CombatJournalPanel.Format(hit);
        Assert.Contains(lines, s => s.Contains("engine-1"));
        Assert.Contains(lines, s => s.Contains("20 → 0"));
        Assert.Contains(lines, s => s.Contains("40 → 25"));
        Assert.Contains(CombatJournalPanel.Format(hit with { Type = CombatEventType.ModuleDestroyed }),
            s => s.Contains("engine-1"));
        Assert.Contains(CombatJournalPanel.Format(hit with
        {
            Type = CombatEventType.ShipDestroyed,
            DestroyReason = "critical:engine-1"
        }), s => s.Contains("engine-1"));
        Assert.Contains(CombatJournalPanel.Format(hit with
        {
            Type = CombatEventType.ShipDestroyed,
            DestroyReason = "threshold:3/9"
        }), s => s.Contains("3/9"));
    }

    [Fact]
    public void Skipped_burst_and_overheat_breakdown_show_reason_and_mode()
    {
        var entry = new CombatJournalEntry(1, 1000, CombatEventType.BurstShotSkipped,
            "ship-a", "torpedo", "countermeasure", 0, 0,
            Result: "target_or_module_unavailable", LauncherModuleId: "pd-1");
        var skipLines = CombatJournalPanel.Format(entry);
        Assert.Contains(skipLines, s => s.Contains("pd-1"));
        Assert.Contains(skipLines, s => s.Contains("Target or launcher unavailable") ||
            s.Contains("Цель или пусковая установка недоступна"));
        var breakdownLines = CombatJournalPanel.Format(entry with
        {
            Type = CombatEventType.LauncherBreakdown,
            Result = "overheat",
            ChanceTenths = 300,
            Roll = 99
        });
        Assert.Contains(breakdownLines, s => s.Contains("30.0%") && s.Contains("99/1000"));
        Assert.Contains(breakdownLines, s => s.Contains("Overheat") || s.Contains("Перегрев"));
    }
}
