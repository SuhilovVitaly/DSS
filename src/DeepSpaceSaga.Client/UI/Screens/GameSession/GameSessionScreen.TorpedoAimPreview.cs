using System.Diagnostics;
using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal sealed record AimShadow(string ObjectId, double X, double Y, double Coverage, bool Friendly);
    internal sealed record TorpedoAimPreview(double StartX, double StartY, double X, double Y, double RadiusKm,
        double CoreFraction, double? SigmaKm, double PhysicalSeconds, double RealSeconds, bool OutOfRange,
        IReadOnlyList<AimShadow> Shadows)
    {
        internal bool FriendlyFire => Shadows.Any(s => s.Friendly && s.Coverage > 0);
    }
    internal TorpedoAimPreview? CurrentTorpedoAimPreview { get; private set; }
    internal SKRect TorpedoAimLabelBounds { get; private set; }
    private (Guid? Session, PointAimState Aim, double X, double Y)? _friendlyConfirmation;
    private long _friendlyConfirmationAt;

    private void ResetPointFriendlyConfirmation() { _friendlyConfirmation = null; }

    // O4 deliberately uses only the currently displayed pose and velocity. No route, AI or future snapshot.
    internal static AimShadow LinearAimShadow(ObjectMotionSnapshot visible, double x, double y,
        double seconds, double radiusKm, double coreFraction, bool friendly)
    {
        double direction = visible.Direction * Math.PI / 180;
        double sx = visible.X + visible.SpeedKmS * 10 * Math.Sin(direction) * seconds;
        double sy = visible.Y - visible.SpeedKmS * 10 * Math.Cos(direction) * seconds;
        double k = BlastMath.Falloff(double.Hypot(sx - x, sy - y) / 10, radiusKm, coreFraction);
        return new(visible.ObjectId, sx, sy, k, friendly);
    }

    private TorpedoAimPreview? BuildTorpedoAimPreview(double x, double y)
    {
        if (PointAim is not { Kind: ProjectileKind.Torpedo } aim ||
            PointAimModule(aim.ModuleId)?.LauncherCombat is not { } launcher ||
            PointProfiles(PointAimModule(aim.ModuleId)!).FirstOrDefault(p => p.ProfileId == aim.ProfileId) is not { } profile ||
            FindPlayerShip(_renderStates) is not { } player || !double.IsFinite(x) || !double.IsFinite(y) ||
            launcher.CoreFraction is not > 0 or >= 1) return null;
        double distanceKm = double.Hypot(x - player.Pose.X, y - player.Pose.Y) / 10;
        if (!double.IsFinite(distanceKm) || profile.SpeedKmS <= 0) return null;
        double seconds = distanceKm / profile.SpeedKmS;
        double multiplier = _buffer.LatestPrediction?.CurrentSpeed.MotionTimeMultiplier() ?? .25;
        var shadows = new List<AimShadow>();
        foreach (var state in _renderStates)
        {
            if (state.Source.IsDestroyed || state.Source.RenderObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip)) continue;
            bool friendly = state.IsPlayerShip || state.Source.RelationToPlayer == PlayerRelation.Friend;
            if (!friendly && state.Source.RelationToPlayer != PlayerRelation.Enemy) continue;
            shadows.Add(LinearAimShadow(state.Pose.ToSnapshot(), x, y, seconds, profile.BlastRadiusKm, launcher.CoreFraction.Value, friendly));
        }
        return new(player.Pose.X, player.Pose.Y, x, y, profile.BlastRadiusKm, launcher.CoreFraction.Value,
            launcher.EffectiveScatterPercent is { } scatter ? distanceKm * scatter : null,
            seconds, seconds / (multiplier > 0 ? multiplier : .25), launcher.RangeKm is not > 0 || distanceKm > launcher.RangeKm, shadows);
    }

    private bool ConfirmPointFriendlyFire(PointAimState aim, double x, double y)
    {
        var preview = BuildTorpedoAimPreview(x, y);
        if (preview is null) { _pointErrors[aim.ModuleId] = CommandReasonCodes.ModuleUnavailable; return false; }
        if (preview is not { FriendlyFire: true }) { ResetPointFriendlyConfirmation(); return true; }
        var key = (_buffer.PresentationSessionId, aim, x, y);
        long now = _timestampProvider();
        double elapsed = (now - _friendlyConfirmationAt) / (double)Stopwatch.Frequency;
        if (_friendlyConfirmation == key && elapsed is >= 0 and <= 1)
        {
            ResetPointFriendlyConfirmation(); return true;
        }
        _friendlyConfirmation = key; _friendlyConfirmationAt = now;
        _pointErrors[aim.ModuleId] = "friendly_fire_confirmation";
        return false;
    }

    private void DrawTorpedoAimPreview(SKCanvas canvas)
    {
        CurrentTorpedoAimPreview = null;
        TorpedoAimLabelBounds = SKRect.Empty;
        if (!_hasMousePosition || IsClickOnUiPanel(_uiMouseX, _uiMouseY)) return;
        var target = _camera.ScreenToWorld(_mouseX, _mouseY, _viewportW, _viewportH);
        if (BuildTorpedoAimPreview(target.Item1, target.Item2) is not { } preview) return;
        CurrentTorpedoAimPreview = preview;
        if (_friendlyConfirmation is { } confirmation && (confirmation.X != preview.X || confirmation.Y != preview.Y))
            ResetPointFriendlyConfirmation();
        var styles = CombatSettings.PointCombat;
        var start = _camera.WorldToScreen(preview.StartX, preview.StartY, _viewportW, _viewportH);
        var end = _camera.WorldToScreen(preview.X, preview.Y, _viewportW, _viewportH);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = (float)styles.LineWidthPx };
        using var dash = SKPathEffect.CreateDash([(float)styles.DashLengthPx, (float)styles.DashGapPx], 0);
        paint.Color = preview.OutOfRange ? styles.Danger : styles.AimReticle;
        paint.PathEffect = dash; canvas.DrawLine(start.X, start.Y, end.X, end.Y, paint); paint.PathEffect = null;
        double radius = preview.RadiusKm * 10 * _camera.PixelsPerWorldUnit;
        if (double.IsFinite(radius) && radius < float.MaxValue)
            DrawPointBlast(canvas, new(end.X, end.Y), radius, preview.CoreFraction, ProjectileKind.Torpedo, .65, paint);
        if (preview.SigmaKm is { } sigma && double.IsFinite(sigma * 10 * _camera.PixelsPerWorldUnit))
        {
            paint.Color = styles.AimReticle; paint.PathEffect = dash;
            canvas.DrawCircle(end.X, end.Y, (float)(sigma * 10 * _camera.PixelsPerWorldUnit), paint); paint.PathEffect = null;
        }
        foreach (var shadow in preview.Shadows)
        {
            if (shadow.Friendly) continue;
            var point = _camera.WorldToScreen(shadow.X, shadow.Y, _viewportW, _viewportH);
            paint.Color = shadow.Coverage > 0 ? styles.Danger : styles.TargetShadow;
            canvas.DrawCircle(point.X, point.Y, (float)styles.ReticleRadiusPx, paint);
            canvas.DrawLine(point.X - 4, point.Y, point.X + 4, point.Y, paint);
        }
        using var text = new SKPaint
        {
            IsAntialias = true,
            TextSize = (float)styles.TextSizePx,
            Color = preview.OutOfRange || preview.FriendlyFire ? styles.Danger : styles.ForeignProfileText
        };
        string number(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
        string label = $"R {number(preview.RadiusKm)} km · σ {(preview.SigmaKm is { } s ? number(s) + " km" : "?")} · ETA {number(preview.RealSeconds)} {CombatText("Seconds", "s")}";
        if (preview.OutOfRange) label += " · " + CombatText("Aim.OutOfRange", "Out of range");
        if (preview.FriendlyFire) label += " · " + CombatText("Aim.FriendlyFire", "Will hit friendly — click again within 1 s");
        TorpedoAimLabelBounds = DrawPointAimLabel(canvas, label, new(end.X, end.Y), text);
    }

    private SKRect DrawPointAimLabel(SKCanvas canvas, string label, SKPoint anchor, SKPaint text)
    {
        var free = AvailableMapRect();
        float available = Math.Max(1, free.Width - 8);
        var lines = new List<string>();
        string current = "";
        foreach (string word in label.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = current.Length == 0 ? word : current + " " + word;
            if (current.Length > 0 && text.MeasureText(candidate) > available) { lines.Add(current); current = word; }
            else current = candidate;
        }
        if (current.Length > 0) lines.Add(current);
        float width = lines.Count == 0 ? 0 : lines.Max(text.MeasureText);
        float height = lines.Count * text.TextSize * 1.2f;
        float x = Math.Clamp(anchor.X + (float)CombatSettings.PointCombat.ReticleRadiusPx, free.Left + 4,
            Math.Max(free.Left + 4, free.Right - width - 4));
        float top = Math.Clamp(anchor.Y - height - text.TextSize, free.Top + 4, Math.Max(free.Top + 4, free.Bottom - height - 4));
        for (int i = 0; i < lines.Count; i++) canvas.DrawText(lines[i], x, top + text.TextSize + i * text.TextSize * 1.2f, text);
        return new(x, top, x + width, top + height);
    }
}
