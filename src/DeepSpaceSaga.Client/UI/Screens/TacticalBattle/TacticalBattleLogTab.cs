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
    private int? _lastHiddenTurn;
    internal ScrollTextPanel Panel => _panel;

    internal static IEnumerable<TacticalBattleLogEvent> Visible(TacticalBattleFrame frame)
    {
        int? hidden = HiddenTurn(frame);
        return hidden is { } turn
            ? frame.Battle.Log.TakeWhile(e => e.Turn < turn || e.Turn == turn && e.Phase != TacticalBattlePhase.Fire)
            : frame.Battle.Log;
    }
    private static int? HiddenTurn(TacticalBattleFrame frame) =>
        frame.Presentation == TacticalBattlePresentation.SalvoFlight && !frame.Battle.Shots.IsDefaultOrEmpty
            ? frame.Battle.Shots[0].Turn : null;

    internal void Draw(SKCanvas canvas, TacticalBattleFrame frame, SKRect bounds)
    {
        _panel.Layout(bounds, frame.UiScale);
        int? hidden = HiddenTurn(frame);
        if (_last != frame.Battle.Log || _lastHiddenTurn != hidden)
        {
            _last = frame.Battle.Log;
            _lastHiddenTurn = hidden;
            _panel.SetText(string.Join("\n", Visible(frame).Select(BattleLogFormatter.Format)));
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
