using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class OverlayBoundaryTests
{
    [Fact]
    public void Eight_cards_fit_and_missing_portrait_draws_initials()
    {
        using var b = new SKBitmap(300, 720); using var c = new SKCanvas(b);
        var column = new PilotCardColumn(); var layout = new BattleLayout();
        BattleDesign.Recording = layout;
        try
        {
            PilotCard[] cards = Enumerable.Range(0, 8).Select(i => new PilotCard("unknown" + i, "Unknown", "", "missing", false,
                [new("armor", "Armor", "0/0", BattleDesign.Purple, 0), new("hull", "Hull", "1/1", BattleDesign.Cyan, 1), new("mind", "Mind", "100", BattleDesign.Green, 1)], "", false, [])).ToArray();
            column.Draw(c, cards, PilotPortraitCatalog.Default);
            Assert.Equal(697, column.Bounds.Bottom);
            Assert.Equal("unknown7", column.Hit(new(50, 650)));
            Assert.Null(column.Hit(new(50, 710)));
            Assert.Contains(layout.Texts, t => t.Text == "U");
        }
        finally { BattleDesign.Recording = null; }
    }
    [Fact]
    public async Task Wide_window_moves_only_right_column_and_uses_height_scale()
    {
        await using var s = await Ep0019Scenes.Create("1-move-no-hover");
        using var b = new SKBitmap(1600, 720); using var c = new SKCanvas(b);
        s.Screen.Render(c, 1600, 720);
        Assert.Equal(new SKRect(1360, 6, 1594, 370), s.Screen.Layout.Rects["phase"]);
        Assert.Equal(new SKRect(6, 6, 196, 88), s.Screen.Layout.Rects["pilot:Вера"]);
        Assert.Equal(1, s.Screen.CaptureFrame(1600, 720)!.UiScale);
    }
}

public sealed class PilotColumnScrollTests
{
    [Fact]
    public void Wheel_clips_hits_and_switching_selection_reveals_the_entire_card()
    {
        var column = new PilotCardColumn();
        PilotCard[] cards = Enumerable.Range(0, 8).Select(i => new PilotCard(
            $"drone{i}", "Pilot", "", "missing", i == 0, [], "", false, [])).ToArray();
        column.Arrange(cards, _ => 162);
        Assert.Equal(853, column.Layout.ContentHeight);
        Assert.Equal(145, column.Layout.MaxScroll);
        Assert.Equal(714, column.Bounds.Bottom);
        Assert.False(column.Scroll(new(202, 300), -1));
        Assert.True(column.Scroll(new(100, 300), -1));
        column.Arrange(cards, _ => 162);
        Assert.Equal(40, column.Layout.Scroll);
        Assert.Equal(new SKRect(6, 6, 196, 210), column.Rect(0));
        Assert.Null(column.Hit(new(100, 5)));
        Assert.Null(column.Hit(new(100, 212)));

        cards[0] = cards[0] with { Selected = false };
        cards[7] = cards[7] with { Selected = true };
        column.Arrange(cards, _ => 162);
        Assert.Equal(145, column.Layout.Scroll);
        Assert.Equal(new SKRect(6, 470, 196, 714), column.Rect(7));
        Assert.Equal("drone7", column.Hit(new(100, 700)));
        Assert.True(column.Rect(0).IsEmpty);
        Assert.Null(column.Hit(new(100, 715)));
    }
}
