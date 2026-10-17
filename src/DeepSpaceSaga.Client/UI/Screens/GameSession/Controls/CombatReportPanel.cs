using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

internal sealed class CombatReportPanel : IDisposable
{
    private readonly SKPaint _title = new() { Typeface = MenuStyle.TypefaceBold, TextSize = 14, IsAntialias = true, Color = MenuStyle.ColorText };
    private readonly SKPaint _body = new() { Typeface = MenuStyle.TypefaceRegular, TextSize = 12, IsAntialias = true, Color = MenuStyle.ColorText };
    private readonly SKPaint _fill = new() { Color = new(9, 18, 24, 248) };
    private readonly SKPaint _line = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
    private CombatReportStore? _store;
    private CombatReportFormatter? _formatter;
    private long? _newest, _expanded;
    private int _count = -1;
    private string _caption = "";
    private string[] _emptyLines = [];
    private bool _pointerCaptured;
    internal bool HasFocus { get; private set; }
    internal CombatReportPanelLayout? Layout { get; private set; }
    internal SKRect Bounds => Layout?.Bounds ?? SKRect.Empty;

    internal void Prepare(SKRect bounds, CombatReportStore store, CombatReportFormatter formatter)
    {
        _store = store; _formatter = formatter;
        long? newest = store.Reports.Count > 0 ? store.Reports[0].Sequence : null;
        if (Layout is null || Layout.Bounds != bounds || _newest != newest || _expanded != store.ExpandedSequence || _count != store.Reports.Count)
        {
            Layout = CombatReportPanelLayout.Create(bounds, store, formatter, _title, _body);
            _caption = formatter.Text("Panel");
            _emptyLines = CombatReportPanelLayout.Wrap(formatter.Text("Empty"), Math.Max(1, bounds.Width - 16), _body).ToArray();
            _newest = newest; _expanded = store.ExpandedSequence; _count = store.Reports.Count;
        }
        store.ScrollOffset = Math.Clamp(store.ScrollOffset, 0, Layout.MaxScroll);
    }

