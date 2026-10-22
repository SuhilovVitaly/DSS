using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>Own authoritative module health; journal effects use real presentation time.</summary>
internal sealed class ModuleDiagram
{
    internal sealed record Tile(string Id, string Name, int Armor, int ArmorMax, int Structure,
        int StructureMax, bool Critical, bool Destroyed, double DisabledRealSeconds, int? HitDamage);
    internal IReadOnlyList<Tile> Tiles { get; private set; } = [];
    internal int DestroyedCount => Tiles.Count(t => t.Destroyed);
    internal float PreferredHeight => 26 + 56 * ((Tiles.Count + 2) / 3);
    private Guid? _session;
    private ulong _sequence;
    private long _cursor;
    private bool _initialized;
    private double _realMs;
    private readonly Dictionary<string, (int Damage, double Started)> _hits = new(StringComparer.Ordinal);
    private ImmutableArray<InstalledModuleSnapshot> _lastModules;
    private double _lastMotion, _lastMultiplier;

    internal void Update(Guid? session, ulong sequence, string owner, ImmutableArray<InstalledModuleSnapshot> modules,
        ImmutableArray<CombatJournalEntry> journal, double motionMs, double multiplier, double realMs)
    {
        bool baseline = !_initialized || _session != session || sequence < _sequence;
        if (!baseline && sequence == _sequence && modules == _lastModules && motionMs == _lastMotion &&
            multiplier == _lastMultiplier && _hits.Count == 0) return;
        _lastModules = modules; _lastMotion = motionMs; _lastMultiplier = multiplier;
        if (baseline) { _hits.Clear(); _cursor = journal.IsDefaultOrEmpty ? 0 : journal.Max(e => e.EventId); }
        _initialized = true; _session = session; _sequence = sequence; _realMs = realMs;
        if (!journal.IsDefaultOrEmpty)
            foreach (var entry in journal.Where(e => e.EventId > _cursor).OrderBy(e => e.EventId))
            {
                if (entry.TargetObjectId == owner && entry.HitModuleId is { } id && entry.Damage is > 0)
                    _hits[id] = (entry.Damage.Value, realMs);
                _cursor = entry.EventId;
            }
        foreach (var id in _hits.Where(p => realMs - p.Value.Started >= 800).Select(p => p.Key).ToArray()) _hits.Remove(id);
        Tiles = modules.IsDefaultOrEmpty ? [] : modules.OrderBy(m => m.Position).ThenBy(m => m.ModuleId, StringComparer.Ordinal)
            .Select(m => new Tile(m.ModuleId, Localization.CombatModuleName(m.ModuleTypeId, m.DisplayName), m.ArmorCurrent, m.ArmorMax, m.StructureCurrent,
                m.StructureMax, m.IsCritical, m.IsDestroyed,
                Math.Max(0, (m.DisabledUntilMotionTimeMs ?? motionMs) - motionMs) / 1000 / (multiplier > 0 ? multiplier : .25),
                _hits.TryGetValue(m.ModuleId, out var hit) ? hit.Damage : null)).ToArray();
    }

    internal void Render(SKCanvas canvas, SKRect rect)
    {
        using var text = new SKPaint { Color = SKColors.White, TextSize = 10, IsAntialias = true };
        using var fill = new SKPaint { Color = new SKColor(8, 35, 50) };
        using var border = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        canvas.DrawText($"{GameSessionScreen.CombatText("DestroyedModules", "Уничтожено модулей")}: {DestroyedCount}/3", rect.Left + 4, rect.Top + 17, text);
        float width = rect.Width / 3;
        for (int i = 0; i < Tiles.Count; i++)
        {
            var tile = Tiles[i];
            var box = new SKRect(rect.Left + i % 3 * width + 3, rect.Top + 26 + i / 3 * 56,
                rect.Left + (i % 3 + 1) * width - 3, rect.Top + 78 + i / 3 * 56);
            bool flash = _hits.TryGetValue(tile.Id, out var hit) && (int)((_realMs - hit.Started) / 125) % 2 == 0;
            fill.Color = flash ? new SKColor(110, 35, 20) : new SKColor(8, 35, 50);
            canvas.DrawRect(box, fill);
            border.Color = tile.Destroyed ? SKColors.Gray : tile.Critical ? SKColors.Orange : SKColors.Cyan;
            border.StrokeWidth = tile.Critical ? 2 : 1;
            canvas.DrawRect(box, border);
            canvas.Save(); canvas.ClipRect(new SKRect(box.Left + 3, box.Top, box.Right - 3, box.Bottom));
            canvas.DrawText(tile.Name, box.Left + 4, box.Top + 11, text);
            canvas.DrawText($"{GameSessionScreen.CombatText("ArmorShort", "")} {tile.Armor}/{tile.ArmorMax}  {GameSessionScreen.CombatText("StructureShort", "")} {tile.Structure}/{tile.StructureMax}", box.Left + 4, box.Top + 23, text);
            string status = tile.Destroyed ? GameSessionScreen.CombatText("Destroyed", "Уничтожено") :
                tile.DisabledRealSeconds > 0 ? $"{GameSessionScreen.CombatText("Disabled", "Отключён")} {tile.DisabledRealSeconds:0.0} {GameSessionScreen.CombatText("Seconds", "")}" : tile.Critical ? "!" : "";
            canvas.DrawText(status, box.Left + 4, box.Top + 35, text);
            if (tile.HitDamage is { } damage) canvas.DrawText($"−{damage}, {tile.Name}", box.Left + 4, box.Top + 47, text);
            canvas.Restore();
        }
    }
}
