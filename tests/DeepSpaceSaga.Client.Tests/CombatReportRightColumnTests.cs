using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportRightColumnTests
{
    [Fact]
    public void ReportsFollowActualObjectInfoBottom()
    {
        var column = GameSessionRightColumnLayout.Create(1280, 720, 8, 590, 496);
        var info = new SKRect(column.Bounds.Left, 8, column.Bounds.Right, 300);
        var reports = column.ReportsAfter(info);
        Assert.Equal(info.Bottom + 8, reports.Top); Assert.Equal(info.Width, reports.Width);
    }

    [Fact]
    public void CollapsedObjectInfoReleasesSpace()
    {
        var column = GameSessionRightColumnLayout.Create(1280, 720, 8, 590, 496);
        Assert.True(column.ReportsAfter(new(column.Bounds.Left, 8, column.Bounds.Right, 40)).Height >
            column.ReportsAfter(new(column.Bounds.Left, 8, column.Bounds.Right, 300)).Height);
    }

    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.2f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1920, 1080, 1.2f)]
    [InlineData(1920, 1080, 1.5f)]
    public void ViewportAndScaleMatrix(int width, int height, float scale)
    {
        float w = width / scale, h = height / scale;
        var column = GameSessionRightColumnLayout.Create(w, h, 8, h - 220, ObjectInfoPanel.ResolveWidth(w, h));
        var info = new ObjectInfoPanel();
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        foreach (bool closed in new[] { false, true })
        {
            info.Render(surface.Canvas, w, 8, null, null, h, column.ObjectInfoMaximumBottom);
            var reports = column.ReportsAfter(info.Bounds);
            Assert.True(reports.Height >= 80); Assert.True(info.Bounds.Bottom < reports.Top);
            Assert.Equal(info.Bounds.Width, reports.Width); Assert.True(reports.Bottom < h - 220);
            if (!closed) info.OnMouseDown(info.CaptionRect.Left + 4, info.CaptionRect.Top + 4);
        }
    }

    [Fact]
    public void MinimumPanelHeightAndNoHudOverlap() => ViewportAndScaleMatrix(1280, 720, 1.5f);

    [Fact]
    public void EmptyPanelDoesNotCoverJournal()
    {
        var column = GameSessionRightColumnLayout.Create(853, 480, 8, 260, 324);
        var journal = new CombatJournalPanel(); using var surface = SKSurface.Create(new SKImageInfo(853, 480));
        journal.Render(surface.Canvas, 853, 480, [], column.Bounds.Left - 8);
        Assert.True(journal.Bounds.Right < column.Bounds.Left);
    }
}
