using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class ShippedSkinTests
{
    [Fact]
    public void Contrast_manifest_and_field_image_are_present_in_real_output()
    {
        List<string> warnings = [];
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"), warnings.Add);

        Assert.Empty(warnings);
        Assert.Contains(("contrast", "Contrast"), catalog.Options);
        var contrast = catalog.GetOrClassic("contrast");
        var image = Assert.IsType<SKImage>(contrast.Image("field.background"));
        Assert.Equal(256, image.Width);
        Assert.Equal(160, image.Height);
        Assert.Equal("diamond", contrast.Shape("field.unit.drone.own.selected"));
        Assert.Equal("triangle", contrast.Shape("torpedo.projectile"));
        Assert.Equal(40, contrast.Metric("shot.trail", "tail-length", 0));
        Assert.NotEqual(SKColors.Black, contrast.Color("tabs.forecast", "fill", SKColors.Black));
    }
}
