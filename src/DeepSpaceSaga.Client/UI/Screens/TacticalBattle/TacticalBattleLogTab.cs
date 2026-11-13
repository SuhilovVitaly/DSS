using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class TacticalBattleLogTab
{
    private readonly ScrollTextPanel _panel = new() { AutoScroll = true, StyleRole = "tabs.log" };
    private ImmutableArray<TacticalBattleLogEvent> _last;
    private string? _battleId;
    private string? _title;
    internal SKRect Bounds { get; private set; }
    internal ScrollTextPanel Panel => _panel;

    internal static IEnumerable<TacticalBattleLogEvent> Visible(TacticalBattleFrame frame) => frame.Battle.Log;

    internal static string FormatJournal(TacticalBattleFrame frame)
    {
        var lines = new List<string>();
        foreach (var turn in Visible(frame).GroupBy(entry => entry.Turn))
        {
            var steps = turn.Where(entry => entry.Phase == TacticalBattlePhase.Fire && entry.FireStepIndex.HasValue).ToArray();
            bool inFlight = frame.Salvo is { InFlight: true } salvo &&
                (salvo.Turn == turn.Key || salvo.Turn == 0 && frame.Battle.Turn == turn.Key);
            bool noTargets = steps.Length > 0 && steps.All(NoTargetSkip) &&
                (!inFlight || steps.Length == frame.Battle.FireQueue.Length);
            var phases = turn.ToLookup(entry => entry.Phase);
            var section = new List<string>();
            foreach (var phase in phases)
            {
                var entries = phase.Where(entry => !NoTargetSkip(entry) && entry.Kind != TacticalBattleLogKind.NoTargets);
                var body = entries.Select(BattleLogFormatter.Body).ToList();
                if (noTargets && phase.Key == TacticalBattlePhase.Fire)
                    body.Add(BattleFormat.Localize("Log.AllNoTargets"));
                if (body.Count == 0) continue;
                section.Add("─ " + BattleLogFormatter.Phase(phase.Key));
                section.AddRange(body);
            }
            if (section.Count == 0) continue;
            if (lines.Count > 0) lines.Add("");
            lines.Add("── " + BattleFormat.Text(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                BattleFormat.Localize("Log.Turn"), BattleFormat.Number(turn.Key))) + " ──");
            lines.AddRange(section);
        }
        return string.Join("\n", lines);
    }

    private static bool NoTargetSkip(TacticalBattleLogEvent entry) =>
        entry.Kind == TacticalBattleLogKind.FireSkipped && entry.SkipReason == TacticalFireSkipReason.NoTarget;

    internal void Draw(SKCanvas canvas, TacticalBattleFrame frame, SKRect bounds)
    {
        Bounds = bounds;
        if (BattleDesign.Recording is { } layout) layout.Rects["battle-log"] = bounds;
        BattleDesign.PanelBox(canvas, bounds);
        string title = BattleFormat.Localize("Log.Title");
        BattleDesign.T(canvas, title, bounds.Left + 9, bounds.Top + 17, 12, BattleDesign.Text, true);
        _panel.Layout(new(bounds.Left + 4, bounds.Top + 23, bounds.Right - 4, bounds.Bottom - 4), .68f * frame.UiScale);
        bool newBattle = _battleId != frame.Battle.BattleId;
        if (_last != frame.Battle.Log || newBattle || _title != title)
        {
            _last = frame.Battle.Log;
            _battleId = frame.Battle.BattleId;
            _title = title;
            _panel.SetText(FormatJournal(frame));
            if (newBattle) _panel.ScrollToBottom();
        }
        _panel.Draw(canvas);
    }

    internal bool OnMouseDown(float x, float y, MouseButton button) => _panel.OnMouseDown(x, y, button);
    internal bool OnMouseMove(float x, float y) => _panel.OnMouseMove(x, y);
    internal void OnMouseUp(MouseButton button) => _panel.OnMouseUp(button);
    internal bool OnMouseWheel(float x, float y, float delta) => _panel.OnMouseWheel(x, y, delta);
    internal bool OnKeyDown(Key key) => _panel.OnKeyDown(key);
    internal void ResetInput() { _panel.ResetInput(); _panel.HasFocus = false; }
}
