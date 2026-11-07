using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class TacticalBattleMovePanel
{
    private readonly TacticalBattleOrders _orders;
    private readonly Slider _speed = new(0, 1, 0);
    private readonly Slider _turn = new(0, 1, 0);
    private readonly ShortcutButton _straight = new("");
    private readonly ShortcutButton _suggest = new("");
    private readonly ShortcutButton _aiAll = new("");
    private readonly List<(string Id, SKRect Bounds)> _cards = [];
    private SKRect _bounds;
    private float _scale = 1, _scroll, _maximumScroll;
    private bool _enabled;
    internal SKRect SpeedBounds => _speed.Bounds;
    internal SKRect TurnBounds => _turn.Bounds;
    internal SKRect AiAllBounds => _aiAll.Bounds;
    internal IReadOnlyList<(string Id, SKRect Bounds)> Cards => _cards;

    internal TacticalBattleMovePanel(TacticalBattleOrders orders)
    {
        _orders = orders;
        _speed.Changed += value => _orders.SetSpeed(value);
        _turn.Changed += value => _orders.SetTurn(value);
    }

    internal void Draw(SKCanvas canvas, TacticalMovementFrame frame, SKRect bounds, float scale, bool enabled)
    {
        _bounds = bounds;
        _scale = scale;
        _enabled = enabled;
        _cards.Clear();
        if (bounds.IsEmpty) return;
        canvas.Save();
        canvas.ClipRect(bounds);
        if (frame.SelectedOrder is not { } selected)
        {
            ControlVisuals.Label(canvas, L("NoAirDrones"), new(bounds.Left, bounds.Top, bounds.Right, bounds.Top + 26 * scale), true, scale);
            canvas.Restore();
            return;
        }

        float fullHeight = (274 + 28 * frame.Orders.Length) * scale;
        _maximumScroll = (float)PyMath.Max(0, fullHeight - bounds.Height);
        _scroll = (float)PyMath.Clamp(_scroll, 0, _maximumScroll);
        float left = bounds.Left, right = bounds.Right - 6 * scale;
        float top = bounds.Top - _scroll;
        float width = (float)PyMath.Max(0, right - left);
        Line(Format("MoveSelected", selected.Unit.Id, L(selected.Touched ? "MoveEdited" : "MoveSuggested")), top, true);
        Line(L("Speed") + " " + BattleFormat.Number(selected.Order.Speed, 1), top + 25 * scale, enabled);
        _speed.Bounds = SKRect.Create(left + 6 * scale, top + 47 * scale, (float)PyMath.Max(0, width - 12 * scale), 22 * scale);
        _speed.SetRange(selected.MinimumSpeed, selected.MaximumSpeed);
        _speed.SetValue(selected.Order.Speed);
        _speed.Enabled = enabled;
        _speed.Draw(canvas, scale);
        Line(L("TurnAngle") + " " + BattleFormat.Signed(selected.Order.Turn, 1) + "°", top + 72 * scale, enabled);
        _turn.Bounds = SKRect.Create(left + 6 * scale, top + 94 * scale, _speed.Bounds.Width, 22 * scale);
        _turn.SetRange(-selected.MaximumTurn, selected.MaximumTurn);
        _turn.SetValue(selected.Order.Turn);
        _turn.Enabled = enabled;
        _turn.Draw(canvas, scale);
        Wrapped(Format("MoveLimits", BattleFormat.Number(selected.MinimumSpeed, 1), BattleFormat.Number(selected.MaximumSpeed, 1),
            BattleFormat.Degrees(selected.MaximumTurn, 1), BattleFormat.Degrees(selected.TurnAtMinimum, 1)), top + 117 * scale);

        float half = (width - 6 * scale) * 0.5f;
        Button(_straight, "MoveStraight", SKRect.Create(left, top + 162 * scale, half, 27 * scale));
        Button(_suggest, "MoveAiSuggest", SKRect.Create(right - half, top + 162 * scale, half, 27 * scale));
        Button(_aiAll, "MoveAiAll", SKRect.Create(left, top + 193 * scale, width, 27 * scale));
        _aiAll.Tooltip = L("AiAllHelp");
        Wrapped(L("MoveKeys"), top + 221 * scale);
        for (int i = 0; i < frame.Orders.Length; i++)
        {
            var row = frame.Orders[i];
            var rect = SKRect.Create(left, top + (268 + i * 28) * scale, width, 25 * scale);
            _cards.Add((row.Unit.Id, rect));
            ControlVisuals.Frame(canvas, rect, row.Unit.Id == frame.Selected, enabled, scale);
            ControlVisuals.Label(canvas, row.Unit.Id + " · " + L(row.Touched ? "MoveEdited" : "MoveSuggested"),
                new(rect.Left + 8 * scale, rect.Top, rect.Right, rect.Bottom), true, scale);
        }
        if (_maximumScroll > 0)
        {
            double fraction = bounds.Height / (double)fullHeight;
            float thumbHeight = (float)(bounds.Height * fraction);
            float y = bounds.Top + (bounds.Height - thumbHeight) * (float)(_scroll / (double)_maximumScroll);
            using var paint = new SKPaint { Color = new SKColor(75, 127, 149) };
            canvas.DrawRect(bounds.Right - 3 * scale, y, 2 * scale, thumbHeight, paint);
        }
        canvas.Restore();

        void Line(string text, float y, bool active) =>
            ControlVisuals.Label(canvas, text, new(left, y, right, y + 21 * scale), active, scale);
        void Wrapped(string text, float y)
        {
            using var paint = ControlText.Paint(scale);
            foreach (string line in ControlText.Wrap(text, width, paint).Take(2))
            {
                Line(line, y, true);
                y += 18 * scale;
            }
        }
        void Button(ShortcutButton button, string key, SKRect rect)
        {
            button.Label = L(key); button.Bounds = rect; button.Enabled = enabled;
            button.Draw(canvas, scale);
        }
    }

    internal bool OnMouseDown(float x, float y, MouseButton button)
    {
        if (!_enabled || button != MouseButton.Left || !_bounds.Contains(x, y) || _orders.Selected is null) return false;
        if (_speed.OnMouseDown(x, y, button)) { _orders.SetSpeed(_speed.Value); return true; }
        if (_turn.OnMouseDown(x, y, button)) { _orders.SetTurn(_turn.Value); return true; }
        if (_straight.OnMouseDown(x, y, button)) return _orders.Straight();
        if (_suggest.OnMouseDown(x, y, button)) return _orders.Suggest();
        if (_aiAll.OnMouseDown(x, y, button)) { _orders.ResetAll(); return true; }
        foreach (var card in _cards)
            if (card.Bounds.Contains(x, y)) return _orders.Select(card.Id);
        return false;
    }

    internal bool OnMouseMove(float x, float y)
    {
        bool dragging = _speed.IsDragging || _turn.IsDragging;
        if (!dragging && !_bounds.Contains(x, y))
        {
            _aiAll.OnMouseMove(-1, -1);
            return false;
        }
        return _speed.OnMouseMove(x, y) | _turn.OnMouseMove(x, y) |
            _straight.OnMouseMove(x, y) | _suggest.OnMouseMove(x, y) | _aiAll.OnMouseMove(x, y);
    }
    internal void OnMouseUp(MouseButton button) { _speed.OnMouseUp(button); _turn.OnMouseUp(button); }
    internal bool OnMouseWheel(float x, float y, float delta)
    {
        if (!_bounds.Contains(x, y) || !float.IsFinite(delta)) return false;
        _scroll = (float)PyMath.Clamp(_scroll + delta / 100.0 * 36 * _scale, 0, _maximumScroll);
        return true;
    }
    internal void DrawTooltip(SKCanvas canvas, SKRect viewport) => _aiAll.DrawTooltip(canvas, viewport, _scale);
    internal void ResetInput()
    {
        _speed.ResetInput(); _turn.ResetInput(); _straight.ResetInput(); _suggest.ResetInput(); _aiAll.ResetInput();
    }
    internal void ResetScroll() => _scroll = 0;

    private static string L(string key) => BattleFormat.Localize(key);
    private static string Format(string key, params object[] args) =>
        BattleFormat.Text(string.Format(CultureInfo.InvariantCulture, L(key), args));
}
