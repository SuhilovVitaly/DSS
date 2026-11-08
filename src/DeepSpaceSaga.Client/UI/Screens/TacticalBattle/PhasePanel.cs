using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
    private TacticalBattleFrame _frame = null!;
    private float _offset, _db;
    private readonly Dictionary<string, SKRect> _buttons = [];
    internal SKRect Bounds { get; private set; }
    internal SKRect Action { get; private set; }
    internal string Phase => _frame is null ? "" : _frame.Salvo is { InFlight: true } ? "Salvo" : _frame.Salvo is not null ? "Results" : _frame.Battle.Phase == TacticalBattlePhase.Over ? "Outcome" : _frame.Battle.Phase == TacticalBattlePhase.Fire ? "Fire" : "Move";
    private SKRect R(float x0, float y0, float x1, float y1) => new(x0 + _offset, y0, x1 + _offset, y1);
    internal SKRect ButtonBounds(string key) => _buttons.GetValueOrDefault(key);
    internal string? HitButton(SKPoint point) => _frame is null || _frame.Pending ? null : _buttons.FirstOrDefault(b => b.Value.Contains(point)).Key;
    internal void Draw(SKCanvas c, TacticalBattleFrame f, BattleLayout layout)
    {
        _frame = f; _offset = f.Panel.Right - 1274; _buttons.Clear();
        _db = Phase switch { "Salvo" => 262, "Results" => 246, "Outcome" => 238, _ => 250 };
        Bounds = R(1040, 6, 1274, _db + 120); Action = R(1040, _db + 28, 1274, _db + 120);
        layout.Rects["phase"] = Bounds; layout.Rects["action"] = Action; layout.Rects["details"] = R(1048, 68, 1266, _db + 24);
        c.Save(); c.Translate(_offset, 0);
        try
        {
            PanelBox(c, new(1040, 6, 1274, _db + 120));
            int turn = f.Salvo is { Turn: > 0 } s ? s.Turn : f.Salvo?.Flights.FirstOrDefault()?.Shot.Turn ?? f.Battle.Turn;
            string heading = U("Turn", turn), phase = U(Phase);
            T(c, heading, 1050, 28, 14, Text, true);
            float px = 1050 + Measure(heading, 14, true) + 8;
            Tag(c, new(px, 12, px + Measure(phase, 12.5f, true) + 16, 33), phase, 12.5f, true);
            int Count(bool own) => f.Battle.Units.Count(u => u.Kind == TacticalUnitKind.Drone && (u.Side == f.Battle.PlayerSide) == own && u.State != TacticalUnitState.Destroyed);
            int Loss(bool own) => f.Battle.Losses.Where(l => (l.Side == f.Battle.PlayerSide) == own).Sum(l => l.Count);
            string summary = U("Summary", f.Battle.ScenarioName, Count(true), Count(false), Loss(true), Loss(false));
            T(c, summary, 1050, 50, FitSize(summary, 10.5f, 214), Muted);
            Line(c, 1048, 1266, 59); Line(c, 1048, 1266, _db + 27);
            switch (Phase)
            {
                case "Move": DrawMove(c); break;
                case "Fire": DrawFire(c); break;
                case "Salvo": case "Results": DrawSalvo(c); break;
                case "Outcome": DrawOutcome(c); break;
            }
        }
        finally { c.Restore(); }
    }
    private void Head(SKCanvas c, string title, string tag)
    { T(c, title, 1050, 84, FitSize(title, 15, 214, true), Text, true); Tag(c, new(1048, 92, 1266, 110), U(tag)); }
    private void ButtonAt(SKCanvas c, string id, SKRect rect, string text, bool big = false)
    { _buttons[id] = R(rect.Left, rect.Top, rect.Right, rect.Bottom); if (Recording is { } layout) layout.Rects["button:" + id] = _buttons[id]; Button(c, rect, text, big); }
    private void StandardAction(SKCanvas c, string title, string status, string primary)
    {
        float y = _db + 28;
        T(c, title, 1050, y + 20, FitSize(title, 14, 214, true), Text, true);
        BoxText(c, new(1048, y + 28, 1266, y + 48), _frame.Status.Length > 0 ? _frame.Status : status);
        ButtonAt(c, "primary", new(1048, y + 56, 1266, _db + 112), primary, true);
    }
    partial void DrawMove(SKCanvas c);
    partial void DrawFire(SKCanvas c);
    partial void DrawSalvo(SKCanvas c);
    partial void DrawOutcome(SKCanvas c);
}
