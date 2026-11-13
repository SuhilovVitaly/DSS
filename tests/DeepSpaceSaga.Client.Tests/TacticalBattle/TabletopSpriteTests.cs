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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Torpedo_sprite_rotates_from_horizontal_to_vertical(bool own)
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(96, 96);
        using var canvas = new SKCanvas(bitmap);
        var horizontal = Footprint(0);
        var vertical = Footprint(90);
        Assert.True(horizontal.Width > horizontal.Height);
        Assert.Equal((horizontal.Height, horizontal.Width), vertical);
        var distant = Footprint(0, 1f / 3);
        Assert.InRange((distant.Width + 1f) / (horizontal.Width + 1f), .28f, .38f);

        (int Width, int Height) Footprint(float angle, float scale = 1)
        {
            canvas.Clear(SKColors.Transparent);
            Assert.True(TacticalShipSprites.DrawTorpedo(canvas, new(48, 48), angle, own, scale));
            var visible = from y in Enumerable.Range(0, bitmap.Height)
                          from x in Enumerable.Range(0, bitmap.Width)
                          where bitmap.GetPixel(x, y).Alpha > 0
                          select (X: x, Y: y);
            var points = visible.ToArray();
            return (points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y));
        }
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
