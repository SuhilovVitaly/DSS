using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly CombatReportStore _combatReportStore = new();
    private readonly CombatReportPanel _combatReportPanel = new();
    private readonly CombatReportAssets _combatReportAssets = new();
    private readonly CombatReportFormatter _combatReportFormatter = new();
    internal CombatReportStore ReportStore => _combatReportStore;
    internal CombatReportPanel ReportPanel => _combatReportPanel;

    // Called by SkiaWindow on the owning UI thread before screen.Render.
    internal void PrepareCombatReports()
    {
        _combatReportStore.Receive(_buffer.DrainCombatReports());
        var expanded = _combatReportStore.Reports.FirstOrDefault(r => r.Sequence == _combatReportStore.ExpandedSequence);
        _combatReportAssets.Prepare(expanded is null ? [] : [expanded]);
    }

    private void RenderCombatReports(SKCanvas canvas)
    {
        // Geometry follows the actual ObjectInfo bottom computed this frame. No resource work here.
        _combatReportPanel.Prepare(CombatReportsBounds, _combatReportStore, _combatReportFormatter);
        _combatReportPanel.Render(canvas, _combatReportAssets);
    }
}
