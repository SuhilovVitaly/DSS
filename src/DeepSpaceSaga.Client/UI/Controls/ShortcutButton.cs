using Silk.NET.Input;
using SkiaSharp;
using SkinStyle = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Controls;

public sealed class ShortcutButton(string label, string keyHint = "", string tooltip = "")
{
    private long _hoverAt;
    private SKPoint _pointer;
    public string Label { get; set; } = label;
    public string KeyHint { get; set; } = keyHint;
    public string Tooltip { get; set; } = tooltip;
    public SKRect Bounds { get; set; }
    public bool Enabled { get; set; } = true;
    public bool HasFocus { get; set; }
    internal bool Primary { get; set; }
    public bool Hovered { get; private set; }
    public event Action? Clicked;
    public bool OnMouseMove(float x, float y, long? nowMs = null)
    {
        bool hover = Bounds.Contains(x, y);
        if (hover && !Hovered) _hoverAt = nowMs ?? Environment.TickCount64;
        Hovered = hover; _pointer = new(x, y);
        return hover;
    }
    public void ResetInput() => Hovered = false;
    public bool OnMouseDown(float x, float y, MouseButton button)
    {
        if (!Enabled || button != MouseButton.Left || !Bounds.Contains(x, y)) return false;
        HasFocus = true; Clicked?.Invoke(); return true;
    }
    public bool OnKeyDown(Key key, bool repeat = false)
    {
        if (!Enabled || !HasFocus || key is not (Key.Enter or Key.KeypadEnter or Key.Space)) return false;
        if (!repeat) Clicked?.Invoke();
        return true;
    }
    public void Draw(SKCanvas canvas, float uiScale = 1)
    {
        if (Bounds.IsEmpty || uiScale <= 0) return;
        string caption = ControlText.SafeSymbols(Label + (KeyHint.Length == 0 ? "" : " [" + KeyHint + "]"));
        string state = !Enabled ? "disabled" : Hovered ? "hover" : HasFocus ? "focused" : "normal";
        var role = SkinStyle.Control("control.button", state);
        var markerRole = SkinStyle.Control("control.button", $"marker.{state}");
        bool boxStyle = SkinStyle.HasBoxStyle(role);
        bool markerStyle = SkinStyle.Has(markerRole, "image") || SkinStyle.Has(markerRole, "fill") ||
            SkinStyle.Has(markerRole, "size");
        bool textStyle = SkinStyle.Has(role, "text") || SkinStyle.Has(role, "font") || SkinStyle.Has(role, "fontSize");
        if (!boxStyle)
        {
            canvas.Save();
            canvas.Translate(Bounds.Left, Bounds.Top);
            canvas.Scale(uiScale);
            GenericButtonTypeA.Draw(canvas, SKRect.Create(0, 0, Bounds.Width / uiScale, Bounds.Height / uiScale),
                textStyle || markerStyle ? "" : caption,
                !Enabled ? ButtonState.Disabled : Hovered || HasFocus ? ButtonState.Hovered : ButtonState.Normal,
                showMarker: !markerStyle, primary: Primary && SkinStyle.Current is null);
            canvas.Restore();
            if (!markerStyle && !textStyle)
                return;
        }
        else
        {
            ControlVisuals.Frame(canvas, Bounds, Hovered || HasFocus, Enabled, uiScale, role);
        }

        float inset = Math.Min(26 * uiScale, Bounds.Width / 3);
        if (boxStyle || markerStyle)
        {
            float size = Math.Min(inset * 0.54f, Math.Clamp(SkinStyle.Number(markerRole, "size", 10), 1, 24) * uiScale);
            var marker = SKRect.Create(Bounds.Left + (inset - size) / 2, Bounds.MidY - size / 2, size, size);
            if (!SkinStyle.Image(canvas, markerRole, marker))
            {
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    Color = SkinStyle.Color(markerRole, "fill", Enabled ? XenonStyle.Cyan : XenonStyle.DisabledText)
                };
                using var path = new SKPath();
                path.MoveTo(marker.MidX, marker.Top);
                path.LineTo(marker.Right, marker.MidY);
                path.LineTo(marker.MidX, marker.Bottom);
                path.LineTo(marker.Left, marker.MidY);
                path.Close();
                canvas.DrawPath(path, paint);
            }
        }

        using var text = ControlText.Paint(uiScale, role, caption, Enabled);
        var textBounds = new SKRect(Bounds.Left + inset, Bounds.Top, Bounds.Right - 5 * uiScale, Bounds.Bottom);
        caption = ControlText.Ellipsize(caption, textBounds.Width, text);
        ControlVisuals.DrawLabel(canvas, caption, textBounds, text, center: true);
    }
    public bool TooltipReady(long nowMs) => Hovered && Tooltip.Length > 0 && nowMs - _hoverAt >= MenuStyle.TooltipHoverDelaySeconds * 1000;
    public void DrawTooltip(SKCanvas canvas, SKRect viewport, float uiScale = 1, long? nowMs = null)
    {
        if (TooltipReady(nowMs ?? Environment.TickCount64)) ControlText.Tooltip(canvas, Tooltip, _pointer, viewport, uiScale);
    }
}
