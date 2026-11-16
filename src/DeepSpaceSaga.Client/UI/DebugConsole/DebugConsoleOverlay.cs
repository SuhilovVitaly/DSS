using DeepSpaceSaga.Client.UI.Controls;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

/// <summary>
/// Draws the open console over the top part of the window, above every screen of the stack.
/// It is not a stack screen: opening it never activates, deactivates or replaces a screen.
/// </summary>
internal sealed class DebugConsoleOverlay
{
    internal const float HeightFraction = 0.45f;
    internal const string Title = "Консоль отладки — Enter: выполнить, Esc или Ctrl+Alt+P: закрыть";
    internal const string ExecutingText = "Выполняется…";
    internal static readonly SKColor Background = new(6, 10, 16, 236);

    private static readonly SKPaint BackgroundFill = new() { Color = Background, Style = SKPaintStyle.Fill };
    private int _shownVersion = -1;

    internal SKRect PanelBounds { get; private set; }
    internal SKRect OutputBounds { get; private set; }
    internal SKRect InputBounds { get; private set; }

    public void Render(SKCanvas canvas, DebugConsoleController console, int width, int height, float uiScale)
    {
        if (!console.IsOpen || width <= 0 || height <= 0) return;
        float scale = float.IsFinite(uiScale) && uiScale > 0 ? uiScale : 1;
        float pad = 10 * scale, titleHeight = 22 * scale, inputHeight = 34 * scale;
        PanelBounds = new SKRect(0, 0, width, MathF.Round(height * HeightFraction));
        InputBounds = new SKRect(pad, PanelBounds.Bottom - pad - inputHeight, width - pad, PanelBounds.Bottom - pad);
        OutputBounds = new SKRect(pad, pad + titleHeight, width - pad, Math.Max(pad + titleHeight, InputBounds.Top - pad));

        canvas.DrawRect(PanelBounds, BackgroundFill);
        using (var border = new SKPaint { Color = MenuStyle.ColorPanelBorder, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1, scale) })
            canvas.DrawLine(0, PanelBounds.Bottom, width, PanelBounds.Bottom, border);
        using (var text = new SKPaint { Color = MenuStyle.ColorTextDim, TextSize = 13 * scale, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular })
            canvas.DrawText(console.IsExecuting ? Title + "   " + ExecutingText : Title, pad, pad + 15 * scale, text);

        var output = console.OutputPanel;
        output.Layout(OutputBounds, scale);
        int version = console.OutputVersion;
        if (version != _shownVersion)
        {
            output.SetText(console.OutputText);
            _shownVersion = version;
        }
        output.Draw(canvas);
        console.Input.Render(canvas, InputBounds, focused: true);
    }

    /// <summary>Every click goes to the console while it is open; only the output scrollbar reacts.</summary>
    public bool OnMouseDown(DebugConsoleController console, float x, float y, MouseButton button)
    {
        console.OutputPanel.OnMouseDown(x, y, button);
        console.OutputPanel.HasFocus = true;
        return true;
    }

    public bool OnMouseMove(DebugConsoleController console, float x, float y) => console.OutputPanel.OnMouseMove(x, y);

    public void OnMouseUp(DebugConsoleController console, MouseButton button) => console.OutputPanel.OnMouseUp(button);

    /// <summary>Wheel in browser units (positive scrolls down).</summary>
    public bool OnMouseWheel(DebugConsoleController console, float x, float y, float browserDelta)
    {
        console.OutputPanel.OnMouseWheel(x, y, browserDelta);
        return true;
    }

    public static void ResetInput(DebugConsoleController console) => console.OutputPanel.ResetInput();
}
