using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("Battle localization")]
public sealed class TabletopSpriteTests
{
    [Theory]
    [InlineData(0, 8)]
    [InlineData(90, 16)]
    [InlineData(180, 24)]
    [InlineData(270, 0)]
    [InlineData(-90, 0)]
    [InlineData(371.25, 9)]
    public void Heading_selects_expected_frame(double heading, int expected) => Assert.Equal(expected, TacticalShipSprites.Frame(heading));

    [Theory]
    [InlineData(1f, 1)]
    [InlineData(1.5f, 2)]
    [InlineData(2f, 3)]
    public void Integer_pixels_and_snapped_origin(float scale, int factor)
    {
        Assert.Equal(factor, TacticalShipSprites.Factor(scale));
        var bounds = TacticalShipSprites.Bounds(new(123.4f, 101.7f), 64, scale, factor);
        Assert.Equal(64 * factor, bounds.Width * scale, .001f);
        Assert.Equal(MathF.Round(bounds.Left * scale), bounds.Left * scale, .001f);
        Assert.Equal(MathF.Round(bounds.Top * scale), bounds.Top * scale, .001f);
    }

    [Fact]
    public async Task Real_sheet_renders_with_integer_pixels_and_classic_returns_to_silhouette()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        var unit = scene.Frame.Battle.Units.First(u => u.Kind == TacticalUnitKind.Drone);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var bitmap = new SKBitmap(256, 256);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(1.5f);
        using (TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop")))
        {
            Assert.True(TacticalShipSprites.Draw(canvas, unit, new(80, 80), true, 0, shadow: false));
            var bounds = TacticalShipSprites.Bounds(new(80, 80), 64, 1.5f, 2);
            int left = (int)MathF.Round(bounds.Left * 1.5f), top = (int)MathF.Round(bounds.Top * 1.5f);
            int opaque = 0;
            for (int y = top; y < top + 128; y += 2)
                for (int x = left; x < left + 128; x += 2)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    Assert.Equal(pixel, bitmap.GetPixel(x + 1, y));
                    Assert.Equal(pixel, bitmap.GetPixel(x, y + 1));
                    Assert.Equal(pixel, bitmap.GetPixel(x + 1, y + 1));
                    if (pixel.Alpha > 0) opaque++;
                }
            Assert.True(opaque > 100);
        }
        using (TacticalBattleSkinRenderScope.Enter(TacticalBattleSkin.Classic))
            Assert.False(TacticalShipSprites.Draw(canvas, unit, new(80, 80), true, 0));
    }
}
