using System.Collections.Immutable;
using Silk.NET.Input;
using SkiaSharp;
using SkinStyle = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Controls;

public sealed class ScrollTextPanel
{
    private float _scale = 1;
    private float _grab;
    private float _lineHeight = 18;
    private (IntPtr Typeface, float Size) _metrics;
    internal string? StyleRole { get; init; }
    public string Text { get; private set; } = "";
    public ImmutableArray<string> Lines { get; private set; } = [];
    public SKRect Bounds { get; private set; }
    public float ScrollOffset { get; private set; }
    public bool AutoScroll { get; set; } = true;
    public bool HasFocus { get; set; }
    public bool IsDragging { get; private set; }
    public float LineHeight => _lineHeight;
    public float MaximumScroll => Math.Max(0, Lines.Length * LineHeight - Math.Max(0, Bounds.Height - 16 * _scale));
    public SKRect ScrollTrack => new(Bounds.Right - 12 * _scale, Bounds.Top + 4 * _scale, Bounds.Right - 4 * _scale, Bounds.Bottom - 4 * _scale);
    public SKRect ScrollThumb
    {
        get
        {
            var track = ScrollTrack;
            if (track.Height <= 0 || MaximumScroll <= 0) return track;
            float height = Math.Min(track.Height, Math.Max(20 * _scale, track.Height * (Bounds.Height - 16 * _scale) / (Lines.Length * LineHeight)));
            return SKRect.Create(track.Left, track.Top + (track.Height - height) * ScrollOffset / MaximumScroll, track.Width, height);
        }
    }
    public void Layout(SKRect bounds, float uiScale = 1)
    {
        if (!float.IsFinite(uiScale) || uiScale <= 0) throw new ArgumentOutOfRangeException(nameof(uiScale));
        if (Bounds == bounds && _scale == uiScale) return;
        bool follow = AutoScroll && AtBottom;
        ResetInput();
        Bounds = bounds; _scale = uiScale;
        Reflow(follow);
    }
    private bool AtBottom => ScrollOffset >= MaximumScroll - 0.5f;
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Text == text) return;
        bool follow = AutoScroll && AtBottom;
        Text = text; Reflow(follow);
    }
    private void Reflow(bool follow)
    {
        using var paint = TextPaint();
        Reflow(paint, follow);
    }

    private SKPaint TextPaint() => ControlText.Paint(_scale, StyleRole is null ? (SkinStyle.Role?)null : new SkinStyle.Role(StyleRole + ".text"), Text);

    private void Reflow(SKPaint paint, bool follow)
    {
        _metrics = (paint.Typeface?.Handle ?? IntPtr.Zero, paint.TextSize);
        _lineHeight = StyleRole is not null &&
            (SkinStyle.Has(StyleRole + ".text", "fontSize") || SkinStyle.Has(StyleRole + ".text", "font"))
            ? Math.Max(18 * _scale, paint.FontSpacing)
            : 18 * _scale;
        Lines = Text.Length == 0 ? [] : ControlText.Wrap(Text, Math.Max(0, Bounds.Width - 28 * _scale), paint);
        ScrollOffset = follow ? MaximumScroll : Math.Clamp(ScrollOffset, 0, MaximumScroll);
    }
    public void ScrollToBottom() => ScrollOffset = MaximumScroll;
    public bool OnMouseWheel(float x, float y, float browserDelta)
    {
        if (!Bounds.Contains(x, y) || !float.IsFinite(browserDelta)) return false;
        ScrollOffset = Math.Clamp(ScrollOffset + browserDelta / 100f * 3 * LineHeight, 0, MaximumScroll);
        return true;
    }
    public bool OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left || !Bounds.Contains(x, y)) return false;
        HasFocus = true;
        if (MaximumScroll <= 0 || !ScrollTrack.Contains(x, y)) return true;
        var thumb = ScrollThumb;
        if (thumb.Contains(x, y)) { IsDragging = true; _grab = y - thumb.Top; }
        else ScrollOffset = Math.Clamp(ScrollOffset + (y < thumb.Top ? -1 : 1) * Bounds.Height, 0, MaximumScroll);
        return true;
    }
    public bool OnMouseMove(float x, float y)
    {
        if (IsDragging && float.IsFinite(y))
        {
            var track = ScrollTrack; var thumb = ScrollThumb;
            float travel = track.Height - thumb.Height;
            ScrollOffset = travel <= 0 ? 0 : Math.Clamp((y - _grab - track.Top) / travel, 0, 1) * MaximumScroll;
        }
        return IsDragging || Bounds.Contains(x, y);
    }
    public void OnMouseUp(MouseButton button) { if (button == MouseButton.Left) IsDragging = false; }
    public void ResetInput() => IsDragging = false;
    public bool OnKeyDown(Key key)
    {
        if (!HasFocus) return false;
        float offset;
        switch (key)
        {
            case Key.Up: offset = ScrollOffset - LineHeight; break;
            case Key.Down: offset = ScrollOffset + LineHeight; break;
            case Key.PageUp: offset = ScrollOffset - Bounds.Height; break;
            case Key.PageDown: offset = ScrollOffset + Bounds.Height; break;
            case Key.Home: offset = 0; break;
            case Key.End: offset = MaximumScroll; break;
            default: return false;
        }
        ScrollOffset = Math.Clamp(offset, 0, MaximumScroll); return true;
    }
    public void Draw(SKCanvas canvas)
    {
        if (Bounds.IsEmpty) return;
        SkinStyle.Role? frameRole = StyleRole is null ? null : new(HasFocus ? StyleRole + ".focused" : StyleRole);
        ControlVisuals.Frame(canvas, Bounds, HasFocus, true, _scale, frameRole);
        using var paint = TextPaint();
        if (_metrics != (paint.Typeface?.Handle ?? IntPtr.Zero, paint.TextSize))
        {
            float position = MaximumScroll > 0 ? ScrollOffset / MaximumScroll : 0;
            bool follow = AutoScroll && AtBottom;
            Reflow(paint, follow);
            if (!follow)
                ScrollOffset = position * MaximumScroll;
        }
        var textRect = new SKRect(Bounds.Left + 8 * _scale, Bounds.Top + 8 * _scale, Bounds.Right - 20 * _scale, Bounds.Bottom - 8 * _scale);
        canvas.Save(); canvas.ClipRect(textRect);
        int first = Math.Max(0, (int)(ScrollOffset / LineHeight));
        int end = Math.Min(Lines.Length, first + (int)(Bounds.Height / LineHeight) + 2);
        for (int i = first; i < end; i++)
            canvas.DrawText(Lines[i], textRect.Left, textRect.Top - paint.FontMetrics.Ascent + i * LineHeight - ScrollOffset, paint);
        canvas.Restore();
        if (MaximumScroll <= 0) return;
        SkinStyle.Box(canvas, StyleRole is null ? "control.frame.scroll.track" : StyleRole + ".scroll.track",
            ScrollTrack, MenuStyle.ColorButtonBorder, null, _scale);
        SkinStyle.Box(canvas, StyleRole is null ? "control.frame.scroll.thumb" : StyleRole + ".scroll.thumb",
            ScrollThumb, XenonStyle.Cyan, null, _scale);
    }
}