    internal void Render(SKCanvas canvas, CombatReportAssets assets)
    {
        if (Layout is not { } layout || _store is null || _formatter is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        canvas.Save(); canvas.ClipRect(Bounds);
        canvas.DrawRect(Bounds, _fill);
        _title.Color = MenuStyle.ColorText;
        canvas.DrawText(_caption, Bounds.Left + 8, Bounds.Top + 22, _title);
        canvas.Save(); canvas.ClipRect(layout.Viewport);
        if (layout.Cards.IsEmpty)
        {
            float y = layout.Viewport.Top + 20;
            foreach (string line in _emptyLines)
            { canvas.DrawText(line, Bounds.Left + 8, y, _body); y += 17; }
        }
        canvas.Translate(0, -_store.ScrollOffset);
        foreach (var card in layout.Cards)
        {
            if (card.Bottom < layout.Viewport.Top + _store.ScrollOffset || card.Header.Top > layout.Viewport.Bottom + _store.ScrollOffset) continue;
            var accent = assets.Accent(card.Text.VisualKey);
            _line.Color = accent;
            canvas.DrawRect(new(card.Header.Left, card.Header.Top, card.Header.Right, card.Bottom), _line);
            if (HasFocus && _store.KeyboardSelection == card.Report.Sequence)
            {
                _line.Color = MenuStyle.ColorText;
                canvas.DrawRect(card.Header, _line);
            }
            _title.Color = MenuStyle.ColorText;
            float y = card.Header.Top + 18;
            foreach (string line in card.TitleLines) { canvas.DrawText(line, card.Header.Left + 8, y, _title); y += 18; }
            canvas.DrawText(card.Text.Time, card.Header.Left + 8, card.Header.Bottom - 8, _body);
            canvas.DrawText(card.Expanded ? "−" : "+", card.Header.Right - 18, card.Header.Top + 18, _body);
            if (!card.Expanded) continue;
            if (assets.Resolve(card.Text.VisualKey) is { } illustration) DrawFit(canvas, illustration, card.Illustration);
            else DrawRole(canvas, card.Illustration, card.Report.Reporter.Role, accent);
            if (assets.ResolvePortrait(card.Report.Reporter.PortraitImage) is { } portrait) DrawFit(canvas, portrait, card.Portrait);
            else DrawRole(canvas, card.Portrait, card.Report.Reporter.Role, accent);
            y = card.Portrait.Top + 16;
            foreach (string line in card.ReporterLines) { canvas.DrawText(line, card.Portrait.Right + 8, y, _body); y += 17; }
            y = card.BodyTop + 12;
            foreach (string line in card.BodyLines) { canvas.DrawText(line, Bounds.Left + 8, y, _body); y += 17; }
        }
        canvas.Restore(); canvas.Restore();
    }

    internal bool OnMouseDown(float x, float y)
    {
        HasFocus = Bounds.Contains(x, y);
        _pointerCaptured = HasFocus;
        if (!HasFocus || _store is null || Layout is null) return false;
        if (Layout.Viewport.Contains(x, y))
        {
            var card = Layout.Cards.FirstOrDefault(c => c.Header.Contains(x, y + _store.ScrollOffset));
            if (card is not null)
            {
                _store.Expand(card.Report.Sequence);
                RefreshLayout();
                EnsureSelectionVisible();
            }
        }
        return true;
    }

    internal bool OnMouseMove(float x, float y) => _pointerCaptured || Bounds.Contains(x, y);
    internal bool OnMouseUp(float x, float y)
    {
        bool consumed = _pointerCaptured || Bounds.Contains(x, y);
        _pointerCaptured = false;
        return consumed;
    }

    internal bool Scroll(float x, float y, float delta)
    {
        if (!Bounds.Contains(x, y) || _store is null || Layout is null) return false;
        if (!float.IsFinite(delta)) return true;
        _store.ScrollOffset = Math.Clamp(_store.ScrollOffset - delta * 48, 0, Layout.MaxScroll);
        return true;
    }

    internal bool OnKeyDown(Key key)
    {
        if (key == Key.Tab && _store is not null && Bounds.Width > 0 && Bounds.Height > 0)
        {
            if (HasFocus) ClearFocus();
            else { HasFocus = true; EnsureSelectionVisible(); }
            return true;
        }
        if (key == Key.Escape && HasFocus) { ClearFocus(); return true; }
        if (!HasFocus || _store is null || key is not (Key.Up or Key.Down or Key.Enter)) return false;
        if (key == Key.Enter && _store.KeyboardSelection is { } selected) { _store.Expand(selected); RefreshLayout(); }
        else if (key != Key.Enter) _store.MoveSelection(key == Key.Down ? 1 : -1);
        EnsureSelectionVisible();
        return true;
    }

    internal void ClearFocus() { HasFocus = false; _pointerCaptured = false; }
    private void RefreshLayout()
    {
        if (_store is not null && _formatter is not null) Prepare(Bounds, _store, _formatter);
    }

    private void EnsureSelectionVisible()
    {
        if (_store is null || Layout is null) return;
        var card = Layout.Cards.FirstOrDefault(c => c.Report.Sequence == _store.KeyboardSelection);
        if (card is null) return;
        float top = card.Header.Top - _store.ScrollOffset, bottom = card.Header.Bottom - _store.ScrollOffset;
        if (top < Layout.Viewport.Top) _store.ScrollOffset = card.Header.Top - Layout.Viewport.Top;
        else if (bottom > Layout.Viewport.Bottom) _store.ScrollOffset = card.Header.Bottom - Layout.Viewport.Bottom;
        _store.ScrollOffset = Math.Clamp(_store.ScrollOffset, 0, Layout.MaxScroll);
    }

    private static void DrawFit(SKCanvas canvas, SKBitmap bitmap, SKRect bounds)
    {
        float scale = Math.Min(bounds.Width / bitmap.Width, bounds.Height / bitmap.Height);
        float w = bitmap.Width * scale, h = bitmap.Height * scale;
        canvas.DrawBitmap(bitmap, new SKRect(bounds.MidX - w / 2, bounds.MidY - h / 2, bounds.MidX + w / 2, bounds.MidY + h / 2));
    }

    private void DrawRole(SKCanvas canvas, SKRect bounds, BattleReportReporterRole role, SKColor accent)
    {
        _line.Color = accent;
        float r = Math.Min(18, Math.Min(bounds.Width, bounds.Height) / 3), x = bounds.MidX, y = bounds.MidY;
        canvas.DrawCircle(x, y, r, _line);
        if (role == BattleReportReporterRole.ScannerOperator)
        { canvas.DrawCircle(x, y, r / 2, _line); canvas.DrawLine(x, y, x + r, y - r, _line); }
        else if (role == BattleReportReporterRole.Captain)
        { canvas.DrawLine(x - r / 2, y + r / 2, x, y - r / 2, _line); canvas.DrawLine(x, y - r / 2, x + r / 2, y + r / 2, _line); }
        else
        { canvas.DrawLine(x, y - r, x, y + r, _line); canvas.DrawLine(x - r / 2, y, x + r / 2, y, _line); }
    }

    public void Dispose() { _title.Dispose(); _body.Dispose(); _fill.Dispose(); _line.Dispose(); }
}
