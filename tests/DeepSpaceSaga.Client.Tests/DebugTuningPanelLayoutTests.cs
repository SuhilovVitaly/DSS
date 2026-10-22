using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DebugTuningPanelLayoutTests
{
    private static DebugTuningSnapshot Snapshot()
    {
        var values = ImmutableArray.CreateBuilder<DebugTuningValue>();
        foreach (string module in DebugTuningPanel.ModuleIds)
        {
            if (module.Contains("engine", StringComparison.Ordinal))
            {
                values.Add(new(module, "maxSpeedKmS", 5)); values.Add(new(module, "accelerationMps2", 100)); values.Add(new(module, "turnRateDegPerSec", 20));
            }
            else
            {
                values.Add(new(module, "reloadMs", 1000));
                foreach (string profile in module.Contains("countermeasure", StringComparison.Ordinal) ? new[] { "wide", "fast" } : new[] { "slow", "medium", "fast" })
                {
                    values.Add(new(module, "speedKmS", 5, profile)); values.Add(new(module, "blastRadiusKm", 1, profile));
                }
            }
        }
        var defaults = values.ToImmutable();
        return new(1, defaults.SetItem(0, defaults[0] with { Value = 5.5 }), defaults);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void CatalogRowsSectionsScalingAndScroll(float scale)
    {
        using var panel = new DebugTuningPanel(k => k) { Visible = true };
        panel.Prepare(1920 / scale, 1080 / scale, Snapshot());
        Assert.Equal(25, panel.RowCount); Assert.Equal(5, panel.SectionCount);
        Assert.Equal(360 * scale, panel.Bounds.Width * scale);
        Assert.True(panel.GetRow(0).Modified); Assert.Equal(5, panel.GetRow(0).Default);
        Assert.True(panel.Scroll(panel.Bounds.MidX, panel.Bounds.MidY, -100));
        Assert.True(panel.ScrollOffset > 0);
        Assert.False(panel.Scroll(1, 1, 1));
        panel.Scroll(panel.Bounds.MidX, panel.Bounds.MidY, 100);
        float rowTop = panel.RowBounds(0).Top;
        Assert.True(panel.ToggleSection(panel.Bounds.MidX, rowTop - 10));
        Assert.True(panel.RowBounds(0).IsEmpty);
        panel.Dispose(); panel.Dispose(); Assert.True(panel.IsDisposed);
    }

    [Fact]
    public void UnchangedOpenPanelAllocatesNothingPerFrame()
    {
        using var panel = new DebugTuningPanel(k => k) { Visible = true };
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var snapshot = Snapshot();
        for (int i = 0; i < 100; i++) { panel.Prepare(800, 600, snapshot); panel.Render(surface.Canvas); }
        var canvas = surface.Canvas;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) { panel.Prepare(800, 600, snapshot); panel.Render(canvas); }
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
