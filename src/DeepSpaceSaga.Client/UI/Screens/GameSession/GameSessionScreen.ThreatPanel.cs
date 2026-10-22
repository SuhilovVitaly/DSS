using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly ThreatPanel _threatPanel = new();
    internal ThreatPanel ThreatsPanel => _threatPanel;

    internal static IReadOnlyList<ThreatPanel.Row> MakeThreatRows(IReadOnlyList<PointThreat> threats, IReadOnlyList<string> predicted,
        string? selected, double multiplier, bool canCounter, bool canEvade) => threats
        .Where(t => t.ClosestKm <= t.Motion.ObservedProjectile!.BlastRadiusKm + 3)
        .OrderBy(t => t.Assessment.EntryAfterSeconds ?? double.PositiveInfinity).ThenBy(t => t.Id, StringComparer.Ordinal)
        .Select(t => new ThreatPanel.Row(t.Id, t.Motion.ObservedProjectile!.ProfileId, t.Assessment.EntryAfterSeconds / multiplier,
            t.Assessment.IsThreat, predicted.Contains(t.Id, StringComparer.Ordinal), t.Id == selected, canCounter, canEvade)).ToArray();

    private void RenderThreatPanel(SKCanvas canvas)
    {
        RefreshManeuverInput();
        if (FindPlayerShip(_renderStates) is not { } player || _buffer.Presented?.Snapshot is not { ActiveDialogue: null } snapshot ||
            snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated)
        { _threatPanel.Clear(); return; }
        double multiplier = _buffer.LatestPrediction?.CurrentSpeed.MotionTimeMultiplier() ?? .25;
        var predicted = PredictPointInterceptions(_renderStates.Where(s => s.Source.OwnProjectile is not null || s.Source.ObservedProjectile is not null)
            .Select(s => s.Pose.ToSnapshot()).ToArray(), player.Source.ObjectId, PointAimTime);
        bool canCounter = _handle is not null && !snapshot.InstalledModules.IsDefaultOrEmpty && snapshot.InstalledModules.Any(m => !m.IsDestroyed && m.Defense?.Profiles is { IsDefaultOrEmpty: false });
        var rows = MakeThreatRows(PointThreats, predicted, CurrentThreatId, multiplier > 0 ? multiplier : .25,
            canCounter, _handle is not null && ManeuverEngineReady(ManeuverEngine()));
        var free = AvailableMapRect(includeThreatPanel: false, includeEncounterHud: false);
        var logical = new SKRect(free.Left / _uiScale, free.Top / _uiScale, free.Right / _uiScale, free.Bottom / _uiScale);
        _threatPanel.Render(canvas, logical, rows, CombatSettings.PointCombat, CombatInputs);
    }

    private bool HandleThreatPanelClick(float x, float y, MouseButton button)
    {
        float ux = x / _uiScale, uy = y / _uiScale;
        if (!_threatPanel.Bounds.Contains(ux, uy)) return false;
        if (button != MouseButton.Left) return true;
        if (_threatPanel.HitTest(ux, uy) is not { Enabled: true } hit) return true;
        SelectPointThreat(hit.Id);
        if (!PointThreats.Any(t => t.Id == hit.Id)) return true;
        switch (hit.Action)
        {
            case ThreatPanel.Action.Countermissile: EnterPointAim(ProjectileKind.Countermissile); break;
            case ThreatPanel.Action.EvadeLeft: SubmitPointEvade(hit.Id, EvadeSide.Left); break;
            case ThreatPanel.Action.EvadeRight: SubmitPointEvade(hit.Id, EvadeSide.Right); break;
        }
        return true;
    }
}
