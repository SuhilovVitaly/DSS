using DeepSpaceSaga.Client.UI.Controls;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class WidgetsBasicTests
{
    [Fact]
    public void SliderRetainsUntouchedSuggestionAndSnapsOnlyManualInputWithBankersRounding()
    {
        const double suggestion = 3.141592653589793;
        var slider = new Slider(0, 10, suggestion) { HasFocus = true };
        var changes = new List<double>(); slider.Changed += changes.Add;
        Assert.Equal(suggestion, slider.Value);
        Assert.Empty(changes);
        slider.OnKeyDown(Key.Right);
        Assert.Equal(3.5, slider.Value);
        Assert.Equal(new[] { 3.5 }, changes);
        slider.SetValue(0.25, snap: true); Assert.Equal(0, slider.Value);
        slider.SetValue(0.75, snap: true); Assert.Equal(1, slider.Value);
        slider.SetRange(-2, 2.125);
        slider.OnKeyDown(Key.End); Assert.Equal(2.125, slider.Value);
        slider.OnKeyDown(Key.Home); Assert.Equal(-2, slider.Value);
        slider.SetValue(suggestion); Assert.Equal(2.125, slider.Value);
    }

    [Theory]
    [InlineData(0.75f)]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void DragUsesScaledBoundsClampsOutsideAndStopsOnReleaseOrReset(float scale)
    {
        var slider = new Slider(0, 10, 0) { Bounds = new(20 * scale, 20 * scale, 220 * scale, 50 * scale) };
        Assert.False(slider.OnMouseDown(120 * scale, 30 * scale, MouseButton.Right));
        Assert.True(slider.OnMouseDown(120 * scale, 30 * scale, MouseButton.Left));
        Assert.Equal(5, slider.Value);
        Assert.True(slider.IsDragging);
        slider.OnMouseMove(-100, 0); Assert.Equal(0, slider.Value);
        slider.OnMouseMove(10000, 0); Assert.Equal(10, slider.Value);
        slider.OnMouseUp(MouseButton.Right); Assert.True(slider.IsDragging);
        slider.OnMouseUp(MouseButton.Left); Assert.False(slider.IsDragging);
        slider.OnMouseMove(0, 0); Assert.Equal(10, slider.Value);
        slider.OnMouseDown(120 * scale, 30 * scale, MouseButton.Left);
        slider.ResetInput(); slider.OnMouseMove(0, 0);
        Assert.Equal(5, slider.Value);
        slider.Enabled = false;
        Assert.False(slider.OnKeyDown(Key.Home));
        Assert.False(slider.OnMouseDown(120 * scale, 30 * scale, MouseButton.Left));
        Assert.Equal(5, slider.Value);
    }

    [Fact]
    public void SliderRejectsNonfiniteAndInvalidRangesAndHandlesFixedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(2, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(0, 1, 0, step: 0));
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(0, 1, value));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value, 1, 0));
        }
        var slider = new Slider(4, 4, 10) { HasFocus = true };
        slider.OnKeyDown(Key.Right);
        Assert.Equal(4, slider.Value);
        slider.SetRange(7, 8); Assert.Equal(7, slider.Value);
    }

    [Fact]
    public void ToggleClickAndKeyboardEmitOneChangeAndRespectFocusDisabledAndRepeat()
    {
        var toggle = new Toggle("Свой дрон") { Bounds = new(0, 0, 200, 30) };
        var events = new List<bool>(); toggle.Changed += events.Add;
        Assert.False(toggle.OnKeyDown(Key.Space));
        Assert.False(toggle.OnMouseDown(10, 10, MouseButton.Right));
        Assert.True(toggle.OnMouseDown(10, 10, MouseButton.Left));
        Assert.True(toggle.Value);
        toggle.OnKeyDown(Key.Space, repeat: true); Assert.True(toggle.Value);
        toggle.OnKeyDown(Key.Space); Assert.False(toggle.Value);
        toggle.OnKeyDown(Key.KeypadEnter); Assert.True(toggle.Value);
        toggle.SetValue(true, notify: true); Assert.Equal(3, events.Count);
        toggle.Enabled = false;
        Assert.False(toggle.OnMouseDown(10, 10, MouseButton.Left));
        Assert.False(toggle.OnKeyDown(Key.Enter));
        Assert.Equal(new[] { true, false, true }, events);
    }

    [Fact]
    public void TabsKeepStableIdsAndWrapKeyboardWithoutDuplicateChangeEvents()
    {
        var tabs = new TabStrip([new("own", "Свои"), new("enemy", "Противник"), new("log", "Журнал")])
        { Bounds = new(0, 0, 300, 30) };
        var events = new List<string>(); tabs.Changed += events.Add;
        Assert.Equal("own", tabs.SelectedId);
        Assert.False(tabs.OnKeyDown(Key.Right));
        Assert.True(tabs.OnMouseDown(150, 10, MouseButton.Left));
        Assert.Equal("enemy", tabs.SelectedId);
        tabs.OnMouseDown(150, 10, MouseButton.Left); Assert.Single(events);
        tabs.OnKeyDown(Key.Right); Assert.Equal("log", tabs.SelectedId);
        tabs.OnKeyDown(Key.Right); Assert.Equal("own", tabs.SelectedId);
        tabs.OnKeyDown(Key.Left); Assert.Equal("log", tabs.SelectedId);
        tabs.OnKeyDown(Key.Home); Assert.Equal("own", tabs.SelectedId);
        tabs.Enabled = false;
        Assert.False(tabs.OnMouseDown(150, 10, MouseButton.Left));
        Assert.False(tabs.OnKeyDown(Key.End));
        Assert.Throws<ArgumentException>(() => tabs.Select("missing"));
        Assert.Throws<ArgumentException>(() => new TabStrip([]));
        Assert.Throws<ArgumentException>(() => new TabStrip([new("same", "A"), new("same", "B")]));
    }

    [Theory]
    [InlineData(0.75f)]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void RasterAtUiScalesShowsAllControlsAndRenderDoesNotMutateValues(float scale)
    {
        using var bitmap = new SKBitmap((int)(500 * scale), (int)(200 * scale));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        SKRect Rect(float x, float y, float w, float h) => SKRect.Create(x * scale, y * scale, w * scale, h * scale);
        var slider = new Slider(-5, 5, 0) { Bounds = Rect(20, 20, 280, 30), HasFocus = true };
        var toggle = new Toggle("Предложение ИИ · AI", true) { Bounds = Rect(20, 75, 440, 30) };
        var tabs = new TabStrip([new("own", "Свои"), new("enemy", "Противник"), new("log", "Журнал")])
        { Bounds = Rect(20, 130, 440, 35) };
        tabs.Select("enemy");
        slider.Draw(canvas, scale); toggle.Draw(canvas, scale); tabs.Draw(canvas, scale);
        Assert.Equal(0, slider.Value); Assert.True(toggle.Value); Assert.Equal("enemy", tabs.SelectedId);
        var normal = bitmap.GetPixel((int)(160 * scale), (int)(35 * scale));
        Assert.NotEqual(SKColors.Black, normal);
        Assert.NotEqual(SKColors.Black, bitmap.GetPixel((int)(200 * scale), (int)(135 * scale)));
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { } root)
        {
            string directory = Path.Combine(root, "renders"); Directory.CreateDirectory(directory);
            using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, "widgets-basic-" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png"));
            data.SaveTo(file);
        }
        slider.Enabled = false; slider.Draw(canvas, scale);
        Assert.NotEqual(normal, bitmap.GetPixel((int)(160 * scale), (int)(35 * scale)));
    }
}
