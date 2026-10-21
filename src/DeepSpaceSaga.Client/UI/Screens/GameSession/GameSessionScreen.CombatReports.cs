using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly CombatReportStore _combatReportStore = new();
    private readonly CombatReportPanel _combatReportPanel = new();
    private readonly CombatReportAssets _combatReportAssets = new();
    private readonly CombatReportFormatter _combatReportFormatter = new();
    private bool _combatReportDisposed;
    private bool _presentationPrepared;
    private Guid? _combatVisualSession;
    internal bool CombatReportResourcesDisposed => _combatReportDisposed;
    internal CombatReportStore ReportStore => _combatReportStore;
    internal CombatReportPanel ReportPanel => _combatReportPanel;

    // Called by SkiaWindow on the owning UI thread before screen.Render.
    internal void PrepareCombatReports()
    {
        ResetCombatPresentationSession();
        _buffer.PreparePresentation();
        _presentationPrepared = true;
        PrepareCombatForecasts();
        if (_combatReportDisposed) return;
        _combatReportStore.Receive(_buffer.DrainCombatReports());
        var expanded = _combatReportStore.Reports.FirstOrDefault(r => r.Sequence == _combatReportStore.ExpandedSequence);
        _combatReportAssets.Prepare(expanded is null ? [] : [expanded]);
    }

    private void ResetCombatPresentationSession()
    {
        if (_combatVisualSession == _buffer.PresentationSessionId) return;
        _combatVisualSession = _buffer.PresentationSessionId;
        _combatEffects.ResetJournal(_buffer.Presented?.Snapshot.CombatJournal ?? default);
        _combatReportStore.Reset();
        _combatTrajectories.Clear();
    }

    private void RenderCombatReports(SKCanvas canvas)
    {
        if (_combatReportDisposed) return;
        // Geometry follows the actual ObjectInfo bottom computed this frame. No resource work here.
        _combatReportPanel.Prepare(CombatReportsBounds, _combatReportStore, _combatReportFormatter);
        _combatReportPanel.Render(canvas, _combatReportAssets);
    }

    private void InitializeCombatReports()
    {
        if (_handle is not null) _handle.Closing += DisposeCombatReports;
    }

    internal void DisposeCombatReports()
    {
        if (_combatReportDisposed) return;
        _combatReportDisposed = true;
        if (_handle is not null) _handle.Closing -= DisposeCombatReports;
        _combatReportPanel.ClearFocus();
        _combatReportAssets.Dispose();
        _combatReportPanel.Dispose();
        _combatReportStore.Reset();
    }
}
