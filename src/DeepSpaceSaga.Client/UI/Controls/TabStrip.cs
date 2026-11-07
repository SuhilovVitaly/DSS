using System.Collections.Immutable;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

public sealed record TabItem(string Id, string Label);
public sealed class TabStrip
{
    public ImmutableArray<TabItem> Items { get; }
    public int SelectedIndex { get; private set; }
    public string SelectedId => Items[SelectedIndex].Id;
    public SKRect Bounds { get; set; }
    public bool Enabled { get; set; } = true;
    public bool HasFocus { get; set; }
    public event Action<string>? Changed;
    public TabStrip(IEnumerable<TabItem> items)
    {
        Items = items.ToImmutableArray();
        if (Items.IsEmpty || Items.Any(i => i is null || string.IsNullOrWhiteSpace(i.Id) || i.Label is null)
            || Items.Select(i => i.Id).Distinct(StringComparer.Ordinal).Count() != Items.Length)
            throw new ArgumentException("Tabs require unique nonempty IDs and labels.", nameof(items));
    }
    public bool Select(string id, bool notify = false)
    {
        int index = -1;
        for (int i = 0; i < Items.Length; i++) if (Items[i].Id == id) { index = i; break; }
        if (index < 0) throw new ArgumentException("Unknown tab ID.", nameof(id));
        if (SelectedIndex == index) return false;
        SelectedIndex = index;
        if (notify) Changed?.Invoke(SelectedId);
        return true;
    }
    public bool OnMouseDown(float x, float y, MouseButton button)
    {
        if (!Enabled || button != MouseButton.Left || Bounds.Width <= 0 || !Bounds.Contains(x, y)) return false;
        HasFocus = true;
        int index = Math.Clamp((int)((x - Bounds.Left) / Bounds.Width * Items.Length), 0, Items.Length - 1);
        Select(Items[index].Id, notify: true);
        return true;
    }
    public bool OnKeyDown(Key key)
    {
        if (!Enabled || !HasFocus) return false;
        int index;
        switch (key)
        {
            case Key.Left: case Key.Up: index = (SelectedIndex + Items.Length - 1) % Items.Length; break;
            case Key.Right: case Key.Down: index = (SelectedIndex + 1) % Items.Length; break;
            case Key.Home: index = 0; break;
            case Key.End: index = Items.Length - 1; break;
            default: return false;
        }
        Select(Items[index].Id, notify: true);
        return true;
    }
    public void Draw(SKCanvas canvas, float uiScale = 1)
    {
        if (Bounds.IsEmpty || uiScale <= 0) return;
        float width = Bounds.Width / Items.Length;
        for (int i = 0; i < Items.Length; i++)
        {
            var rect = SKRect.Create(Bounds.Left + i * width, Bounds.Top, width, Bounds.Height);
            ControlVisuals.Frame(canvas, rect, i == SelectedIndex, Enabled, uiScale);
            ControlVisuals.Label(canvas, Items[i].Label, rect, Enabled, uiScale, center: true);
        }
    }
}

internal static class ControlVisuals
{
    internal static void Frame(SKCanvas canvas, SKRect rect, bool active, bool enabled, float scale)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = active && enabled ? MenuStyle.ColorButtonHover : MenuStyle.ColorButtonBg };
        canvas.DrawRect(rect, paint);
        paint.Color = !enabled ? XenonStyle.DisabledText : active ? XenonStyle.Cyan : MenuStyle.ColorButtonBorder;
        paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = scale;
        canvas.DrawRect(rect, paint);
    }
    internal static void Label(SKCanvas canvas, string text, SKRect rect, bool enabled, float scale, bool center = false)
    {
        if (rect.IsEmpty) return;
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = enabled ? MenuStyle.ColorText : XenonStyle.DisabledText,
            Typeface = MenuStyle.TypefaceRegular,
            TextSize = 14 * scale,
            TextAlign = center ? SKTextAlign.Center : SKTextAlign.Left
        };
        canvas.Save(); canvas.ClipRect(rect);
        canvas.DrawText(ControlText.SafeSymbols(text), center ? rect.MidX : rect.Left, MenuStyle.VerticalCenterBaseline(rect, paint), paint);
        canvas.Restore();
    }
}
