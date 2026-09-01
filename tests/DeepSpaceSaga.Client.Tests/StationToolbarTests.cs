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
    public void Food_rations_icon_is_loaded()
    {
        Assert.True(StationToolbar.HasLoadedFoodRationsImage);
    }

    [Fact]
    public void ResolveFoodRationsCount_sums_the_item_across_every_installed_modules_cargo()
    {
        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1, GameTimeMs: 0, CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty,
            InstalledModules: ImmutableArray.Create(
                new InstalledModuleSnapshot(
                    ModuleId: "MOD-1", ModuleTypeId: "module.container.basic", DisplayName: "Cargo Bay 1",
                    Position: 0, CommandTypeIds: ImmutableArray<string>.Empty,
                    Cargo: ImmutableArray.Create(
                        new CargoStackSnapshot(StationToolbar.FoodRationsItemTypeId, 12),
                        new CargoStackSnapshot("item.ice", 999))),
                new InstalledModuleSnapshot(
                    ModuleId: "MOD-2", ModuleTypeId: "module.container.basic", DisplayName: "Cargo Bay 2",
                    Position: 1, CommandTypeIds: ImmutableArray<string>.Empty,
                    Cargo: ImmutableArray.Create(
                        new CargoStackSnapshot(StationToolbar.FoodRationsItemTypeId, 8)))));

        Assert.Equal(20, StationToolbar.ResolveFoodRationsCount(snapshot));
    }

    [Fact]
    public void ResolveFoodRationsCount_is_zero_for_a_null_snapshot_or_no_installed_modules()
    {
        Assert.Equal(0, StationToolbar.ResolveFoodRationsCount(null));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1, GameTimeMs: 0, CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty);
        Assert.Equal(0, StationToolbar.ResolveFoodRationsCount(snapshot));
    }

    [Fact]
    public void Draw_places_the_food_rations_icon_before_the_exit_button()
    {
        using var bitmap = new SKBitmap((int)StationToolbar.Width, (int)StationToolbar.Height);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 0, 0, stationName: null, isStationHub: false, foodRationsCount: 42);
        canvas.Flush();

        // The rations icon+value sit in the gap just left of the exit button — not
        // overlapping it (StationToolbar.ResourceInfoGapFromExitButton keeps them apart).
        float exitLeft = StationToolbar.ExitButtonLocalRect().Left;
        Assert.True(RegionHasNonBackgroundPixel(bitmap, (int)(exitLeft - 100), (int)exitLeft - 2));
    }

    [Fact]
    public void FoodRationsLocalRect_stays_fixed_regardless_of_the_actual_digit_count()
    {
        // The field is reserved wide enough for "9999" up front — the icon's position
        // (FoodRationsLocalRect) must not depend on today's actual count.
        var rectWithOneDigit = StationToolbar.FoodRationsLocalRect();

        using var bitmap = new SKBitmap((int)StationToolbar.Width, (int)StationToolbar.Height);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        StationToolbar.Draw(canvas, 0, 0, stationName: null, isStationHub: false, foodRationsCount: 9999);
        canvas.Flush();

        var rectWithFourDigits = StationToolbar.FoodRationsLocalRect();
        Assert.Equal(rectWithOneDigit, rectWithFourDigits);

        // A 4-digit value must not bleed past the reserved field's right edge into the
        // exit-button gap.
        Assert.True(rectWithFourDigits.Right <= StationToolbar.ExitButtonLocalRect().Left - StationToolbar.ResourceInfoGapFromExitButton + 0.5f);
    }

    [Fact]
    public void Hovering_food_rations_shows_a_tooltip_below_the_toolbar()
    {
        using var hoveredBitmap = new SKBitmap((int)StationToolbar.Width, 120);
        hoveredBitmap.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(hoveredBitmap))
            StationToolbar.Draw(canvas, 0, 0, stationName: null, isStationHub: false, foodRationsCount: 5,
                isFoodRationsHovered: true);

        using var normalBitmap = new SKBitmap((int)StationToolbar.Width, 120);
        normalBitmap.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(normalBitmap))
            StationToolbar.Draw(canvas, 0, 0, stationName: null, isStationHub: false, foodRationsCount: 5,
                isFoodRationsHovered: false);

        // Below the toolbar strip stays fully transparent when not hovered, but the hovered
        // render paints a tooltip box there. Start a couple px past Height to skip the
        // toolbar's own 1px border stroke, whose antialiased edge bleeds slightly past it.
        bool foundBelowWhenHovered = false, foundBelowWhenNormal = false;
        for (int y = (int)StationToolbar.Height + 2; y < hoveredBitmap.Height; y++)
        for (int x = 0; x < hoveredBitmap.Width; x++)
        {
            if (hoveredBitmap.GetPixel(x, y).Alpha > 0) foundBelowWhenHovered = true;
            if (normalBitmap.GetPixel(x, y).Alpha > 0) foundBelowWhenNormal = true;
        }

        Assert.True(foundBelowWhenHovered);
        Assert.False(foundBelowWhenNormal);
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

    [Fact]
    public void Glow_color_is_more_saturated_than_the_active_location_color()
    {
        StationToolbar.ColorNameGlow.ToHsv(out float glowHue, out float glowSaturation, out float glowValue);
        StationToolbar.ColorNameActive.ToHsv(out float activeHue, out float activeSaturation, out float activeValue);

        Assert.True(glowSaturation > activeSaturation);
        // Same hue/brightness family — only saturation should change (allow a small
        // tolerance for HSV<->RGB round-trip rounding).
        Assert.InRange(glowHue, activeHue - 1f, activeHue + 1f);
        Assert.InRange(glowValue, activeValue - 1f, activeValue + 1f);
    }

    [Fact]
    public void Hovering_a_non_hub_name_adds_a_glow_bleeding_past_the_glyphs()
    {
        // Blur bleeds a few px beyond the tight glyph bounds — a ring just outside
        // NameLocalRect stays pure background when not hovered, but picks up glow color
        // when hovered, proving the hover state actually changes what's drawn (not just
        // the crisp text, which NameLocalRect already covers).
        using var hoveredBitmap = new SKBitmap(1420, 80);
        hoveredBitmap.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(hoveredBitmap))
            StationToolbar.Draw(canvas, 10, 10, "Alpha Station", isStationHub: false, isHovered: true);

        using var normalBitmap = new SKBitmap(1420, 80);
        normalBitmap.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(normalBitmap))
            StationToolbar.Draw(canvas, 10, 10, "Alpha Station", isStationHub: false, isHovered: false);

        var tight = StationToolbar.NameLocalRect("Alpha Station");
        var padded = SKRect.Inflate(tight, 6f, 6f);

        Assert.False(RingHasNonBackgroundPixel(normalBitmap, tight, padded, 10, 10));
        Assert.True(RingHasNonBackgroundPixel(hoveredBitmap, tight, padded, 10, 10));
    }

    [Fact]
    public void Hovering_the_exit_button_adds_a_glow_bleeding_past_the_icon()
    {
        Assert.True(StationToolbar.HasLoadedExitButtonImage);

        using var hoveredBitmap = new SKBitmap(1420, 80);
        hoveredBitmap.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(hoveredBitmap))
            StationToolbar.Draw(canvas, 10, 10, stationName: null, isStationHub: false, isExitButtonHovered: true);

        using var normalBitmap = new SKBitmap(1420, 80);
        normalBitmap.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(normalBitmap))
            StationToolbar.Draw(canvas, 10, 10, stationName: null, isStationHub: false, isExitButtonHovered: false);

        var tight = StationToolbar.ExitButtonLocalRect();
        var padded = SKRect.Inflate(tight, 6f, 6f);

        Assert.False(RingHasNonBackgroundPixel(normalBitmap, tight, padded, 10, 10));
        Assert.True(RingHasNonBackgroundPixel(hoveredBitmap, tight, padded, 10, 10));
    }

    private static bool RingHasNonBackgroundPixel(SKBitmap bitmap, SKRect tight, SKRect padded, float offsetX, float offsetY)
    {
        int left = Math.Max(0, (int)(offsetX + padded.Left));
        int top = Math.Max(0, (int)(offsetY + padded.Top));
        int right = Math.Min(bitmap.Width, (int)Math.Ceiling(offsetX + padded.Right));
        int bottom = Math.Min(bitmap.Height, (int)Math.Ceiling(offsetY + padded.Bottom));

        int tightLeft = (int)(offsetX + tight.Left);
        int tightTop = (int)(offsetY + tight.Top);
        int tightRight = (int)Math.Ceiling(offsetX + tight.Right);
        int tightBottom = (int)Math.Ceiling(offsetY + tight.Bottom);

        for (int y = top; y < bottom; y++)
        for (int x = left; x < right; x++)
        {
            bool insideTight = x >= tightLeft && x < tightRight && y >= tightTop && y < tightBottom;
            if (insideTight)
                continue;

            if (bitmap.GetPixel(x, y) != StationToolbar.ColorBackground)
                return true;
        }

        return false;
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
    public void Draw_with_window_name_renders_a_separator_gap_and_the_window_name_color()
    {
        // ColorSeparator is now the same white as the link station name, so its presence
        // can't be told apart by color alone — check geometry instead: something (the
        // ">>" glyph) is drawn in the gap right after the station name ends.
        using var bitmap = new SKBitmap((int)StationToolbar.Width, (int)StationToolbar.Height);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 0, 0, "Alpha Station", isStationHub: false, windowName: "TRADE");
        canvas.Flush();

        Assert.True(BitmapContainsColor(bitmap, StationToolbar.ColorNameActive));

        int nameEnd = (int)StationToolbar.NameLocalRect("Alpha Station").Right;
        Assert.True(RegionHasNonBackgroundPixel(bitmap, nameEnd + 2, nameEnd + (int)StationToolbar.NameSegmentGap));
    }

    [Fact]
    public void Draw_with_window_name_places_the_window_name_after_the_station_name()
    {
        // The window-name segment's own color (ColorNameActive) is unambiguous, so locate
        // it directly — this also stays correct now that the toolbar's exit-button icon is
        // always drawn regardless of windowName, which would otherwise dominate a plain
        // "rightmost non-background pixel" comparison.
        using var bitmap = RenderToolbar("Alpha Station", windowName: "TRADE");

        int windowNameStart = MinColumnOfColor(bitmap, StationToolbar.ColorNameActive);
        int stationNameEnd = (int)StationToolbar.NameLocalRect("Alpha Station").Right;

        Assert.True(windowNameStart > stationNameEnd);
    }

    [Fact]
    public void Draw_with_window_name_but_no_station_name_starts_earlier_than_with_one()
    {
        using var withoutStationName = RenderToolbar(stationName: null, windowName: "FINANCE");
        using var withStationName = RenderToolbar("Alpha Station", windowName: "FINANCE");

        // The window-name segment's own color (ColorNameActive) is unambiguous, so locate
        // it directly rather than reasoning about exact separator/gap pixel widths.
        int startWithoutStation = MinColumnOfColor(withoutStationName, StationToolbar.ColorNameActive);
        int startWithStation = MinColumnOfColor(withStationName, StationToolbar.ColorNameActive);

        Assert.True(startWithoutStation < startWithStation);
    }

    private static SKBitmap RenderToolbar(string? stationName, string? windowName)
    {
        var bitmap = new SKBitmap((int)StationToolbar.Width, (int)StationToolbar.Height);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        StationToolbar.Draw(canvas, 0, 0, stationName, isStationHub: false, windowName: windowName);
        canvas.Flush();
        return bitmap;
    }

    private static bool BitmapContainsColor(SKBitmap bitmap, SKColor color)
    {
        for (int y = 0; y < bitmap.Height; y++)
        for (int x = 0; x < bitmap.Width; x++)
            if (bitmap.GetPixel(x, y) == color)
                return true;

        return false;
    }

    private static bool RegionHasNonBackgroundPixel(SKBitmap bitmap, int xStart, int xEnd)
    {
        xStart = Math.Max(0, xStart);
        xEnd = Math.Min(bitmap.Width, xEnd);

        for (int y = 0; y < bitmap.Height; y++)
        for (int x = xStart; x < xEnd; x++)
            if (bitmap.GetPixel(x, y) != StationToolbar.ColorBackground)
                return true;

        return false;
    }

    private static int MinColumnOfColor(SKBitmap bitmap, SKColor color)
    {
        for (int x = 0; x < bitmap.Width; x++)
        for (int y = 0; y < bitmap.Height; y++)
            if (bitmap.GetPixel(x, y) == color)
                return x;

        return int.MaxValue;
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
