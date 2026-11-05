using System.Collections.Immutable;
using System.Globalization;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

public sealed class DropDown<T>
{
    private readonly Func<T, string> _label;
    private float _scale = 1;
    private int _visibleRows;
    private int _highlighted;
    private double _wheel;
    private string? _caption;
    private string? _hoverText;
    private long _hoverAt;
    private SKPoint _pointer;
    public ImmutableArray<T> Items { get; private set; } = [];
    public int SelectedIndex { get; private set; } = -1;
    public bool HasSelection => SelectedIndex >= 0;
    public T SelectedItem => HasSelection ? Items[SelectedIndex] : throw new InvalidOperationException("No selection.");
    public string SelectedLabel => HasSelection ? _label(SelectedItem) : Localization.Get("TacticalBattle.NoOptions");
    public bool Enabled { get; set; } = true;
    public bool HasFocus { get; set; }
    public bool IsOpen { get; private set; }
    public int FirstVisibleIndex { get; private set; }
    public SKRect Bounds { get; private set; }
    public SKRect PopupBounds { get; private set; }
    public event Action<T>? Changed;

    public DropDown(IEnumerable<T> items, Func<T, string> label)
    {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        SetItems(items);
    }
    public void SetItems(IEnumerable<T> items)
    {
        T previous = HasSelection ? SelectedItem : default!;
        bool had = HasSelection;
        Items = items.ToImmutableArray();
        SelectedIndex = Items.IsEmpty ? -1 : 0;
        if (had) for (int i = 0; i < Items.Length; i++)
            if (EqualityComparer<T>.Default.Equals(previous, Items[i])) { SelectedIndex = i; break; }
        _caption = null; FirstVisibleIndex = 0; _visibleRows = 0; PopupBounds = SKRect.Empty;
        ResetInput();
    }
    public bool SelectIndex(int index, bool notify = false)
    {
        if (index < 0 || index >= Items.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (SelectedIndex == index) return false;
        SelectedIndex = index; _caption = null;
        if (notify) Changed?.Invoke(SelectedItem);
        return true;
    }
    public void Layout(SKRect bounds, SKRect viewport, float uiScale = 1, int maximumRows = 8)
    {
        if (!float.IsFinite(uiScale) || uiScale <= 0 || maximumRows < 1) throw new ArgumentOutOfRangeException(nameof(uiScale));
        if (Bounds != bounds || _scale != uiScale) _caption = null;
        Bounds = bounds; _scale = uiScale;
        float row = 30 * uiScale;
        float below = Math.Max(0, viewport.Bottom - bounds.Bottom), above = Math.Max(0, bounds.Top - viewport.Top);
        int desired = Math.Min(maximumRows, Items.Length);
        bool down = below >= desired * row || below >= above;
        _visibleRows = Math.Min(desired, (int)((down ? below : above) / row));
        float width = Math.Min(bounds.Width, viewport.Width);
        if (_visibleRows <= 0 || width <= 0) { PopupBounds = SKRect.Empty; ResetInput(); return; }
        float left = Math.Clamp(bounds.Left, viewport.Left, viewport.Right - width);
        PopupBounds = SKRect.Create(left, down ? bounds.Bottom : bounds.Top - _visibleRows * row, width, _visibleRows * row);
        ClampScroll();
        if (IsOpen) EnsureVisible();
    }
    public void ResetInput() { IsOpen = false; _wheel = 0; _hoverText = null; }
    private void Open()
    {
        if (!Enabled || Items.IsEmpty || PopupBounds.IsEmpty) return;
        IsOpen = true; _highlighted = SelectedIndex; EnsureVisible();
    }
    public bool OnMouseDown(float x, float y, MouseButton button)
    {
        if (!Enabled) { ResetInput(); return false; }
        if (button != MouseButton.Left) { bool was = IsOpen; ResetInput(); return was; }
        if (IsOpen && PopupBounds.Contains(x, y))
        {
            int index = RowAt(y);
            if (index >= 0 && index < Items.Length) SelectIndex(index, notify: true);
            ResetInput(); return true;
        }
        if (Bounds.Contains(x, y))
        {
            HasFocus = true;
            if (IsOpen) ResetInput(); else Open();
            return true;
        }
        bool consume = IsOpen; ResetInput(); return consume;
    }
    private int RowAt(float y) => FirstVisibleIndex + (int)((y - PopupBounds.Top) / (30 * _scale));
    public bool OnMouseMove(float x, float y, long? nowMs = null)
    {
        _pointer = new(x, y);
        string? text = Bounds.Contains(x, y) ? SelectedLabel : null;
        if (IsOpen && PopupBounds.Contains(x, y))
        {
            _highlighted = Math.Clamp(RowAt(y), 0, Items.Length - 1);
            text = _label(Items[_highlighted]);
        }
        if (text != _hoverText) { _hoverText = text; _hoverAt = nowMs ?? Environment.TickCount64; }
        return text is not null;
    }
    public bool OnMouseWheel(float x, float y, float browserDelta)
    {
        if (!Enabled || !IsOpen || !float.IsFinite(browserDelta) || !PopupBounds.Contains(x, y) && !Bounds.Contains(x, y)) return false;
        _wheel += browserDelta / 100.0;
        int steps = (int)Math.Truncate(_wheel); _wheel -= steps;
        FirstVisibleIndex += steps; ClampScroll();
        return true;
    }
    private void ClampScroll() => FirstVisibleIndex = Math.Clamp(FirstVisibleIndex, 0, Math.Max(0, Items.Length - _visibleRows));
    private void EnsureVisible()
    {
        if (_highlighted < FirstVisibleIndex) FirstVisibleIndex = _highlighted;
        if (_highlighted >= FirstVisibleIndex + _visibleRows) FirstVisibleIndex = _highlighted - _visibleRows + 1;
        ClampScroll();
    }
    public bool OnKeyDown(Key key)
    {
        if (!Enabled || !HasFocus || Items.IsEmpty) return false;
        if (key == Key.Tab) { ResetInput(); return false; }
        if (key == Key.Escape) { bool was = IsOpen; ResetInput(); return was; }
        if (key is Key.Enter or Key.KeypadEnter or Key.Space)
        {
            if (IsOpen) { SelectIndex(_highlighted, notify: true); ResetInput(); } else Open();
            return true;
        }
        int current = IsOpen ? _highlighted : SelectedIndex;
        int next;
        switch (key)
        {
            case Key.Up: next = current - 1; break;
            case Key.Down: next = current + 1; break;
            case Key.Home: next = 0; break;
            case Key.End: next = Items.Length - 1; break;
            case Key.PageUp: next = current - Math.Max(1, _visibleRows); break;
            case Key.PageDown: next = current + Math.Max(1, _visibleRows); break;
            default: return false;
        }
        next = Math.Clamp(next, 0, Items.Length - 1);
        if (IsOpen) { _highlighted = next; EnsureVisible(); } else SelectIndex(next, notify: true);
        return true;
    }
    public string DisplayCaption
    {
        get
        {
            if (_caption is not null) return _caption;
            using var paint = ControlText.Paint(_scale);
            return _caption ??= ControlText.Ellipsize(SelectedLabel, Math.Max(0, Bounds.Width - 56 * _scale), paint);
        }
    }
    public void Draw(SKCanvas canvas)
    {
        if (Bounds.IsEmpty) return;
        canvas.Save(); canvas.ClipRect(Bounds);
        canvas.Translate(Bounds.Left, Bounds.Top); canvas.Scale(_scale);
        XenonComboBox.DrawClosed(canvas, SKRect.Create(0, 0, Bounds.Width / _scale, Bounds.Height / _scale), "", "", HasFocus && Enabled);
        canvas.Restore();
        ControlVisuals.Label(canvas, DisplayCaption, new(Bounds.Left + 44 * _scale, Bounds.Top, Bounds.Right - 12 * _scale, Bounds.Bottom), Enabled, _scale);
    }
    /// <summary>Draw after other controls so the popup cannot be covered by later rows.</summary>
    public void DrawPopup(SKCanvas canvas)
    {
        if (!IsOpen || !Enabled || PopupBounds.IsEmpty) return;
        ControlVisuals.Frame(canvas, PopupBounds, true, true, _scale);
        using var paint = ControlText.Paint(_scale);
        for (int row = 0; row < _visibleRows; row++)
        {
            int index = FirstVisibleIndex + row;
            var rect = SKRect.Create(PopupBounds.Left, PopupBounds.Top + row * 30 * _scale, PopupBounds.Width, 30 * _scale);
            canvas.Save(); canvas.Translate(rect.Left, rect.Top); canvas.Scale(_scale);
            XenonComboBox.DrawOption(canvas, SKRect.Create(0, 0, rect.Width / _scale, 30), "", index == _highlighted);
            canvas.Restore();
            string caption = ControlText.Ellipsize(_label(Items[index]), Math.Max(0, rect.Width - 36 * _scale), paint);
            ControlVisuals.Label(canvas, caption, new(rect.Left + 24 * _scale, rect.Top, rect.Right - 12 * _scale, rect.Bottom), true, _scale);
        }
    }
    public void DrawTooltip(SKCanvas canvas, SKRect viewport, long? nowMs = null)
    {
        if (_hoverText is not null && (nowMs ?? Environment.TickCount64) - _hoverAt >= MenuStyle.TooltipHoverDelaySeconds * 1000)
            ControlText.Tooltip(canvas, _hoverText, _pointer, viewport, _scale);
    }
}

internal static class ControlText
{
    internal static SKPaint Paint(float scale) => new() { IsAntialias = true, Typeface = MenuStyle.TypefaceRegular, TextSize = 14 * scale, Color = MenuStyle.ColorText };
    internal static string Ellipsize(string text, float width, SKPaint paint)
    {
        if (paint.MeasureText(text) <= width) return text;
        const string ellipsis = "…";
        if (paint.MeasureText(ellipsis) > width) return "";
        int[] starts = StringInfo.ParseCombiningCharacters(text);
        int low = 0, high = starts.Length;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            int end = mid == starts.Length ? text.Length : starts[mid];
            if (paint.MeasureText(text[..end] + ellipsis) <= width) low = mid; else high = mid - 1;
        }
        return text[..(low == starts.Length ? text.Length : starts[low])] + ellipsis;
    }
    internal static ImmutableArray<string> Wrap(string text, float width, SKPaint paint)
    {
        if (width <= 0) return [];
        var lines = ImmutableArray.CreateBuilder<string>();
        foreach (string paragraph in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length > 0 && paint.MeasureText(line + " " + word) > width) { lines.Add(line); line = ""; }
                if (paint.MeasureText(word) <= width) { line = line.Length == 0 ? word : line + " " + word; continue; }
                foreach (int start in StringInfo.ParseCombiningCharacters(word))
                {
                    string element = StringInfo.GetNextTextElement(word, start);
                    if (line.Length > 0 && paint.MeasureText(line + element) > width) { lines.Add(line); line = ""; }
                    line += element;
                }
            }
            lines.Add(line);
        }
        return lines.ToImmutable();
    }
    internal static void Tooltip(SKCanvas canvas, string text, SKPoint pointer, SKRect viewport, float scale)
    {
        if (viewport.Width <= 16 * scale || viewport.Height <= 16 * scale) return;
        using var paint = Paint(scale);
        float width = Math.Min(400 * scale, viewport.Width - 16 * scale);
        var lines = Wrap(text, width - 16 * scale, paint);
        float height = Math.Min(viewport.Height - 8 * scale, lines.Length * 18 * scale + 16 * scale);
        float left = Math.Clamp(pointer.X + 12 * scale, viewport.Left + 4 * scale, viewport.Right - width - 4 * scale);
        float top = pointer.Y + height + 16 * scale <= viewport.Bottom ? pointer.Y + 16 * scale : pointer.Y - height - 12 * scale;
        top = Math.Clamp(top, viewport.Top + 4 * scale, viewport.Bottom - height - 4 * scale);
        var rect = SKRect.Create(left, top, width, height);
        ControlVisuals.Frame(canvas, rect, true, true, scale);
        canvas.Save(); canvas.ClipRect(rect);
        for (int i = 0; i < lines.Length; i++) canvas.DrawText(lines[i], left + 8 * scale, top + 8 * scale - paint.FontMetrics.Ascent + i * 18 * scale, paint);
        canvas.Restore();
    }
}
