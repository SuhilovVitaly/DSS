using DeepSpaceSaga.Client.UI.Assets;
using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

/// <summary>Reuses three paints throughout a frame; shared bitmap cache owns item images.</summary>
internal sealed class TradePainter(SKCanvas canvas) : IDisposable
{
    // The menu's display fonts do not cover every UI arrow; use a body font with Cyrillic and symbol coverage.
    private static readonly SKTypeface Regular = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal) ?? MenuStyle.TypefaceRegular;
    private static readonly SKTypeface Bold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold) ?? MenuStyle.TypefaceBold;
    internal static readonly SKColor Background = TabletopStyle.Background, Surface = TabletopStyle.Inset,
        Border = TabletopStyle.Edge, TextColor = TabletopStyle.Ink, Muted = TabletopStyle.Secondary,
        Accent = TabletopStyle.Gold, Amber = TabletopStyle.Gold, Green = TabletopStyle.Success,
        Red = TabletopStyle.Danger, Selected = TabletopStyle.Steel;
    private readonly SKPaint _line = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
    private readonly SKPaint _text = new() { IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
    internal void Box(SKRect rect, SKColor? color = null, SKColor? border = null) =>
        TabletopStyle.Draw(canvas, rect, color ?? Surface,
            recessed: color is null, highlighted: border is not null && border != Border);

    internal void Line(float x, float y, float right, SKColor? color = null)
    { _line.Color = color ?? Border; canvas.DrawLine(x, y, right, y, _line); }
    internal void Text(string text, SKRect rect, float size = 17, SKColor? color = null, bool bold = false, SKTextAlign align = SKTextAlign.Left)
    {
        _text.TextSize = size; _text.Color = color ?? TextColor; _text.Typeface = bold ? Bold : Regular;
        _text.TextAlign = align;
        if (_text.MeasureText(text) > rect.Width)
        {
            int length = text.Length;
            while (length > 0 && _text.MeasureText(text[..length] + "…") > rect.Width) length--;
            text = length > 0 ? text[..length] + "…" : "";
        }
        canvas.Save(); canvas.ClipRect(rect);
        canvas.DrawText(text, align == SKTextAlign.Right ? rect.Right : align == SKTextAlign.Center ? rect.MidX : rect.Left,
            rect.MidY - (_text.FontMetrics.Ascent + _text.FontMetrics.Descent) / 2, _text);
        canvas.Restore();
    }
    internal void Paragraph(string text, SKRect rect, float size = 14, SKColor? color = null)
    {
        _text.TextSize = size; _text.Typeface = Regular;
        var words = text.Split(' '); string line = ""; float top = rect.Top;
        foreach (string word in words)
        {
            string next = line.Length == 0 ? word : line + " " + word;
            if (_text.MeasureText(next) > rect.Width && line.Length > 0)
            {
                Text(line, new(rect.Left, top, rect.Right, top + size + 5), size, color ?? Muted);
                top += size + 5; line = word;
                if (top + size + 5 > rect.Bottom) return;
            }
            else line = next;
        }
        if (top + size + 5 <= rect.Bottom) Text(line, new(rect.Left, top, rect.Right, top + size + 5), size, color ?? Muted);
    }
    internal void Button(SKRect rect, string text, bool hovered = false, bool active = false, bool enabled = true, bool primary = false, float size = 16)
    {
        var state = !enabled ? ButtonState.Disabled : hovered ? ButtonState.Hovered : ButtonState.Normal;
        TabletopStyle.Button(canvas, rect, state, primary || active);
        float padding = rect.Width < 70 ? 4 : 10;
        Text(text, new(rect.Left + padding, rect.Top, rect.Right - padding, rect.Bottom), size,
            TabletopStyle.ButtonInk(state, primary || active), primary, SKTextAlign.Center);
    }
    internal void Chevron(float x, float y)
    {
        _line.Color = Muted;
        canvas.DrawLine(x - 5, y - 2, x, y + 3, _line);
        canvas.DrawLine(x, y + 3, x + 5, y - 2, _line);
    }
    internal void Bar(SKRect rect, double before, double after)
    {
        Box(rect, Border);
        float first = rect.Left + rect.Width * (float)Math.Clamp(before, 0, 1);
        float last = rect.Left + rect.Width * (float)Math.Clamp(after, 0, 1);
        if (first > rect.Left) Box(new(rect.Left, rect.Top, first, rect.Bottom), Accent);
        if (last > first) Box(new(first, rect.Top, last, rect.Bottom), Green);
        if (last < first) Box(new(last, rect.Top, first, rect.Bottom), Amber.WithAlpha(180));
    }
    internal void Icon(string itemId, SKRect rect)
    {
        var path = TradeItemPresentation.ItemImagePath(itemId);
        var bitmap = path is null ? null : UiAssetLoader.LoadBitmap(path);
        if (bitmap is null) { Box(rect, Selected); return; }
        float scale = Math.Min(rect.Width / bitmap.Width, rect.Height / bitmap.Height);
        canvas.DrawBitmap(bitmap, SKRect.Create(rect.MidX - bitmap.Width * scale / 2, rect.MidY - bitmap.Height * scale / 2, bitmap.Width * scale, bitmap.Height * scale));
    }
    public void Dispose() { _line.Dispose(); _text.Dispose(); }
}
