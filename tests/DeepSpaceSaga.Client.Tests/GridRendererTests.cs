using DeepSpaceSaga.Client.UI;

namespace DeepSpaceSaga.Client.Tests;

public class GridRendererTests
{
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
