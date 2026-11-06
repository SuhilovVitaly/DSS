using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;
using SkinStyle = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class TacticalBattleFirePanel
{
    internal sealed record Choice(string? Id, string Caption);
    private readonly TacticalBattleFireOrders _orders;
    private readonly DropDown<Choice> _targets = new([], c => c.Caption);
    private readonly ShortcutButton _none = new("");
    private readonly List<(string Id, SKRect Bounds)> _cards = [];
    private TacticalTargetGroupSnapshot? _group;
    private SKRect _bounds;
    private float _scroll, _maximumScroll, _scale;
    private bool _enabled;
    internal SKRect NoneBounds => _none.Bounds;
    internal DropDown<Choice> Targets => _targets;

    internal TacticalBattleFirePanel(TacticalBattleFireOrders orders)
    {
        _orders = orders;
        _targets.Changed += item => _orders.Choose(item.Id);
    }

    internal void Draw(SKCanvas canvas, TacticalFirePlan plan, TacticalBattleSnapshot snapshot, SKRect bounds, SKRect viewport, float scale, bool enabled)
    {
        _bounds = bounds;
        _scale = scale;
        _enabled = enabled;
        _cards.Clear();
        if (bounds.IsEmpty || plan.Selected is not { } selected) return;
        using var layout = new SKPaint { Typeface = MenuStyle.TypefaceRegular, TextSize = 14 * scale };
        float width = (float)PyMath.Max(1, bounds.Width - 6 * scale);
        string summaryText = Format("FireSummary", string.Join(" · ", plan.Rows.Select(r =>
            r.Drone.Id + " " + BattleFormat.Text("→") + " " + (r.TargetId ?? L("FireNoTarget")))));
        int summaryRows = ControlText.Wrap(summaryText, width, layout).Length;
        using var text = ControlText.Paint(scale, "orders.fire.text", summaryText);
        text.TextSize = Math.Min(text.TextSize, 17 * scale);
        var summary = ControlText.Wrap(summaryText, width, text);
        float fullHeight = (145 + 28 * plan.Rows.Length + 18 * summaryRows) * scale;
        _maximumScroll = (float)PyMath.Max(0, fullHeight - bounds.Height);
        _scroll = (float)PyMath.Clamp(_scroll, 0, _maximumScroll);
        float top = bounds.Top - _scroll, x = bounds.Left;
        canvas.Save();
        canvas.ClipRect(bounds);
        if (SkinStyle.HasBoxStyle("orders.fire"))
            SkinStyle.Box(canvas, "orders.fire", bounds, SKColors.Transparent, null, scale);
        Label(Format("FireSelected", selected.Drone.Id), top, 24, "orders.fire.title");
        if (!ReferenceEquals(_group, selected.Group))
        {
            _group = selected.Group;
            _targets.SetItems(_group.List.IsEmpty ? [] :
                new[] { new Choice(null, L("FireNone")) }.Concat(_group.List.Select(candidate =>
                    new Choice(candidate.TargetId, CandidateCaption(candidate, snapshot)))));
        }
        if (!_targets.Items.IsEmpty)
        {
            for (int i = 0; i < _targets.Items.Length; i++)
                if (_targets.Items[i].Id == selected.TargetId) { _targets.SelectIndex(i); break; }
            var targetBounds = SKRect.Create(x, top + 27 * scale, width, 33 * scale);
            _targets.Layout(targetBounds, viewport, scale);
            _targets.Enabled = enabled && bounds.Contains(targetBounds);
            if (!_targets.Enabled) _targets.ResetInput();
            using var role = SkinStyle.EnterRole("orders.fire.dropdown");
            _targets.Draw(canvas);
        }
        else
        {
            _targets.Enabled = false;
            float y = top + 26 * scale;
            string caption = UnavailableText(selected);
            using var paint = ControlText.Paint(scale, "orders.fire.unavailable", caption);
            paint.TextSize = Math.Min(paint.TextSize, 17 * scale);
            foreach (var line in ControlText.Wrap(caption, width, paint).Take(2))
            {
                ControlVisuals.DrawLabel(canvas, line, SKRect.Create(x, y, width, 18 * scale), paint);
                y += 18 * scale;
            }
        }
        _none.Bounds = SKRect.Create(x, top + 70 * scale, width, 29 * scale);
        _none.Label = L("FireNoneAll");
        _none.Enabled = enabled && plan.Rows.Any(r => r.TargetId is not null);
        using (SkinStyle.EnterRole("orders.fire.button.none"))
            _none.Draw(canvas, scale);
        for (int i = 0; i < plan.Rows.Length; i++)
        {
            var row = plan.Rows[i];
            var rect = SKRect.Create(x, top + (108 + 28 * i) * scale, width, 25 * scale);
            _cards.Add((row.Drone.Id, rect));
            bool selectedCard = row.Drone.Id == plan.SelectedDroneId;
            string role = selectedCard ? "orders.fire.card.selected" : "orders.fire.card.normal";
            if (row.TargetId is null) role += ".no-target";
            ControlVisuals.Frame(canvas, rect, selectedCard, enabled, scale, role);
            ControlVisuals.Label(canvas, BattleFormat.Text(row.Drone.Id + " → " + (row.TargetId ?? L("FireNoTarget"))),
                new(rect.Left + 8 * scale, rect.Top, rect.Right, rect.Bottom), true, scale, role: role);
        }
        float summaryY = top + (115 + 28 * plan.Rows.Length) * scale;
        foreach (var line in summary.Take(summaryRows))
        {
            ControlVisuals.DrawLabel(canvas, line, SKRect.Create(x, summaryY, width, 18 * scale), text);
            summaryY += 18 * scale;
        }
        if (_maximumScroll > 0)
        {
            float thumb = (float)(bounds.Height * (bounds.Height / (double)fullHeight));
            float y = bounds.Top + (bounds.Height - thumb) * (float)(_scroll / (double)_maximumScroll);
            if (SkinStyle.HasBoxStyle("orders.fire.scroll.track"))
                SkinStyle.Box(canvas, "orders.fire.scroll.track",
                    SKRect.Create(bounds.Right - 3 * scale, bounds.Top, 2 * scale, bounds.Height), SKColors.Transparent, null, scale, antialias: false);
            SkinStyle.Box(canvas, "orders.fire.scroll.thumb",
                SKRect.Create(bounds.Right - 3 * scale, y, 2 * scale, thumb), new(75, 127, 149), null, scale, antialias: false);
        }
        canvas.Restore();

        void Label(string caption, float y, float height, string role) =>
            ControlVisuals.Label(canvas, caption, SKRect.Create(x, y, width, height * scale), true, scale, role: role);
    }

    internal bool OnMouseDown(float x, float y, MouseButton button)
    {
        if (!_enabled) return false;
        if (_targets.IsOpen) return _targets.OnMouseDown(x, y, button);
        if (button != MouseButton.Left || !_bounds.Contains(x, y)) return false;
        if (_targets.OnMouseDown(x, y, button)) return true;
        if (_none.OnMouseDown(x, y, button)) { _orders.ClearAll(); return true; }
        foreach (var card in _cards)
            if (card.Bounds.Contains(x, y)) return _orders.Select(card.Id);
        return false;
    }

    internal bool OnKeyDown(Key key) => _targets.IsOpen && _targets.OnKeyDown(key);
    internal bool OnMouseMove(float x, float y) =>
        (_targets.IsOpen || _bounds.Contains(x, y) ? _targets.OnMouseMove(x, y) : _targets.OnMouseMove(-1, -1)) |
        _none.OnMouseMove(x, y);

    internal bool OnMouseWheel(float x, float y, float delta)
    {
        if (_targets.OnMouseWheel(x, y, delta)) return true;
        if (!_bounds.Contains(x, y) || !float.IsFinite(delta)) return false;
        _scroll = (float)PyMath.Clamp(_scroll + delta / 100.0 * 36 * _scale, 0, _maximumScroll);
        return true;
    }

    internal void DrawOverlay(SKCanvas canvas, SKRect viewport)
    {
        using (SkinStyle.EnterRole("orders.fire.dropdown"))
            _targets.DrawPopup(canvas);
        using (SkinStyle.EnterRole("orders.fire.tooltip"))
            _targets.DrawTooltip(canvas, viewport);
    }

    internal void ResetInput() { _targets.ResetInput(); _none.ResetInput(); }
    internal void ResetScroll() => _scroll = 0;

    internal static string UnavailableText(TacticalFirePlanRow row) => row.Drone.Jammed ? L("FireJammed")
        : row.Drone.Build is { } build
            ? Format("FireEmptySector", BattleFormat.Degrees(2 * build.ArcDegrees), BattleFormat.Number(build.Range))
            : L("FireNone");

    internal static string CandidateCaption(TacticalTargetCandidateSnapshot candidate, TacticalBattleSnapshot snapshot)
    {
        var unit = snapshot.Units.FirstOrDefault(u => u.Id == candidate.TargetId);
        var marks = new List<string>();
        if (unit?.Kind == TacticalUnitKind.Carrier) marks.Add(L("FireCarrier"));
        if (candidate.Rear) marks.Add(Format("FireRearMark", BattleFormat.Probability(candidate.RearKillChance)));
        return Format("FireTargetCaption", candidate.TargetId, BattleFormat.Number(candidate.Distance, 1),
            BattleFormat.Degrees(candidate.Angle, 1), BattleFormat.Probability(candidate.Chance))
            + (marks.Count == 0 ? "" : " · " + string.Join(" · ", marks));
    }

    private static string L(string key) => BattleFormat.Localize(key);
    private static string Format(string key, params object[] args) =>
        BattleFormat.Text(string.Format(CultureInfo.InvariantCulture, L(key), args));
}
