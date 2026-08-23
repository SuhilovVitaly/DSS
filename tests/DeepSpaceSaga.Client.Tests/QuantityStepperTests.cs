using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The [-] [ value ] [+] quantity control (Trade screen's Buy/Sell quantity picker).
/// Geometry/hit-test are a public contract consumed by TradeScreen (Batch 6); Draw is a
/// smoke test only, matching ImageButtonTests.Draw_does_not_throw_for_any_button_state.
/// </summary>
public class QuantityStepperTests
{
    private static readonly SKRect[] _rects =
    {
        new(0, 0, 160, 44),
        new(20, 8, 240, 52), // width 220
    };

    [Theory]
    [MemberData(nameof(RectCases))]
    public void Zones_cover_rect_without_gaps_or_overlaps(SKRect rect)
    {
        var minus = QuantityStepper.MinusButtonRect(rect);
        var value = QuantityStepper.ValueBoxRect(rect);
        var plus = QuantityStepper.PlusButtonRect(rect);

        // No overlap: adjacent edges touch exactly, don't cross.
        Assert.Equal(minus.Right, value.Left);
        Assert.Equal(value.Right, plus.Left);

        // Full coverage: outer edges match the input rect exactly.
        Assert.Equal(rect.Left, minus.Left);
        Assert.Equal(rect.Right, plus.Right);

        // Same vertical extent for all three zones.
        foreach (var sub in new[] { minus, value, plus })
        {
            Assert.Equal(rect.Top, sub.Top);
            Assert.Equal(rect.Top + QuantityStepper.Height, sub.Bottom);
        }
    }

    public static IEnumerable<object[]> RectCases()
    {
        foreach (var rect in _rects)
            yield return new object[] { rect };
    }

    [Theory]
    [MemberData(nameof(RectCases))]
    public void HitTest_returns_minus_for_points_inside_minus_zone(SKRect rect)
    {
        var minus = QuantityStepper.MinusButtonRect(rect);
        Assert.Equal(QuantityStepperButton.Minus, QuantityStepper.HitTest(rect, minus.Left, minus.Top));
        Assert.Equal(QuantityStepperButton.Minus, QuantityStepper.HitTest(rect, minus.MidX, minus.MidY));
        Assert.Equal(QuantityStepperButton.Minus, QuantityStepper.HitTest(rect, minus.Right - 1, minus.Bottom - 1));
    }

    [Theory]
    [MemberData(nameof(RectCases))]
    public void HitTest_returns_plus_for_points_inside_plus_zone(SKRect rect)
    {
        var plus = QuantityStepper.PlusButtonRect(rect);
        Assert.Equal(QuantityStepperButton.Plus, QuantityStepper.HitTest(rect, plus.Left, plus.Top));
        Assert.Equal(QuantityStepperButton.Plus, QuantityStepper.HitTest(rect, plus.MidX, plus.MidY));
        Assert.Equal(QuantityStepperButton.Plus, QuantityStepper.HitTest(rect, plus.Right - 1, plus.Bottom - 1));
    }

    [Theory]
    [MemberData(nameof(RectCases))]
    public void HitTest_returns_none_for_points_in_value_box_and_outside_rect(SKRect rect)
    {
        var value = QuantityStepper.ValueBoxRect(rect);
        Assert.Equal(QuantityStepperButton.None, QuantityStepper.HitTest(rect, value.MidX, value.MidY));

        // Outside the whole control, in each direction.
        Assert.Equal(QuantityStepperButton.None, QuantityStepper.HitTest(rect, rect.Left - 5, rect.MidY));
        Assert.Equal(QuantityStepperButton.None, QuantityStepper.HitTest(rect, rect.Right + 5, rect.MidY));
        Assert.Equal(QuantityStepperButton.None, QuantityStepper.HitTest(rect, rect.MidX, rect.Top - 5));
        Assert.Equal(QuantityStepperButton.None, QuantityStepper.HitTest(rect, rect.MidX, rect.Bottom + 5));
    }

    [Fact]
    public void Draw_does_not_throw_for_any_combination_of_state_and_value()
    {
        using var bitmap = new SKBitmap(300, 80);
        using var canvas = new SKCanvas(bitmap);
        var rect = new SKRect(10, 10, 250, 54);

        var hoveredOptions = new[] { QuantityStepperButton.None, QuantityStepperButton.Minus, QuantityStepperButton.Plus };
        var boolOptions = new[] { true, false };
        var values = new long[] { -5, 0, 1, 999 };

        foreach (var hovered in hoveredOptions)
        foreach (var canDecrement in boolOptions)
        foreach (var canIncrement in boolOptions)
        foreach (var value in values)
            QuantityStepper.Draw(canvas, rect, value, hovered, canDecrement, canIncrement);
    }
}
