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
    internal ScrollTextPanel Panel => _panel;

    internal static IEnumerable<TacticalBattleLogEvent> Visible(TacticalBattleFrame frame) => frame.Battle.Log;

    internal void Draw(SKCanvas canvas, TacticalBattleFrame frame, SKRect bounds)
    {
        _panel.Layout(bounds, frame.UiScale);
        if (_last != frame.Battle.Log || _battleId != frame.Battle.BattleId)
        {
            _last = frame.Battle.Log;
            _battleId = frame.Battle.BattleId;
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
