using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal sealed record PointThreat(ObjectMotionSnapshot Motion, BlastThreatResult Assessment)
    {
        internal string Id => Motion.ObjectId;
        internal double ClosestKm => double.Hypot(Assessment.ClosestOffsetXKm, Assessment.ClosestOffsetYKm);
    }
    internal sealed record EvadeInputPreview(string ThreatId, EvadeSide Side, double ExitPhysicalSeconds,
        double ExitRealSeconds, bool CanEscape, double DirectionX, double DirectionY, double MaxSpeedKmS);
    internal IReadOnlyList<PointThreat> PointThreats { get; private set; } = [];
    internal string? CurrentThreatId { get; private set; }
    internal EvadeInputPreview? CurrentEvadePreview { get; private set; }
    internal string? LastManeuverCommandId { get; private set; }
    internal string? ManeuverFeedback { get; private set; }
    internal double? DisplayedBaseCourse { get; private set; }
    internal string? EngineDisabledFeedback { get; private set; }
    internal SKRect EngineDisabledIconBounds { get; private set; }
    private (Key Key, EvadeSide Side, string ThreatId)? _heldEvade;
    private (string Owner, string Module, TurnDirection Direction)? _heldTurn;
    private readonly HashSet<Key> _heldFullSpeed = [];
    private Guid? _maneuverSession;
    private bool _maneuverInitialized;
    private SimulationSpeed _maneuverLastSpeed;
    private Task? _maneuverSendTask;

    internal static IReadOnlyList<PointThreat> AssessPointThreats(ObjectMotionSnapshot player, IEnumerable<ObjectMotionSnapshot> visible)
    {
        double angle = player.Direction * Math.PI / 180;
        return visible.Where(o => !o.IsDestroyed && o.ObservedProjectile is { Kind: ProjectileKind.Torpedo }).Select(o =>
        {
            var flight = o.ObservedProjectile!;
            return new PointThreat(o, BlastThreatMath.Evaluate(player.X, player.Y, player.SpeedKmS * Math.Sin(angle), -player.SpeedKmS * Math.Cos(angle),
                o.X, o.Y, flight.DirectionX, flight.DirectionY, flight.SpeedKmS, flight.BlastRadiusKm));
        }).OrderBy(t => t.Assessment.EntryAfterSeconds ?? double.PositiveInfinity)
            .ThenBy(t => t.Id, StringComparer.Ordinal).ToArray();
    }

    internal static EvadeInputPreview PreviewEvade(ObjectMotionSnapshot player, PointThreat threat, EvadeSide side, double multiplier)
    {
        var flight = threat.Motion.ObservedProjectile!;
        var direction = Unit(flight);
        double nx = side == EvadeSide.Left ? direction.Y : -direction.Y;
        double ny = side == EvadeSide.Left ? -direction.X : direction.X;
        double offset = ((player.X - threat.Motion.X) * nx + (player.Y - threat.Motion.Y) * ny) / 10;
        double speed = player.MaxSpeedKmS ?? 0;
        double seconds = speed > 0 ? Math.Max(0, (flight.BlastRadiusKm + .5 - offset) / speed) : double.PositiveInfinity;
        bool safe = double.IsFinite(seconds) && seconds <= threat.Assessment.ClosestAfterSeconds + .5 && (!threat.Assessment.IsThreat ||
            threat.Assessment.EntryAfterSeconds is { } deadline && seconds <= deadline);
        return new(threat.Id, side, seconds, seconds / multiplier, safe, nx, ny, speed);
    }

    private InstalledModuleSnapshot? ManeuverEngine() => _buffer.Presented?.Snapshot.InstalledModules is { IsDefaultOrEmpty: false } modules
        ? modules.FirstOrDefault(m => m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal)) : null;
    private bool ManeuverEngineReady(InstalledModuleSnapshot? engine) => engine is { IsDestroyed: false, PowerState: "On", OperationalState: "Ready" } &&
        (engine.StructureMax > 0 ? engine.StructureCurrent > 0 : engine.StructurePoints > 0) &&
        !(engine.DisabledUntilMotionTimeMs is { } until && until > PointAimTime);
    private bool UsesCombatTurnInput => _buffer.Presented?.Snapshot.Encounter?.State == CombatEncounterState.Active || PointThreats.Count > 0;

    private void RefreshManeuverInput()
    {
        var session = _buffer.PresentationSessionId;
        if (!_maneuverInitialized || session != _maneuverSession)
        {
            _heldTurn = null; _heldEvade = null; _heldFullSpeed.Clear(); CurrentThreatId = null;
            CurrentEvadePreview = null; LastManeuverCommandId = null; ManeuverFeedback = null;
            _maneuverSession = session; _maneuverInitialized = true; _maneuverLastSpeed = _buffer.CurrentSpeed;
            _ = _maneuverSendTask?.Exception; _maneuverSendTask = null;
        }
        if (_maneuverLastSpeed != SimulationSpeed.Speed0 && _buffer.CurrentSpeed == SimulationSpeed.Speed0) CancelManeuverInput();
        _maneuverLastSpeed = _buffer.CurrentSpeed;
        if (_maneuverSendTask is { IsFaulted: true } or { IsCanceled: true })
        { _ = _maneuverSendTask.Exception; ManeuverFeedback = PointLaunchReason("send_failed"); _maneuverSendTask = null; }
        if (LastManeuverCommandId is { } id && _buffer.FindCommandResult(id) is { Status: CommandResultStatus.Rejected or CommandResultStatus.Failed } rejected)
            ManeuverFeedback = PointLaunchReason(rejected.ReasonCode ?? "command_rejected");
        if (_buffer.Presented?.Snapshot is not { ActiveDialogue: null } snapshot || FindPlayerShip(_renderStates) is not { } player ||
            snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated)
        { CancelManeuverInput(); PointThreats = []; return; }
        PointThreats = AssessPointThreats(player.Pose.ToSnapshot(), _renderStates.Select(s => s.Pose.ToSnapshot()));
        if (_heldEvade is { } held && !PointThreats.Any(t => t.Id == held.ThreatId)) _heldEvade = null;
        if (CurrentThreatId is null || !PointThreats.Any(t => t.Id == CurrentThreatId)) CurrentThreatId = PointThreats.FirstOrDefault()?.Id;
        CurrentEvadePreview = null;
        if (_heldEvade is { } preview && PointThreats.FirstOrDefault(t => t.Id == preview.ThreatId) is { } threat)
        {
            double multiplier = _buffer.LatestPrediction?.CurrentSpeed.MotionTimeMultiplier() ?? .25;
            var pose = player.Pose.ToSnapshot();
            if (!ManeuverEngineReady(ManeuverEngine())) pose = pose with { MaxSpeedKmS = null };
            CurrentEvadePreview = PreviewEvade(pose, threat, preview.Side, multiplier > 0 ? multiplier : .25);
        }
    }

    private void SelectPointThreat(string? id)
    {
        RefreshManeuverInput();
        if (id is null || !PointThreats.Any(t => t.Id == id)) return;
        CurrentThreatId = id;
        if (_heldEvade is { } held) _heldEvade = (held.Key, held.Side, id);
        RefreshManeuverInput();
    }

    private bool HandleManeuverKey(Key key)
    {
        RefreshManeuverInput();
        if (key == CombatInputs.Cancel || key == Key.Escape)
        {
            bool preview = _heldEvade is not null;
            CancelManeuverInput();
            if (preview) { CancelPointAim(); return true; }
            return false;
        }
        if (key == Key.Space) { CancelManeuverInput(); return false; }
        if (IsCtrlDown) return false;
        if (key == CombatInputs.NextThreat && PointThreats.Count > 0)
        {
            int index = PointThreats.ToList().FindIndex(t => t.Id == CurrentThreatId);
            SelectPointThreat(PointThreats[(index + 1) % PointThreats.Count].Id); return true;
        }
        if (key == CombatInputs.EvadeLeft || key == CombatInputs.EvadeRight)
        {
            if (CurrentThreatId is { } threat) _heldEvade = (key, key == CombatInputs.EvadeLeft ? EvadeSide.Left : EvadeSide.Right, threat);
            RefreshManeuverInput(); return true;
        }
        if (CombatInputs.FullSpeed.Contains(key))
        {
            bool first = _heldFullSpeed.Count == 0;
            _heldFullSpeed.Add(key);
            if (first && ManeuverEngineReady(ManeuverEngine()) && CanSendEngineCommand(ShipEngineCommandTypes.Accelerate, _buffer.Presented?.Snapshot))
                SendEngineCommand(ShipEngineCommandTypes.Accelerate);
            return true;
        }
        if (key == CombatInputs.TurnLeft || key == CombatInputs.TurnRight)
        {
            var direction = key == CombatInputs.TurnLeft ? TurnDirection.Left : TurnDirection.Right;
            if (UsesCombatTurnInput)
            {
                if (_heldTurn?.Direction != direction && ManeuverEngineReady(ManeuverEngine()) && _handle is not null &&
                    _buffer.Presented?.Snapshot.PlayerShipObjectId is { } owner && ManeuverEngine() is { } engine)
                {
                    _heldEvade = null; CurrentEvadePreview = null;
                    _heldTurn = (owner, engine.ModuleId, direction); SendHeldTurn(owner, engine.ModuleId, direction, true);
                }
            }
            else
            {
                string command = direction == TurnDirection.Left ? ShipEngineCommandTypes.TurnLeftStep : ShipEngineCommandTypes.TurnRightStep;
                if (CanSendEngineCommand(command, _buffer.Presented?.Snapshot)) SendEngineCommand(command);
            }
            return true;
        }
        return UsesCombatTurnInput && key is Key.Left or Key.Right;
    }

    private void ReleaseManeuverKey(Key key)
    {
        _heldFullSpeed.Remove(key);
        if (_heldTurn is { } turn && (key == CombatInputs.TurnLeft && turn.Direction == TurnDirection.Left || key == CombatInputs.TurnRight && turn.Direction == TurnDirection.Right))
        { _heldTurn = null; SendHeldTurn(turn.Owner, turn.Module, turn.Direction, false); }
        if (_heldEvade is not { } evade || evade.Key != key) return;
        _heldEvade = null; CurrentEvadePreview = null;
        RefreshManeuverInput();
        if (_handle is null || !ManeuverEngineReady(ManeuverEngine()) || ManeuverEngine() is not { } engine ||
            _buffer.Presented?.Snapshot is not { ActiveDialogue: null } snapshot || snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated ||
            !PointThreats.Any(t => t.Id == evade.ThreatId) || snapshot.PlayerShipObjectId is not { } owner) return;
        try
        {
            _maneuverSendTask = _handle.SendEvadeAsync(owner, engine.ModuleId, evade.ThreatId, evade.Side, out var id).AsTask();
            LastManeuverCommandId = id; ManeuverFeedback = CombatText("Sending", "Sending...");
        }
        catch (Exception) { ManeuverFeedback = PointLaunchReason("send_failed"); }
    }

    private void SendHeldTurn(string owner, string module, TurnDirection direction, bool active)
    {
        if (_handle is null || _maneuverSession != _buffer.PresentationSessionId) return;
        try
        {
            _maneuverSendTask = _handle.SendTurnHoldAsync(owner, module, direction, active, out var id).AsTask();
            LastManeuverCommandId = id;
        }
        catch (Exception) { ManeuverFeedback = PointLaunchReason("send_failed"); }
    }
    private void CancelManeuverInput()
    {
        _heldEvade = null; CurrentEvadePreview = null; _heldFullSpeed.Clear();
        if (_heldTurn is not { } turn) return;
        _heldTurn = null; SendHeldTurn(turn.Owner, turn.Module, turn.Direction, false);
    }

    private void DrawManeuverInput(SKCanvas canvas)
    {
        DisplayedBaseCourse = null; EngineDisabledFeedback = null; EngineDisabledIconBounds = SKRect.Empty;
        RefreshManeuverInput();
        if (FindPlayerShip(_renderStates) is not { } player) return;
        if (!UsesCombatTurnInput && player.Source.Evasion is null &&
            !(ManeuverEngine()?.DisabledUntilMotionTimeMs is { } disabled && disabled > PointAimTime)) return;
        var styles = CombatSettings.PointCombat;
        var center = _camera.WorldToScreen(player.Pose.X, player.Pose.Y, _viewportW, _viewportH);
        double baseCourse = player.Source.Evasion?.BaseMotion.Direction ?? player.Pose.Direction;
        DisplayedBaseCourse = baseCourse;
        double angle = baseCourse * Math.PI / 180;
        float length = (float)styles.ReticleRadiusPx * 4;
        using var line = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = (float)styles.LineWidthPx, Color = styles.OwnPath };
        float ex = center.X + length * (float)Math.Sin(angle), ey = center.Y - length * (float)Math.Cos(angle);
        canvas.DrawLine(center.X, center.Y, ex, ey, line);
        float dx = (float)Math.Sin(angle), dy = -(float)Math.Cos(angle), head = (float)styles.ReticleRadiusPx;
        canvas.DrawLine(ex, ey, ex - dx * head - dy * head / 2, ey - dy * head + dx * head / 2, line);
        canvas.DrawLine(ex, ey, ex - dx * head + dy * head / 2, ey - dy * head - dx * head / 2, line);
        using var text = new SKPaint { IsAntialias = true, TextSize = (float)styles.TextSizePx, Color = styles.ForeignProfileText };
        if (CurrentEvadePreview is { } preview)
        {
            string label = preview.CanEscape ? CombatText("Maneuver.ExitIn", "Exit in") + " " + preview.ExitRealSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " " + CombatText("Seconds", "s")
                : CombatText("Maneuver.TooLate", "Cannot escape in time");
            line.Color = preview.CanEscape ? styles.Safe : styles.Danger;
            if (double.IsFinite(preview.ExitPhysicalSeconds))
            {
                double distance = preview.MaxSpeedKmS * 10 * preview.ExitPhysicalSeconds;
                var end = _camera.WorldToScreen(player.Pose.X + preview.DirectionX * distance, player.Pose.Y + preview.DirectionY * distance, _viewportW, _viewportH);
                canvas.DrawLine(center.X, center.Y, end.X, end.Y, line);
            }
            DrawPointAimLabel(canvas, label, new(center.X, center.Y), text);
        }
        if (ManeuverEngine()?.DisabledUntilMotionTimeMs is { } until && until > PointAimTime)
        {
            double multiplier = _buffer.LatestPrediction?.CurrentSpeed.MotionTimeMultiplier() ?? .25;
            string label = CombatText("Maneuver.EngineDisabled", "Engine disabled") + " " +
                ((until - PointAimTime) / 1000 / (multiplier > 0 ? multiplier : .25)).ToString("0.0", CultureInfo.InvariantCulture) + " " + CombatText("Seconds", "s");
            EngineDisabledFeedback = label;
            line.Color = styles.Danger;
            float mark = (float)styles.ReticleRadiusPx;
            float iconX = center.X - length, iconY = center.Y + length;
            EngineDisabledIconBounds = new(iconX - mark, iconY - mark, iconX + mark, iconY + mark);
            canvas.DrawCircle(iconX, iconY, mark, line);
            canvas.DrawLine(iconX - mark / 2, iconY - mark / 2, iconX + mark / 2, iconY + mark / 2, line);
            canvas.DrawLine(iconX - mark / 2, iconY + mark / 2, iconX + mark / 2, iconY - mark / 2, line);
            DrawPointAimLabel(canvas, label, new(center.X, center.Y + length + text.TextSize * 3), text);
        }
    }
}
