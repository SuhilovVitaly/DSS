using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class MapBackgroundPaletteTests
{
    [Theory]
    [InlineData("Radiation", -1, 15, 64)]
    [InlineData("Radiation", .5, 23, 64)]
    [InlineData("Radiation", 2, 31, 64)]
    [InlineData("Dust", -1, 13, 51)]
    [InlineData("Dust", .5, 19, 51)]
    [InlineData("Debris", 2, 26, 51)]
    public void Field_fill_clamps_intensity_and_raster_uses_the_fill(string kind, double intensity, byte fillAlpha, byte borderAlpha)
    {
        var (fill, border) = EnvironmentFieldRenderer.FieldColors(kind, intensity);
        Assert.Equal(fillAlpha, fill.Alpha);
        Assert.Equal(borderAlpha, border.Alpha);
        var field = new EnvironmentFieldData("field", kind, intensity, "Parent", "station", null, 0, 0, 0, 80, 0, 360, 42);
        using var bitmap = new SKBitmap(240, 240);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        new EnvironmentFieldRenderer().Draw(canvas, [new(field, 0, 0)], new(0, 0, 1), 240, 240, null);
        // Away from boundaries and decorations; the base fill must reach the canvas.
        Assert.Equal(fillAlpha, bitmap.GetPixel(127, 127).Alpha);
    }

    [Theory]
    [InlineData("Rocky", 201, 173, 125)]
    [InlineData("Icy", 159, 197, 214)]
    [InlineData("Gas", 213, 180, 151)]
    public void Planet_kind_routes_to_the_same_color_in_marker_and_planet_renderer(string kind, byte r, byte g, byte b)
    {
        Assert.Equal(new SKColor(r, g, b, 230), SolarSystemLayerRenderer.PlanetColor(kind));
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        SolarSystemLayerRenderer.DrawPlanet(canvas, new("planet", kind, 20), 0, 0, new(0, 0, 1), 100, 100);
        var pixel = bitmap.GetPixel(50, 30);
        Assert.InRange(pixel.Alpha, 220, 235);
        Assert.InRange(Math.Abs(pixel.Red - r), 0, 2);
        Assert.InRange(Math.Abs(pixel.Green - g), 0, 2);
        Assert.InRange(Math.Abs(pixel.Blue - b), 0, 2);
    }

    [Fact]
    public void Territory_inner_and_overlapping_circles_do_not_double_the_five_percent_fill()
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [new("base", 0, 0, 0, 0)], "player",
            AiMap: new(1, [], [new("a", "base", 2, 4), new("b", "base", 2, 4)]));
        using var bitmap = new SKBitmap(240, 240);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        AiMapPresentation.DrawTerritories(canvas, snapshot, snapshot.Objects, new(0, 0, 1), 240, 240, null);
        Assert.Equal(13, bitmap.GetPixel(120, 120).Alpha);
        Assert.Equal(13, bitmap.GetPixel(145, 120).Alpha);
    }
}
