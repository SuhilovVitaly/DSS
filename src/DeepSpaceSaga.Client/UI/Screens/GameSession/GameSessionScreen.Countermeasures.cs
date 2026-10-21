using DeepSpaceSaga.Contracts;
using SkiaSharp;
namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal sealed record WeaponRangeRing(string ModuleId, string Kind, double RangeKm, SKPoint Center, float RadiusPx);
    internal readonly List<WeaponRangeRing> WeaponRangeRings = new();
    private void DrawDefenseAnnotations(SKCanvas canvas)
    {
        WeaponRangeRings.Clear();
        using var line = new SKPaint { Color = CombatSettings.DefenseRange, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        DrawHoveredWeaponRanges(canvas, line);
    }

    private void DrawHoveredWeaponRanges(SKCanvas canvas, SKPaint paint)
    {
        if (FindPlayerShip(_renderStates) is not { } player || player.Source.IsDestroyed) return;
        var (x, y) = _camera.WorldToScreen(player.Pose.X, player.Pose.Y, _viewportW, _viewportH);
        var modules = _buffer.Presented?.Snapshot.InstalledModules ?? default;
        if (modules.IsDefault) return;
        static bool IsPointWeapon(InstalledModuleSnapshot module) =>
            module.LauncherCombat?.Profiles is { IsDefaultOrEmpty: false } || module.Defense?.Profiles is { IsDefaultOrEmpty: false };
        _commandsPanel.InitializePointWeaponRanges(_buffer.PresentationSessionId, modules.Where(IsPointWeapon).Select(m => m.ModuleId));
        foreach (var module in modules)
        {
            if (IsPointWeapon(module))
            {
                if (!_commandsPanel.IsWeaponRangeVisible(module.ModuleId)) continue;
                if (module.LauncherCombat?.RangeKm is { } pointRange) Ring(module.ModuleId, "torpedo", pointRange, false, true);
                else if (module.Defense is { } pointDefense) Ring(module.ModuleId, "counter", pointDefense.RangeKm, false, true);
                continue;
            }

        }

        void Ring(string module, string kind, double range, bool dashed, bool point = false)
        {
            double pixels = range * 10 * _camera.PixelsPerWorldUnit;
            if (!double.IsFinite(pixels) || pixels <= 0 || pixels > float.MaxValue) return;
            using var effect = dashed ? SKPathEffect.CreateDash([6, 4], 0) : null;
            paint.Color = CombatSettings.RangeColor(kind);
            paint.StrokeWidth = point ? (float)CombatSettings.PointCombat.LineWidthPx : 1;
            paint.PathEffect = effect;
            canvas.DrawCircle(x, y, (float)pixels, paint);
            paint.PathEffect = null;
            WeaponRangeRings.Add(new(module, kind, range, new(x, y), (float)pixels));
        }
    }

    private void DrawCountermeasureResults(SKCanvas canvas, BufferedSnapshot? buffered) =>
        _combatEffects.ReceiveJournal(buffered?.Snapshot.CombatJournal ?? default, buffered?.ReceivedAtTimestamp ?? _timestampProvider());
}
