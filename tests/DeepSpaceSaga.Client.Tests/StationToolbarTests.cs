using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class StationToolbarTests
{
    private static AuthoritativeSnapshot DockedSnapshot(string stationName = "Alpha Station") =>
        new(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(
                new ObjectMotionSnapshot("SHIP-01", 0, 0, 0, 0, IsDocked: true, DockedStationObjectId: "STN-01"),
                new ObjectMotionSnapshot("STN-01", 10, 10, 0, 0, DisplayName: stationName)),
            PlayerShipObjectId: "SHIP-01");

    [Fact]
    public void Size_matches_the_1400x60_toolbar_spec()
    {
        Assert.Equal(1400f, StationToolbar.Width);
        Assert.Equal(60f, StationToolbar.Height);
    }

    [Fact]
    public void Name_font_size_is_26px()
    {
        Assert.Equal(26f, StationToolbar.NameFontSize);
    }

    [Fact]
    public void Draw_fills_the_interior_with_the_spec_background_color()
    {
        using var bitmap = new SKBitmap(1420, 80);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 10, 10, stationName: null, isStationHub: false);
        canvas.Flush();

        var interior = bitmap.GetPixel(700, 40);
        Assert.Equal(new SKColor(0x5e, 0x5e, 0x5e), interior);
    }

    [Fact]
    public void Draw_strokes_the_top_edge_with_the_spec_border_color()
    {
        using var bitmap = new SKBitmap(1420, 80);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 10, 10, stationName: null, isStationHub: false);
        canvas.Flush();

        var borderPixel = bitmap.GetPixel(700, 10);
        Assert.Equal(new SKColor(0x99, 0x99, 0x99), borderPixel);
    }

    [Fact]
    public void Draw_does_not_paint_outside_the_toolbar_bounds()
    {
        using var bitmap = new SKBitmap(1420, 80);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 10, 10, stationName: null, isStationHub: false);
        canvas.Flush();

        Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
        Assert.Equal(0, bitmap.GetPixel(700, 75).Alpha);
    }

    [Fact]
    public void ResolveDockedStationName_reads_the_docked_station_display_name()
    {
        string? name = StationToolbar.ResolveDockedStationName(DockedSnapshot("Alpha Station"));
        Assert.Equal("Alpha Station", name);
    }

    [Fact]
    public void ResolveDockedStationName_is_null_when_not_docked()
    {
        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1, GameTimeMs: 0, CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(new ObjectMotionSnapshot("SHIP-01", 0, 0, 0, 0, IsDocked: false)),
            PlayerShipObjectId: "SHIP-01");

        Assert.Null(StationToolbar.ResolveDockedStationName(snapshot));
    }

    [Fact]
    public void ResolveDockedStationName_is_null_for_a_null_snapshot()
    {
        Assert.Null(StationToolbar.ResolveDockedStationName(null));
    }

    [Fact]
    public void Draw_on_the_station_hub_paints_the_name_in_the_active_location_color()
    {
        using var bitmap = new SKBitmap(1420, 80);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 10, 10, "Alpha Station", isStationHub: true);
        canvas.Flush();

        Assert.True(RectContainsColor(bitmap, StationToolbar.NameLocalRect("Alpha Station"), 10, 10,
            new SKColor(0xe9, 0x9e, 0x58)));
    }

    [Fact]
    public void Draw_on_a_non_hub_screen_paints_the_name_in_white()
    {
        using var bitmap = new SKBitmap(1420, 80);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 10, 10, "Alpha Station", isStationHub: false);
        canvas.Flush();

        Assert.True(RectContainsColor(bitmap, StationToolbar.NameLocalRect("Alpha Station"), 10, 10,
            new SKColor(0xff, 0xff, 0xff)));
    }

    /// <summary>
    /// Anti-aliased glyph rendering means no single fixed pixel is guaranteed to be pure
    /// foreground color, so these tests scan every pixel in the label's hit-test rect for
    /// an exact match instead of sampling one point.
    /// </summary>
    private static bool RectContainsColor(SKBitmap bitmap, SKRect localRect, float offsetX, float offsetY, SKColor color)
    {
        int left = (int)(offsetX + localRect.Left);
        int top = (int)(offsetY + localRect.Top);
        int right = (int)Math.Ceiling(offsetX + localRect.Right);
        int bottom = (int)Math.Ceiling(offsetY + localRect.Bottom);

        for (int y = Math.Max(0, top); y < Math.Min(bitmap.Height, bottom); y++)
        for (int x = Math.Max(0, left); x < Math.Min(bitmap.Width, right); x++)
            if (bitmap.GetPixel(x, y) == color)
                return true;

        return false;
    }

    [Fact]
    public void NameLocalRect_is_inset_by_roughly_20px_from_the_toolbars_top_left_corner()
    {
        var local = StationToolbar.NameLocalRect("Alpha Station");

        // Tight glyph bounds vary a couple px with the first letter's side bearing/font
        // metrics — assert "close to the 20px inset", not bit-exact.
        Assert.False(local.IsEmpty);
        Assert.InRange(local.Left, StationToolbar.NameOffsetX - 5f, StationToolbar.NameOffsetX + 5f);
        Assert.InRange(local.Top, StationToolbar.NameOffsetY - 10f, StationToolbar.NameOffsetY + 10f);
    }

    [Fact]
    public void NameLocalRect_is_empty_for_a_null_or_empty_name()
    {
        Assert.True(StationToolbar.NameLocalRect(null).IsEmpty);
        Assert.True(StationToolbar.NameLocalRect("").IsEmpty);
    }
}
