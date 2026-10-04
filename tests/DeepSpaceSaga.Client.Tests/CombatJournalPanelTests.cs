using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class CombatJournalPanelTests
{
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
}
