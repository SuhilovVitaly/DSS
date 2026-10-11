using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Defeat;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class DefeatScreenTests
{
    [Fact]
    public void Defeat_has_only_load_and_main_menu()
    {
        var screen = Render(640, 360);
        var layout = screen.Layout;
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnMouseDown(layout.Load.MidX, layout.Load.MidY, MouseButton.Left));
        Assert.Equal(ScreenEvent.MainMenu, screen.OnMouseDown(layout.MainMenu.MidX, layout.MainMenu.MidY, MouseButton.Left));
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnKeyDown(Key.Enter));
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Tab));
        Assert.Equal(ScreenEvent.MainMenu, screen.OnKeyDown(Key.Space));
        screen.OnKeyDown(Key.Up);
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnKeyDown(Key.Enter));
    }

    [Fact]
    public void Escape_and_outside_do_not_resume()
    {
        var screen = Render(640, 360);
        foreach (var key in new[] { Key.Escape, Key.F5, Key.F9, Key.Number1, Key.Number2 })
            Assert.Equal(ScreenEvent.None, screen.OnKeyDown(key));
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(0, 0, MouseButton.Left));
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(320, 180, MouseButton.Right));
        Assert.Equal(ScreenEvent.None, screen.OnMouseWheel(320, 180, 10));
        Assert.False(screen.OnMouseMove(0, 0));
    }

    [Theory]
    [InlineData(1280, 720, 1)]
    [InlineData(640, 360, 1.2)]
    [InlineData(480, 320, 1.5)]
    public void Buttons_fit_supported_ui_scales(int width, int height, double scale)
    {
        int logicalWidth = (int)(width / scale), logicalHeight = (int)(height / scale);
        var screen = Render(width, height, (float)scale);
        var layout = screen.Layout;
        Assert.True(new SKRect(0, 0, logicalWidth, logicalHeight).Contains(layout.Panel));
        Assert.True(layout.Panel.Contains(layout.Load));
        Assert.True(layout.Panel.Contains(layout.MainMenu));
        Assert.True(layout.Load.Bottom < layout.MainMenu.Top);
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnMouseDown(layout.Load.MidX * (float)scale, layout.Load.MidY * (float)scale, MouseButton.Left));
        Assert.Equal(ScreenEvent.MainMenu, screen.OnMouseDown(layout.MainMenu.MidX * (float)scale, layout.MainMenu.MidY * (float)scale, MouseButton.Left));
    }

    private static DefeatScreen Render(int width, int height, float scale = 1)
    {
        var screen = new DefeatScreen(scale);
        screen.OnActivated();
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        return screen;
    }
}
