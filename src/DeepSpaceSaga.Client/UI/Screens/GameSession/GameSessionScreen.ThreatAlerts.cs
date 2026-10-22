using System.Diagnostics;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal enum ObservedThreatClass { Dangerous, Near, Miss }
    internal sealed record ThreatVisual(ObservedThreatClass Class, SKColor Color, float WidthPx, float[]? Dash);
    internal readonly record struct AlertSignal(CombatAlertKind Kind, double RealTimeMs);
    internal readonly Dictionary<string, ThreatVisual> ObservedThreatVisuals = new(StringComparer.Ordinal);
    internal readonly List<AlertSignal> RecentAlertSignals = [];
    internal readonly Dictionary<string, SKPoint> ThreatOffscreenIndicators = new(StringComparer.Ordinal);
    internal int LaunchFlashCount => _launchFlashes.Count;
    private readonly Action<CombatAlertCue>? _alertAudio;
    private readonly Action? _activateAlertAudio, _deactivateAlertAudio;
    private readonly List<(CombatJournalEntry Entry, double Started)> _launchFlashes = [];
    private readonly Dictionary<string, string> _publicActorRelations = new(StringComparer.Ordinal);
    private HashSet<string> _previousDanger = new(StringComparer.Ordinal), _previousUrgent = new(StringComparer.Ordinal);
    private Guid? _alertSession;
    private bool _alertsInitialized, _alertsInactive, _pointAlertsEnabled;
    private ulong _alertSnapshotSequence;
    private long _alertJournalCursor;
    private double _alertBaselineAt;
    private double? _lastLaunchSignalAt, _lastWarningSignalAt;

    private readonly ReloadReportNotificationCursor _reloadNotifications = new();
    private void NotifyReloadReports(IEnumerable<BattleReportSnapshot> reports)
    {
        foreach (var report in _reloadNotifications.Take(_buffer.PresentationSessionId, reports))
        {
            if (_alertsInactive || !CombatSettings.ThreatAlerts.SoundEnabled) continue;
            RecentAlertSignals.Add(new(CombatAlertKind.TorpedoReloadCompleted, _timestampProvider() * (1000d / Stopwatch.Frequency)));
            _alertAudio?.Invoke(new(CombatAlertKind.TorpedoReloadCompleted, 880, 100, 1, 0, 0));
        }
    }

    internal static ThreatVisual ClassifyThreat(PointThreat threat, PointCombatVisualSettings point, ThreatAlertVisualSettings alerts)
    {
        var kind = threat.Assessment.IsThreat ? ObservedThreatClass.Dangerous :
            threat.ClosestKm <= threat.Motion.ObservedProjectile!.BlastRadiusKm + 3 ? ObservedThreatClass.Near : ObservedThreatClass.Miss;
        return kind switch
        {
            ObservedThreatClass.Dangerous => new(kind, point.Danger, (float)(point.LineWidthPx * alerts.DangerWidthMultiplier), null),
            ObservedThreatClass.Near => new(kind, point.Threat, (float)(point.LineWidthPx * alerts.NearWidthMultiplier), [(float)alerts.NearDashLengthPx, (float)alerts.NearDashGapPx]),
            _ => new(kind, point.Safe, (float)(point.LineWidthPx * alerts.MissWidthMultiplier), [(float)alerts.MissDotLengthPx, (float)alerts.MissDashGapPx])
        };
    }

    private void UpdateThreatAlerts(long timestamp)
    {
        ObservedThreatVisuals.Clear(); ThreatOffscreenIndicators.Clear();
        var snapshot = _buffer.Presented?.Snapshot;
        _pointAlertsEnabled = snapshot?.Encounter is not null || _renderStates.Any(s => s.Source.ObservedProjectile is { Kind: ProjectileKind.Torpedo });
        if (!_pointAlertsEnabled || snapshot is null || FindPlayerShip(_renderStates) is not { } player) return;
        double now = timestamp * (1000d / Stopwatch.Frequency);
        var settings = CombatSettings.ThreatAlerts;
        var threats = AssessPointThreats(player.Pose.ToSnapshot(), _renderStates
            .Where(s => !s.Source.IsDestroyed && s.Source.ObservedProjectile is { Kind: ProjectileKind.Torpedo }).Select(s => s.Pose.ToSnapshot()));
        double multiplier = _buffer.LatestPrediction?.CurrentSpeed.MotionTimeMultiplier() ?? .25;
        if (multiplier <= 0) multiplier = .25;
        foreach (var threat in threats) ObservedThreatVisuals[threat.Id] = ClassifyThreat(threat, CombatSettings.PointCombat, settings);
        var danger = threats.Where(t => t.Assessment.IsThreat).Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var urgent = threats.Where(t => t.Assessment.EntryAfterSeconds is { } entry && entry / multiplier <= 3).Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        long newest = snapshot.CombatJournal.IsDefaultOrEmpty ? 0 : snapshot.CombatJournal.Max(e => e.EventId);
        if (!_alertsInitialized || _alertSession != _buffer.PresentationSessionId || snapshot.SnapshotSequence < _alertSnapshotSequence)
        {
            _alertsInitialized = true; _alertSession = _buffer.PresentationSessionId; _alertBaselineAt = now;
            _alertJournalCursor = newest; _lastLaunchSignalAt = _lastWarningSignalAt = null;
            _launchFlashes.Clear(); RecentAlertSignals.Clear(); _publicActorRelations.Clear();
            _previousDanger = danger; _previousUrgent = urgent;
            RememberPublicActorRelations(); _alertSnapshotSequence = snapshot.SnapshotSequence;
            return; // baseline is state, not a replay of past launch/warning effects
        }
        _alertSnapshotSequence = snapshot.SnapshotSequence;
        RememberPublicActorRelations();
        bool audible = !_alertsInactive && snapshot.ActiveDialogue is null && snapshot.EffectiveOutcome.State != SessionOutcomeState.Defeated && _buffer.CurrentSpeed != SimulationSpeed.Speed0;
        if (!snapshot.CombatJournal.IsDefaultOrEmpty)
            foreach (var entry in snapshot.CombatJournal.Where(e => e.EventId > _alertJournalCursor).OrderBy(e => e.EventId))
            {
                if (_alertsInactive || entry.Type != CombatEventType.Launch || entry.ProjectileKind != ProjectileKind.Torpedo ||
                    entry.ActorObjectId == snapshot.PlayerShipObjectId || _publicActorRelations.GetValueOrDefault(entry.ActorObjectId) != PlayerRelation.Enemy) continue;
                if (EmitThreatSignal(CombatAlertKind.EnemyLaunch, now, audible)) _launchFlashes.Add((entry, now));
            }
        _alertJournalCursor = Math.Max(_alertJournalCursor, newest);
        _launchFlashes.RemoveAll(f => now - f.Started >= settings.LaunchFlashDurationRealMs);
        if (audible && danger.Count > 0)
        {
            bool isUrgent = urgent.Count > 0;
            bool newRisk = isUrgent ? urgent.Except(_previousUrgent).Any() : danger.Except(_previousDanger).Any();
            double interval = isUrgent ? settings.UrgentSignalIntervalRealMs : settings.DangerSignalIntervalRealMs;
            if (newRisk || now - (_lastWarningSignalAt ?? _alertBaselineAt) >= interval)
                EmitThreatSignal(isUrgent ? CombatAlertKind.Urgent : CombatAlertKind.Danger, now, true);
        }
        _previousDanger = danger; _previousUrgent = urgent;

        void RememberPublicActorRelations()
        {
            foreach (var row in _renderStates)
                if (row.Source.RelationToPlayer is { } relation && (relation == PlayerRelation.Enemy || _publicActorRelations.ContainsKey(row.Source.ObjectId)))
                    _publicActorRelations[row.Source.ObjectId] = relation;
            if (_publicActorRelations.Count > 4096)
                foreach (string id in _publicActorRelations.Keys.Order(StringComparer.Ordinal).Take(_publicActorRelations.Count - 4096).ToArray()) _publicActorRelations.Remove(id);
        }
    }

    private bool EmitThreatSignal(CombatAlertKind kind, double now, bool audible)
    {
        var settings = CombatSettings.ThreatAlerts;
        double? previous = kind == CombatAlertKind.EnemyLaunch ? _lastLaunchSignalAt : _lastWarningSignalAt;
        if (previous is { } last && now - last < settings.MinimumSignalIntervalRealMs) return false;
        if (kind == CombatAlertKind.EnemyLaunch) _lastLaunchSignalAt = now; else _lastWarningSignalAt = now;
        RecentAlertSignals.Add(new(kind, now));
        if (RecentAlertSignals.Count > 64) RecentAlertSignals.RemoveAt(0);
        if (audible && settings.SoundEnabled && _alertAudio is not null)
        {
            double frequency = kind switch { CombatAlertKind.EnemyLaunch => settings.LaunchToneHz, CombatAlertKind.Danger => settings.DangerToneHz, _ => settings.UrgentToneHz };
            var cue = new CombatAlertCue(kind, (int)Math.Round(frequency), (int)Math.Round(settings.ToneDurationRealMs),
                kind == CombatAlertKind.Urgent ? (int)settings.UrgentPulseCount : 1, (int)Math.Round(settings.UrgentPulseGapRealMs),
                (int)Math.Ceiling(settings.MinimumSignalIntervalRealMs));
            try { _alertAudio(cue); }
            catch (Exception error)
            {
                try { InterfaceLog.Write("Combat audio cue failed: " + error.Message); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        return true;
    }

    private void DrawObservedThreatMarker(SKCanvas canvas, ObjectRenderState state, float x, float y, float radius, long timestamp)
    {
        if (!ObservedThreatVisuals.TryGetValue(state.Source.ObjectId, out var visual))
        { DrawCombatMarker(canvas, state, x, y); return; }
        using var fill = new SKPaint { Color = visual.Color, IsAntialias = true };
        canvas.DrawCircle(x, y, radius, fill);
        using var outline = new SKPaint { Color = visual.Color, Style = SKPaintStyle.Stroke, StrokeWidth = visual.WidthPx, IsAntialias = true };
        using var dash = visual.Dash is { } pattern ? SKPathEffect.CreateDash(pattern, 0) : null;
        outline.PathEffect = dash;
        float halo = radius + 3;
        if (visual.Class == ObservedThreatClass.Dangerous)
        {
            double now = timestamp * (1000d / Stopwatch.Frequency);
            halo += (float)(2 * (.5 + .5 * Math.Sin(now * 2 * Math.PI / CombatSettings.PointCombat.ThreatPulsePeriodRealMs)));
        }
        canvas.DrawCircle(x, y, halo, outline);
    }

    private void DrawThreatAlertOverlays(SKCanvas canvas, long timestamp)
    {
        if (!_pointAlertsEnabled) return;
        double now = timestamp * (1000d / Stopwatch.Frequency);
        var styles = CombatSettings.PointCombat; var settings = CombatSettings.ThreatAlerts;
        using var line = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = (float)styles.LineWidthPx * 2, Color = styles.Danger };
        foreach (var flash in _launchFlashes)
        {
            double progress = Math.Clamp((now - flash.Started) / settings.LaunchFlashDurationRealMs, 0, 1);
            var point = _camera.WorldToScreen(flash.Entry.X, flash.Entry.Y, _viewportW, _viewportH);
            line.Color = styles.Danger.WithAlpha((byte)Math.Round(255 * (1 - progress)));
            canvas.DrawCircle(point.X, point.Y, (float)(styles.ReticleRadiusPx * (2 + 2 * progress)), line);
        }
        var safe = AvailableMapRect(); safe.Inflate(-(float)settings.OffscreenArrowRadiusPx * 2, -(float)settings.OffscreenArrowRadiusPx * 2);
        if (safe.IsEmpty) return;
        using var fill = new SKPaint { Color = styles.Danger, IsAntialias = true };
        foreach (var row in _renderStates.Where(s => !s.Source.IsDestroyed && s.Source.RelationToPlayer == PlayerRelation.Enemy && s.Source.RenderObjectType == SpaceObjectType.NpcShip))
        {
            var position = _camera.WorldToScreen(row.Pose.X, row.Pose.Y, _viewportW, _viewportH);
            if (position.X >= 0 && position.X <= _viewportW && position.Y >= 0 && position.Y <= _viewportH) continue;
            double dx = position.X - safe.MidX, dy = position.Y - safe.MidY;
            double factor = Math.Min(safe.Width / 2 / Math.Max(.001, Math.Abs(dx)), safe.Height / 2 / Math.Max(.001, Math.Abs(dy)));
            float x = safe.MidX + (float)(dx * factor), y = safe.MidY + (float)(dy * factor);
            ThreatOffscreenIndicators[row.Source.ObjectId] = new(x, y);
            float radius = (float)settings.OffscreenArrowRadiusPx;
            canvas.Save(); canvas.Translate(x, y); canvas.RotateDegrees((float)(Math.Atan2(dy, dx) * 180 / Math.PI));
            using var path = new SKPath(); path.MoveTo(radius, 0); path.LineTo(-radius, -radius / 2); path.LineTo(-radius, radius / 2); path.Close();
            canvas.DrawPath(path, fill); canvas.Restore();
        }
    }
}

internal sealed class ReloadReportNotificationCursor
{
    private Guid? _session;
    private long _sequence;
    internal IEnumerable<BattleReportSnapshot> Take(Guid? session, IEnumerable<BattleReportSnapshot> reports)
    {
        if (_session != session) { _session = session; _sequence = 0; }
        foreach (var report in reports.OrderBy(r => r.Sequence))
        {
            if (report.Sequence <= _sequence) continue;
            _sequence = report.Sequence;
            if (report.Kind == BattleReportKind.PlayerTorpedoReloadCompleted) yield return report;
        }
    }
}
