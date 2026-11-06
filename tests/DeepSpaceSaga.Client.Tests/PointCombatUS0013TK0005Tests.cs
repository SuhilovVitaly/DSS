using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Defeat;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0013TK0005Tests
{
    private static CombatEncounterStats Stats => new(1, 2, 3, 4, 5, 6, 7, 8, 90, 100);
    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(480, 320, 1.5f)]
    public void Defeat_renders_all_captured_stats_in_scrollable_block_without_resume(int width, int height, float scale)
    {
        var encounter = new CombatEncounterSnapshot(CombatEncounterState.Lost, 0, 2500, Stats, 2.5);
        var screen = new DefeatScreen(scale, encounter);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, width, height);
        Assert.Equal(11, screen.StatsLines.Count); Assert.Contains("00:00:10", screen.StatsLines[0]);
        Assert.Contains(screen.StatsLines, line => line.EndsWith(": 100", StringComparison.Ordinal));
        Assert.True(new SKRect(0, 0, width / scale, height / scale).Contains(screen.Layout.Panel));
        Assert.True(screen.Layout.Panel.Contains(screen.Layout.Stats)); Assert.True(screen.Layout.Stats.Bottom < screen.Layout.Load.Top);
        var stats = screen.Layout.Stats;
        Assert.Equal(ScreenEvent.None, screen.OnMouseWheel(stats.MidX * scale, stats.MidY * scale, -1));
        screen.Render(canvas, width, height); Assert.True(screen.StatsScrollOffset > 0);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnMouseDown(screen.Layout.Load.MidX * scale, screen.Layout.Load.MidY * scale, MouseButton.Left));
        Assert.Equal(ScreenEvent.MainMenu, screen.OnMouseDown(screen.Layout.MainMenu.MidX * scale, screen.Layout.MainMenu.MidY * scale, MouseButton.Left));
        if (scale == 1 && Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, "defeat-stats-headless.png")); png.SaveTo(file);
        }
    }
}
