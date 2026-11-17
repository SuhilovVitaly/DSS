using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public sealed partial class TacticalBattleScreen
{
    private BattleLootWindow? _lootWindow;
    internal BattleLootWindow? LootWindow => _lootWindow;
    private bool ObserveLoot()
    {
        var campaign = Flow.Session.DemoCampaign;
        if (campaign?.Rewards?.Loot is not { Phase: LootPhases.Open } loot || loot.BattleId != Flow.State?.BattleId) return false;
        _lootWindow ??= new(new BattleLootPresentation(command => Flow.Session.ExecuteDemoCampaignAsync(command).AsTask()));
        _lootWindow.Presentation.Observe(loot, campaign.Revision);
        return _lootWindow.Presentation.State is not null;
    }

    private ScreenEvent RequestLootExit()
    {
        if (_lootWindow is null || Flow.Pending) return ScreenEvent.None;
        Flow.ConfirmLootDiscard = false;
        return _lootWindow.RequestExit() ? ScreenEvent.CloseTacticalBattle : ScreenEvent.None;
    }

    private ScreenEvent LootClick(SKPoint point, MouseButton button)
    {
        if (_lootWindow is null) return ScreenEvent.None;
        if (!_lootWindow.ConfirmDiscard && _logTab.OnMouseDown(point.X, point.Y, button)) return ScreenEvent.None;
        if (button == MouseButton.Right)
        {
            if (!_lootWindow.ConfirmDiscard && !_lootWindow.Bounds.Contains(point)) _camera.BeginPan(point.X, point.Y);
            return ScreenEvent.None;
        }
        if (button != MouseButton.Left) return ScreenEvent.None;
        if (_lootWindow.Click(point, Flow.Pending))
        {
            Flow.ConfirmLootDiscard = _lootWindow.ConfirmDiscard;
            return ScreenEvent.CloseTacticalBattle;
        }
        if (!_lootWindow.ConfirmDiscard && !_lootWindow.Bounds.Contains(point) && !_lootWindow.JettisonBounds.Contains(point) && !_lootWindow.RetryBounds.Contains(point) && !Flow.Pending && !_lootWindow.Presentation.Pending &&
            _lootWindow.Presentation.State is { } loot && TacticalBattleMapRenderer.PickLootSource(View, loot, point) is { } id)
            _lootWindow.Select(id);
        return ScreenEvent.None;
    }
}
