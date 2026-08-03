using DeepSpaceSaga.Client.UI;

namespace DeepSpaceSaga.Client.Tests;

public class GridRendererTests
{
    // --- Detail mode selection ---

    [Fact]
    public void Detail_mode_high_requires_above_5_0()
    {
        // Exactly 5.0 is Normal, not High
        var candidates = GridRenderer.GetDetailModeCandidates(pixelsPerWorldUnit: 5.0);
        Assert.Equal(new[] { 10, 100, 1000, 10000 }, candidates);
    }

    [Fact]
    public void Detail_mode_high_at_scale_5_01()
    {
        // 5.01 is strictly above 5.0 → High
        var candidates = GridRenderer.GetDetailModeCandidates(pixelsPerWorldUnit: 5.01);
        Assert.Equal(new[] { 1, 5, 10, 100, 1000, 10000 }, candidates);
    }

    [Fact]
    public void Detail_mode_high_at_scale_10()
    {
        var candidates = GridRenderer.GetDetailModeCandidates(pixelsPerWorldUnit: 10.0);
        Assert.Contains(1, candidates);
        Assert.Contains(5, candidates);
    }

    [Fact]
    public void Detail_mode_normal_excludes_fine_levels()
    {
        var candidates = GridRenderer.GetDetailModeCandidates(pixelsPerWorldUnit: 1.0);
        Assert.Equal(new[] { 10, 100, 1000, 10000 }, candidates);
    }

    [Fact]
    public void Detail_mode_normal_at_upper_bound()
    {
        // Exactly 5.0 is Normal
        var candidates = GridRenderer.GetDetailModeCandidates(pixelsPerWorldUnit: 5.0);
        Assert.Equal(new[] { 10, 100, 1000, 10000 }, candidates);
    }

    [Fact]
    public void Detail_mode_normal_at_lower_bound()
    {
        // Exactly 0.1 is Normal
        var candidates = GridRenderer.GetDetailModeCandidates(pixelsPerWorldUnit: 0.1);
        Assert.Equal(new[] { 10, 100, 1000, 10000 }, candidates);
    }

    [Fact]
    public void Detail_mode_low_requires_below_0_1()
    {
        // 0.099 is strictly below 0.1 → Low
        var candidates = GridRenderer.GetDetailModeCandidates(pixelsPerWorldUnit: 0.099);
        Assert.Equal(new[] { 1000, 10000 }, candidates);
    }

    [Fact]
    public void Detail_mode_low_at_very_small_scale()
    {
        var candidates = GridRenderer.GetDetailModeCandidates(pixelsPerWorldUnit: 0.01);
        Assert.Equal(new[] { 1000, 10000 }, candidates);
    }

    // --- Eligibility filtering (detail mode + min pixel spacing) ---

    [Fact]
    public void GetEligibleLevels_at_high_detail_filters_by_pixel_spacing()
    {
        // scale = 5.01 → High detail → screenStep for step=1 is 5.01 px (< 10) → filtered out
        // screenStep for step=5 is 25.05 px (>= 10) → kept
        var levels = GridRenderer.GetEligibleLevels(pixelsPerWorldUnit: 5.01);

        Assert.DoesNotContain(1, levels); // 1 * 5 = 5 px → filtered
        Assert.Contains(5, levels);       // 5 * 5 = 25 px → kept
    }

    [Fact]
    public void GetEligibleLevels_at_high_detail_keeps_levels_with_sufficient_spacing()
    {
        // scale = 5.01
        // step 1: 5.01 px → filtered
        // step 5: 25.05 px → kept
        // step 10: 50.1 px → kept
        var levels = GridRenderer.GetEligibleLevels(pixelsPerWorldUnit: 5.01);

        Assert.DoesNotContain(1, levels);
        Assert.Contains(5, levels);
        Assert.Contains(10, levels);
        Assert.Contains(100, levels);
        Assert.Contains(1000, levels);
        Assert.Contains(10000, levels);
    }

    [Fact]
    public void GetEligibleLevels_filters_all_below_10_pixels()
    {
        // scale = 0.5 → step 10 gives 5 px (< 10)
        var levels = GridRenderer.GetEligibleLevels(pixelsPerWorldUnit: 0.5);

        Assert.DoesNotContain(10, levels);
        Assert.Contains(100, levels);  // 100 * 0.5 = 50 px
    }

    [Fact]
    public void GetEligibleLevels_at_very_low_scale()
    {
        // scale = 0.009 → step 1000 gives 9 px (< 10), only 10000 kept
        var levels = GridRenderer.GetEligibleLevels(pixelsPerWorldUnit: 0.009);

        Assert.DoesNotContain(1000, levels);
        Assert.Contains(10000, levels); // 10000 * 0.009 = 90 px
    }

    [Fact]
    public void GetEligibleLevels_returns_empty_when_no_level_has_sufficient_spacing()
    {
        // scale = 0.0005 → step 10000 gives 5 px (< 10)
        var levels = GridRenderer.GetEligibleLevels(pixelsPerWorldUnit: 0.0005);

        Assert.Empty(levels);
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
