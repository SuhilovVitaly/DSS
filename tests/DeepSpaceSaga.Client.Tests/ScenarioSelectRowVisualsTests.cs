using DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Pixel-level checks for scenario row selection/hover feedback: rows have no fill (fully
/// transparent over the content panel), hover on an unselected row draws a dark gray
/// outline, the selected row always draws a full-height accent line down its left edge
/// (matching the action panel's LABEL | VALUE separator color) instead of an outline, and
/// hovering the selected row does not also draw the gray outline. Complements
/// ScreenEventTests.cs's click/selection behavior.
/// </summary>
public class ScenarioSelectRowVisualsTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static readonly SKColor SelectedAccentColor = new(0xFF, 0x84, 0x04);
    private static readonly SKColor HoveredBorderColor = new(90, 90, 90);

    private static ScenarioInfo Scenario(string id, string name) =>
        new($"/fake/{id}/scenario.json", id, name, "A short description.");

    private static ScenarioSelectScreen NewScreenWithTwoRows(out SKBitmap bitmap)
    {
        var scenarios = new[] { Scenario("a", "A"), Scenario("b", "B") };
        var screen = new ScenarioSelectScreen(() => scenarios);
        bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight); // establishes _screenWidth/_screenHeight for hit-testing
        return screen;
    }

    private static SKColor TopBorderPixel(SKBitmap bitmap, (float X, float Y, float W, float H) row)
    {
        float pl = ScenarioSelectLayout.PanelLeft(ScreenWidth);
        float pt = ScenarioSelectLayout.PanelTop(ScreenHeight);
        return bitmap.GetPixel((int)(pl + row.X + 5), (int)(pt + row.Y));
    }

    private static SKColor LeftEdgeMidPixel(SKBitmap bitmap, (float X, float Y, float W, float H) row)
    {
        float pl = ScenarioSelectLayout.PanelLeft(ScreenWidth);
        float pt = ScenarioSelectLayout.PanelTop(ScreenHeight);
        return bitmap.GetPixel((int)(pl + row.X), (int)(pt + row.Y + row.H / 2f));
    }

    private static (float X, float Y, float W, float H) RowZero() =>
        ScenarioSelectLayout.RowRect(ScenarioSelectLayout.ListTop, ScenarioSelectLayout.RowHeightFor(1));

    private static (float X, float Y, float W, float H) RowOne() =>
        ScenarioSelectLayout.RowRect(
            ScenarioSelectLayout.ListTop + ScenarioSelectLayout.RowHeightFor(1) + ScenarioSelectLayout.RowSpacing,
            ScenarioSelectLayout.RowHeightFor(1));

    private static void MoveMouseTo(ScenarioSelectScreen screen, (float X, float Y, float W, float H) row)
    {
        float pl = ScenarioSelectLayout.PanelLeft(ScreenWidth);
        float pt = ScenarioSelectLayout.PanelTop(ScreenHeight);
        screen.OnMouseMove(pl + row.X + 10, pt + row.Y + 10);
    }

    [Fact]
    public void Unselected_unhovered_row_has_no_border()
    {
        var screen = NewScreenWithTwoRows(out var bitmap);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        var pixel = TopBorderPixel(bitmap, RowOne());
        Assert.NotEqual(HoveredBorderColor, pixel);
        Assert.NotEqual(SelectedAccentColor, pixel);
    }

    [Fact]
    public void Hovering_an_unselected_row_draws_a_gray_border()
    {
        var screen = NewScreenWithTwoRows(out var bitmap);
        MoveMouseTo(screen, RowOne());
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        Assert.Equal(HoveredBorderColor, TopBorderPixel(bitmap, RowOne()));
    }

    [Fact]
    public void Selected_row_has_a_left_edge_accent_line_even_without_hover()
    {
        // Row 0 is selected by default (no click needed).
        var screen = NewScreenWithTwoRows(out var bitmap);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        Assert.Equal(SelectedAccentColor, LeftEdgeMidPixel(bitmap, RowZero()));
    }

    [Fact]
    public void Selected_row_has_no_top_border_only_the_left_accent_line()
    {
        var screen = NewScreenWithTwoRows(out var bitmap);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        Assert.NotEqual(SelectedAccentColor, TopBorderPixel(bitmap, RowZero()));
    }

    [Fact]
    public void Hovering_the_selected_row_keeps_the_accent_line_not_a_gray_outline()
    {
        var screen = NewScreenWithTwoRows(out var bitmap);
        MoveMouseTo(screen, RowZero());
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        Assert.Equal(SelectedAccentColor, LeftEdgeMidPixel(bitmap, RowZero()));
        Assert.NotEqual(HoveredBorderColor, TopBorderPixel(bitmap, RowZero()));
    }
}
