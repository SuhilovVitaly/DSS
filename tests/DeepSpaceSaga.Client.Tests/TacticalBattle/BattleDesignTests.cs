using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class BattleDesignTests
{
    [Fact]
    public void Tokens_match_design_spec()
    { Assert.Equal(new SKColor(34, 227, 240), BattleDesign.Cyan); Assert.Equal(new SKColor(14, 27, 31), BattleDesign.Panel); Assert.Equal(new BattleViewport(1.5f, 1280), BattleViewport.From(1920, 1080)); }
    [Fact]
    public void Number_formatting_matches_design_spec()
    { Assert.Equal("56%", BattleDesign.Pct(.555)); Assert.Equal("−24,3°", BattleDesign.Deg(-24.26)); Assert.Equal("3 выстрела", BattleDesign.Plural(3, "выстрел", "выстрела", "выстрелов")); Assert.Equal("Марты", BattleDesign.Genitive("Марта")); }
    [Fact]
    public void Primitives_draw_reference_pixels()
    {
        using var bitmap = new SKBitmap(300, 150); using var c = new SKCanvas(bitmap);
        BattleDesign.PanelBox(c, new(10, 10, 110, 110), true); BattleDesign.BigButton(c, new(150, 10, 280, 110), "OK");
        Assert.Equal(BattleDesign.PanelSel, bitmap.GetPixel(20, 20)); Assert.Equal(BattleDesign.Yellow, bitmap.GetPixel(160, 20));
    }
}
