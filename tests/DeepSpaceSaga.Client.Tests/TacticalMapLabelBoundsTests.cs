using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapLabelBoundsTests
{
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(100, 100)]
    [InlineData(3, 3)]
    [InlineData(0, 0)]
    public void Oversized_label_geometry_fits_even_tiny_viewports(int width, int height)
    {
        var geometry = ObjectLabelLayout.Create(new(width / 2f, height / 2f), 135,
            1500, new(width, height), 5);
        Assert.InRange(geometry.PlaqueRect.Left, 0, width);
        Assert.InRange(geometry.PlaqueRect.Right, 0, width);
        Assert.InRange(geometry.PlaqueRect.Top, 0, height);
        Assert.InRange(geometry.PlaqueRect.Bottom, 0, height);
    }

    [Fact]
    public void Ellipsis_preserves_text_elements_and_obeys_pixel_budget()
    {
        using var paint = new SKPaint { TextSize = 12 };
        string text = string.Concat(Enumerable.Repeat("A\u0301🚀", 100));
        const float budget = 90;
        string fitted = ObjectLabelRenderer.FitText(text, paint, budget);
        Assert.EndsWith("…", fitted);
        Assert.True(paint.MeasureText(fitted) <= budget);
        int cut = fitted.Length - 1;
        Assert.Contains(cut, System.Globalization.StringInfo.ParseCombiningCharacters(text));
        Assert.Equal(string.Empty, ObjectLabelRenderer.FitText(text, paint, 0));
        Assert.Equal(text, ObjectLabelRenderer.FitText(text, paint, paint.MeasureText(text)));
    }

    [Fact]
    public void Real_screen_renders_long_name_across_resize_without_exception()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new ObjectMotionSnapshot("ship", 0, 0, 0, 0,
                DisplayName: new string('W', 500), RenderObjectType: SpaceObjectType.PlayerShip)], "ship"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        screen.Render(surface.Canvas, 100, 100);
        screen.Render(surface.Canvas, 1280, 720);
        Assert.Single(screen.RenderStates);
    }
}
