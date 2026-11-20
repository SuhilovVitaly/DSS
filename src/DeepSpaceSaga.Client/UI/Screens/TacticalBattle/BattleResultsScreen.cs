using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class BattleResultsScreen(TacticalBattleFlow flow, float uiScale)
    : TacticalBattleModalScreen(flow, isSetup: false, uiScale)
{
    private SKRect _exitBounds;
    private bool _acknowledgeRequested;
    internal bool ConfirmDiscard { get; private set; }

    public override void Render(SKCanvas canvas, int width, int height)
    {
        canvas.Clear(MapBg);
        float left = width * .1f;
        float top = height * .1f;
        var panel = new SKRect(left, top, width - left, height - top);
        Round(canvas, panel, Box, BoxEdge);
        T(canvas, Localization.Get("TacticalBattle.Results"), panel.Left + 20, panel.Top + 42, 26, Text, true);
        _exitBounds = new(panel.Right - 220, panel.Bottom - 65, panel.Right - 20, panel.Bottom - 20);
        Button(canvas, _exitBounds, Localization.Get("TacticalBattle.ToMap"), pending: Flow.Pending);
        if (ConfirmDiscard)
            T(canvas, Localization.Get("loot.loot_confirm_discard_required"), panel.Left + 20, _exitBounds.MidY, 18, Text);
        if (Flow.Error.Length > 0)
            T(canvas, Flow.Error, panel.Left + 20, _exitBounds.Top - 10, 18, Red);
    }

    private ScreenEvent RequestExit()
    {
        if (Flow.Pending) return ScreenEvent.None;
        if (!ConfirmDiscard && Flow.Session.DemoCampaign?.Rewards?.Loot is { HasUnclaimed: true } loot && loot.BattleId == Flow.State?.BattleId)
        {
            ConfirmDiscard = true;
            return ScreenEvent.None;
        }
        _acknowledgeRequested = true;
        return ScreenEvent.AcknowledgeBattleResults;
    }

    internal bool ConsumeAcknowledgeRequest()
    {
        bool requested = _acknowledgeRequested;
        _acknowledgeRequested = false;
        return requested;
    }

    public override ScreenEvent OnMouseDown(float x, float y, MouseButton button) =>
        button == MouseButton.Left && _exitBounds.Contains(x, y) ? RequestExit() : ScreenEvent.None;
    public override bool OnMouseMove(float x, float y) => _exitBounds.Contains(x, y);
    public override ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;
    public override ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat)
    {
        if (Flow.Pending || repeat || modifiers != KeyModifiers.None) return ScreenEvent.None;
        if (key == Key.Escape) ConfirmDiscard = false;
        return key is Key.Enter or Key.KeypadEnter ? RequestExit() : ScreenEvent.None;
    }
}
