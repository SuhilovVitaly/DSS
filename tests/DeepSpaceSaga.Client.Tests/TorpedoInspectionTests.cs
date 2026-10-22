using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public sealed class TorpedoInspectionTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public void Rendered_speed_value_has_clear_gap_after_label(float scale)
    {
        var panel = new ObjectInfoPanel();
        var data = new ObjectInfoPanelData("torpedo", "Torpedo", 3, 0, SpaceObjectType.Missile);
        using var bitmap = new SKBitmap((int)(800 * scale), (int)(600 * scale));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        canvas.Scale(scale);
        panel.Render(canvas, 800, 0, null, data);

        var body = panel.RowBodyRects[1];
        // The text starts after the 200px image and two 6px paddings.
        float textX = body.Left + 212;
        int row = ObjectInfoPanel.BuildLines(data).FindIndex(line => line.Label == "Speed");
        float baseline = body.Top + 6 + 16 - 3 + row * 16;
        using var labelPaint = new SKPaint { TextSize = 12, Typeface = XenonStyle.TypefaceRegular };
        float labelEnd = (textX + labelPaint.MeasureText("Speed")) * scale;

        int firstValuePixel = int.MaxValue;
        for (int y = (int)((baseline - 14) * scale); y <= (int)((baseline + 2) * scale); y++)
        {
            for (int x = (int)(textX * scale); x < (int)((body.Right - 6) * scale); x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                // Values are brighter than the grey labels (140); the background is blue.
                if (pixel.Red > 150 && Math.Abs(pixel.Red - pixel.Green) <= 2 && Math.Abs(pixel.Green - pixel.Blue) <= 2)
                    firstValuePixel = Math.Min(firstValuePixel, x);
            }
        }

        Assert.NotEqual(int.MaxValue, firstValuePixel);
        Assert.True(firstValuePixel >= labelEnd + 4 * scale,
            $"Speed overlaps its value at scale {scale}: label ends at {labelEnd}, value starts at {firstValuePixel}.");
    }

}
