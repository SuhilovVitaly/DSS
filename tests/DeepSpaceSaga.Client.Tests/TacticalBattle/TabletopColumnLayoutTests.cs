using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopColumnLayoutTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Selected_panel_moves_only_following_cards(int selected)
    {
        var layout = new PilotColumnLayout([40, 40, 40, 40], 0, selected, true, selected, 91);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(new SKRect(6, 6 + 45 * i + (i > selected ? 95 : 0), 156, 46 + 45 * i + (i > selected ? 95 : 0)), layout.Cards[i]);
            Assert.Equal(layout.Cards[i], layout.Visible(i));
        }
        var card = layout.Cards[selected];
        Assert.Equal(SKRect.Create(51, card.Bottom + 3, 150, 91), layout.Panel);
        Assert.Equal(layout.Panel, layout.VisiblePanel);
        Assert.Equal(0, layout.MaxScroll);
    }

    [Fact]
    public void Eight_cards_fit_and_long_panel_scrolls_to_reveal_the_whole_selected_pair()
    {
        float[] heights = Enumerable.Repeat(40f, 8).ToArray();
        var compact = new PilotColumnLayout(heights, 0, slim: true);
        Assert.Equal(355, compact.ContentHeight);
        Assert.Equal(361, PilotColumnLayout.CompactBottom(8, true));
        var expanded = new PilotColumnLayout(heights, 0, 7, true, 7, 400);
        Assert.True(expanded.Scroll > 0);
        Assert.Equal(expanded.MaxScroll, expanded.Scroll);
        Assert.Equal(714, expanded.Panel.Bottom);
        Assert.True(expanded.Cards[7].Top >= PilotColumnLayout.Viewport.Top);
        Assert.Equal(expanded.Panel, expanded.VisiblePanel);
        Assert.Equal(40, expanded.Cards[7].Height);
    }

    [Fact]
    public async Task Column_uses_skin_flag_and_leaves_classic_geometry_intact()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        var cards = PilotCardModel.Project(scene.Frame);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var column = new PilotCardColumn();
        using (TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop")))
        {
            column.Arrange(scene.Frame, cards);
            Assert.Equal(new SKRect(6, 6, 156, 46), column.Layout.Cards[0]);
            Assert.Equal(new SKRect(51, 49, 201, 140), column.Layout.Panel);
            Assert.Equal(146, column.Layout.Cards[1].Top);
        }
        using (TacticalBattleSkinRenderScope.Enter(TacticalBattleSkin.Classic))
        {
            column.Arrange(scene.Frame, cards);
            Assert.False(column.Layout.Slim);
            Assert.True(column.Layout.Panel.IsEmpty);
            Assert.Equal(190, column.Layout.Cards[0].Width);
            Assert.True(column.Layout.Cards[0].Height > PilotColumnLayout.BaseHeight);
        }
    }
}
