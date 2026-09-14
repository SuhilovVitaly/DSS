using DeepSpaceSaga.Client.UI;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GridRendererTests
{
    [Fact]
    public void Maximum_zoom_has_200_pixel_cells_and_1000_pixel_parents()
    {
        var settings = new TacticalMapSettings();
        var levels = GridRenderer.GetEligibleLevels(settings.MaximumPpu, settings);
        Assert.Equal(200, levels[0] * settings.MaximumPpu);
        Assert.Equal(1000, levels[1] * settings.MaximumPpu);
        using var bitmap = RenderGrid(1);
        var lines = Enumerable.Range(0, 1001).Where(x => bitmap.GetPixel(x, 123).Blue > 0).ToArray();
        Assert.Equal(new[] { 0, 200, 400, 600, 800, 1000 }, lines);
        Assert.True(bitmap.GetPixel(1000, 123).Blue > bitmap.GetPixel(200, 123).Blue);
    }

    [Theory]
    [InlineData(1)] [InlineData(.15)] [InlineData(.1)] [InlineData(.02)]
    [InlineData(.001)] [InlineData(.00001)] [InlineData(1e-12)] [InlineData(1e-15)]
    public void Levels_always_share_five_by_five_world_boundaries(double ppu)
    {
        var levels = GridRenderer.GetEligibleLevels(ppu);
        Assert.InRange(levels.Count, 2, 6);
        for (int i = 1; i < levels.Count; i++)
            Assert.Equal(levels[i - 1] * 5, levels[i]);
        double baseExponent = Math.Log(levels[0] / 200, 5);
        Assert.Equal(Math.Round(baseExponent), baseExponent, 10);
    }

    [Fact]
    public void Base_cell_tracks_configured_maximum_zoom()
    {
        var settings = (new TacticalMapSettings { MetersPerPixel = [50, 1000, 100000, 1000000, 10000000] }).Validate();
        Assert.Equal(200, GridRenderer.GetEligibleLevels(settings.MaximumPpu, settings)[0] * settings.MaximumPpu);
    }

    [Fact]
    public void Fine_lines_disappear_but_their_parent_remains()
    {
        using var bitmap = RenderGrid(.1);
        Assert.Equal(SKColors.Black, bitmap.GetPixel(20, 13));
        Assert.True(bitmap.GetPixel(100, 13).Blue > 0);
        Assert.True(bitmap.GetPixel(500, 13).Blue > bitmap.GetPixel(100, 13).Blue);
    }

    [Fact]
    public void Negative_world_coordinates_keep_the_same_grid_when_panning()
    {
        using var bitmap = RenderGrid(1, -350);
        Assert.True(bitmap.GetPixel(150, 123).Blue > 0); // World X = -200.
        Assert.True(bitmap.GetPixel(350, 123).Blue > bitmap.GetPixel(150, 123).Blue); // World X = 0.
        Assert.Equal(SKColors.Black, bitmap.GetPixel(200, 123));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(5)] [InlineData(12)]
    public void Dropping_a_level_does_not_flash_or_recolor_parent_boundaries(int level)
    {
        double thresholdPpu = .1 / Math.Pow(5, level);
        using var before = RenderGrid(thresholdPpu * (1 + 1e-8));
        using var after = RenderGrid(thresholdPpu * (1 - 1e-8));
        for (int x = 0; x < before.Width; x++)
            Assert.InRange(Math.Abs(before.GetPixel(x, 13).Blue - after.GetPixel(x, 13).Blue), 0, 1);
    }

    [Fact]
    public void Color_darkens_smoothly_with_density_and_saturates_for_ancestors()
    {
        var settings = new TacticalMapSettings();
        byte previous = 0;
        for (int tenths = 200; tenths <= 20000; tenths++)
        {
            var color = GridRenderer.LevelColor(tenths / 10.0, 1, settings);
            Assert.InRange(color.Blue - previous, 0, 1);
            Assert.True(color.Blue >= color.Green && color.Green >= color.Red);
            previous = color.Blue;
        }
        Assert.Equal(GridRenderer.LevelColor(1000, 1, settings), GridRenderer.LevelColor(1e15, 1, settings));
    }

    private static SKBitmap RenderGrid(double ppu, double worldLeft = 0)
    {
        const int size = 1200;
        var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);
        // Half-pixel placement lets raster assertions sample the exact center of 1px strokes.
        var camera = new CameraState(worldLeft + (size / 2.0 - .5) / ppu, (size / 2.0 - .5) / ppu, ppu);
        new GridRenderer().Draw(canvas, camera, size, size);
        return bitmap;
    }

    // --- First world line computation ---

    [Fact]
    public void ComputeFirstWorldLine_for_positive_bounds()
    {
        double first = GridRenderer.ComputeFirstWorldLine(worldBound: 10050, worldStep: 100);

        Assert.Equal(10000, first);
    }

    [Fact]
    public void ComputeFirstWorldLine_when_bound_is_already_multiple()
    {
        double first = GridRenderer.ComputeFirstWorldLine(worldBound: 10000, worldStep: 100);

        Assert.Equal(10000, first);
    }

    [Fact]
    public void ComputeFirstWorldLine_for_negative_bounds()
    {
        double first = GridRenderer.ComputeFirstWorldLine(worldBound: -150, worldStep: 100);

        // floor(-150 / 100) = floor(-1.5) = -2; -2 * 100 = -200
        Assert.Equal(-200, first);
    }

    [Fact]
    public void ComputeFirstWorldLine_for_negative_bound_near_zero()
    {
        double first = GridRenderer.ComputeFirstWorldLine(worldBound: -50, worldStep: 100);

        // floor(-50 / 100) = floor(-0.5) = -1; -1 * 100 = -100
        Assert.Equal(-100, first);
    }

    [Fact]
    public void ComputeFirstWorldLine_at_zero()
    {
        double first = GridRenderer.ComputeFirstWorldLine(worldBound: 0, worldStep: 100);

        Assert.Equal(0, first);
    }

    [Fact]
    public void ComputeFirstWorldLine_with_step_10()
    {
        double first = GridRenderer.ComputeFirstWorldLine(worldBound: 10007, worldStep: 10);

        Assert.Equal(10000, first);
    }

    [Fact]
    public void ComputeFirstWorldLine_with_step_1000()
    {
        double first = GridRenderer.ComputeFirstWorldLine(worldBound: 9500, worldStep: 1000);

        Assert.Equal(9000, first);
    }

    // --- Grid alignment ---

    [Fact]
    public void Grid_alignment_all_coordinates_are_multiples_of_step()
    {
        const int worldStep = 100;
        double firstX = GridRenderer.ComputeFirstWorldLine(worldBound: 50, worldStep: worldStep);

        // firstX should be 0 (floor(50/100)*100)
        Assert.Equal(0, firstX);
        Assert.True(firstX % worldStep == 0);

        // Generate a few subsequent coordinates
        for (int i = 0; i < 10; i++)
        {
            double coord = firstX + i * worldStep;
            Assert.True(coord % worldStep == 0, $"Coordinate {coord} is not a multiple of {worldStep}");
        }
    }

    [Fact]
    public void Negative_coordinates_grid_aligns_correctly()
    {
        // Viewport that includes negative world coords
        const int worldStep = 100;

        double firstX = GridRenderer.ComputeFirstWorldLine(worldBound: -350, worldStep: worldStep);
        Assert.Equal(-400, firstX);

        // Generate coordinates crossing zero
        var coords = new List<double>();
        for (double wx = firstX; wx <= 200; wx += worldStep)
            coords.Add(wx);

        Assert.Contains(-400, coords);
        Assert.Contains(-300, coords);
        Assert.Contains(-200, coords);
        Assert.Contains(-100, coords);
        Assert.Contains(0, coords);
        Assert.Contains(100, coords);
        Assert.Contains(200, coords);

        // All must be multiples
        foreach (double c in coords)
            Assert.True(c % worldStep == 0, $"Coordinate {c} is not a multiple of {worldStep}");
    }
}
