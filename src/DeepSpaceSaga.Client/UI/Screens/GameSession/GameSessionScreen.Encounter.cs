using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly CombatSummaryPanel _combatSummaryPanel = new();
    private readonly bool _playInitialCombatStart;
    private bool _combatHudInitialized;
    private int? _summaryViewportHash;
    private SKRect _summaryViewport;
    private Guid? _combatHudSession;
    private ulong _combatHudSequence;
    private CombatEncounterState? _previousEncounterState;
    private long _previousEncounterStart;
    private Task? _encounterAckTask;
    private string? _encounterAckId, _encounterAckError;
    internal CombatSummaryPanel CombatSummary => _combatSummaryPanel;
    internal SKRect CombatBannerRect { get; private set; }
    internal bool CombatBorderVisible { get; private set; }
    internal double CombatElapsedRealSeconds { get; private set; }
    internal string? CombatSpeedTooltip { get; private set; }
    internal string? LastEncounterAcknowledgementCommandId { get; private set; }
    private bool CombatIsActive => _buffer.Presented?.Snapshot.Encounter?.State == CombatEncounterState.Active;
    internal bool IsCombatSpeedEnabled(SimulationSpeed speed) => !CombatIsActive || speed.IsAllowedInCombat();

    private void DrawEncounterHud(SKCanvas canvas, BufferedSnapshot? buffered, SnapshotPrediction? prediction)
    {
        CombatBannerRect = SKRect.Empty; CombatBorderVisible = false;
        if (buffered?.Snapshot is not { Encounter: { } encounter } snapshot) { _combatSummaryPanel.Hide(); return; }
        bool reset = !_combatHudInitialized || _combatHudSession != _buffer.PresentationSessionId || snapshot.SnapshotSequence < _combatHudSequence;
        bool entered = encounter.State == CombatEncounterState.Active &&
            (reset ? !_combatHudInitialized && _playInitialCombatStart : _previousEncounterState != CombatEncounterState.Active || _previousEncounterStart != encounter.StartedAtMotionMs);
        if (reset) { _encounterAckTask = null; _encounterAckId = _encounterAckError = null; LastEncounterAcknowledgementCommandId = null; }
        _combatHudInitialized = true; _combatHudSession = _buffer.PresentationSessionId; _combatHudSequence = snapshot.SnapshotSequence;
        _previousEncounterState = encounter.State; _previousEncounterStart = encounter.StartedAtMotionMs;
        if (entered && !_alertsInactive && snapshot.ActiveDialogue is null && CombatSettings.ThreatAlerts.SoundEnabled && _alertAudio is not null)
        {
            var sound = CombatSettings.ThreatAlerts;
            try { _alertAudio(new(CombatAlertKind.CombatStarted, (int)Math.Round(sound.LaunchToneHz), (int)Math.Round(sound.ToneDurationRealMs), 1, 0)); }
            catch (Exception error)
            {
                try { InterfaceLog.Write("Combat start audio failed: " + error.Message); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        double extra = encounter.State == CombatEncounterState.Active && prediction is not null
            ? Math.Max(0, GetPredictedGameTimeMs(prediction) - snapshot.MotionTimeMs) / 1000d : 0;
        CombatElapsedRealSeconds = Math.Max(0, encounter.DurationSeconds + extra) * 4;
        var map = AvailableMapRect(includeEncounterHud: false);
        // The largest free area can be a shallow strip. A result panel needs both
        // dimensions; prefer another unobstructed rectangle over hiding Continue.
        if (encounter.State == CombatEncounterState.Won && (map.Width < 90 * _uiScale || map.Height < 100 * _uiScale))
        {
            if (_summaryViewportHash != _freeViewportHash)
            {
                _summaryViewport = MapViewGeometry.FreeViewport(_viewportW, _viewportH, _mapObstacles, 90 * _uiScale, 100 * _uiScale);
                _summaryViewportHash = _freeViewportHash;
            }
            map = _summaryViewport;
        }
        var logical = new SKRect(map.Left / _uiScale, map.Top / _uiScale, map.Right / _uiScale, map.Bottom / _uiScale);
        if (encounter.State == CombatEncounterState.Active)
        {
            _combatSummaryPanel.Hide(); CombatBorderVisible = true;
            using var line = new SKPaint { Color = CombatSettings.PointCombat.Danger, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
            canvas.DrawRect(new SKRect(1, 1, _uiViewportW - 1, _uiViewportH - 1), line);
            using var text = new SKPaint { Color = CombatSettings.PointCombat.Danger, TextSize = 16, IsAntialias = true, TextAlign = SKTextAlign.Center };
            string value = $"{CombatText("Battle", "БОЙ")} · {CombatSummaryPanel.Duration(CombatElapsedRealSeconds)}";
            float width = Math.Min(logical.Width, text.MeasureText(value) + 20);
            CombatBannerRect = new(logical.MidX - width / 2, logical.Top + 4, logical.MidX + width / 2, logical.Top + 34);
            using var fill = new SKPaint { Color = new(0, 0, 0, 210) };
            canvas.DrawRect(CombatBannerRect, fill); canvas.Save(); canvas.ClipRect(CombatBannerRect);
            canvas.DrawText(value, CombatBannerRect.MidX, CombatBannerRect.Top + 21, text); canvas.Restore();
        }
        else if (encounter.State == CombatEncounterState.Won)
        {
            if (_encounterAckTask is { IsFaulted: true } or { IsCanceled: true })
            { _ = _encounterAckTask.Exception; _encounterAckTask = null; _encounterAckError = CombatText("CommandRejected", "Команда отклонена"); }
            if (_encounterAckId is { } id && _buffer.FindCommandResult(id) is { Status: CommandResultStatus.Rejected } result)
            { _encounterAckTask = null; _encounterAckId = null; _encounterAckError = PointLaunchReason(result.ReasonCode ?? "command_rejected"); }
            _combatSummaryPanel.Render(canvas, logical, encounter, _handle is not null && _encounterAckTask is null, _encounterAckError);
        }
        else { _combatSummaryPanel.Hide(); _encounterAckTask = null; _encounterAckId = _encounterAckError = null; }
    }

    private bool HandleEncounterClick(float x, float y)
    {
        if (!_combatSummaryPanel.Bounds.Contains(x, y)) return false;
        if (!_combatSummaryPanel.ContinueRect.Contains(x, y) || !_combatSummaryPanel.CanContinue || _encounterAckTask is not null ||
            _handle is null || _buffer.Presented?.Snapshot is not { PlayerShipObjectId: { } player, Encounter.State: CombatEncounterState.Won }) return true;
        try
        {
            _encounterAckError = null;
            _encounterAckTask = _handle.SendCommandAsync(player, string.Empty, CombatEncounterCommandTypes.AcknowledgeResults, out _encounterAckId).AsTask();
            LastEncounterAcknowledgementCommandId = _encounterAckId;
        }
        catch (Exception) { _encounterAckTask = null; _encounterAckError = CombatText("CommandRejected", "Команда отклонена"); }
        return true;
    }

    private void DrawCombatSpeedTooltip(SKCanvas canvas)
    {
        CombatSpeedTooltip = null;
        if (!CombatIsActive || !_hasMousePosition) return;
        int index = HitTestSpeedPanel(_uiMouseX, _uiMouseY);
        if (index < 0 || IsCombatSpeedEnabled(SpeedValues[index])) return;
        CombatSpeedTooltip = CombatText("Reason.combat_speed_locked", "Во время боя доступны пауза и x1");
        using var paint = new SKPaint { Color = XenonStyle.CyanBright, TextSize = 12, IsAntialias = true };
        float width = Math.Min(_uiViewportW - 16, paint.MeasureText(CombatSpeedTooltip) + 12);
        var lines = CombatReportPanelLayout.Wrap(CombatSpeedTooltip, Math.Max(1, width - 12), paint);
        float height = lines.Length * 16 + 8;
        float left = Math.Clamp(_lastSpeedPanelRect.Left, 8, Math.Max(8, _uiViewportW - width - 8));
        float top = Math.Max(0, _lastSpeedPanelRect.Top - height - 4);
        using var fill = new SKPaint { Color = new(0, 0, 0, 240) };
        canvas.DrawRect(new SKRect(left, top, left + width, top + height), fill);
        for (int i = 0; i < lines.Length; i++) canvas.DrawText(lines[i], left + 6, top + 16 + i * 16, paint);
    }
}
