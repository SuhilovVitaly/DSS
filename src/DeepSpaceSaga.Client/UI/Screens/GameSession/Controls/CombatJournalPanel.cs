using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

internal sealed class CombatJournalPanel
{
    internal bool Expanded { get; private set; }
    internal SKRect Bounds { get; private set; }
    internal ImmutableArray<CombatJournalEntry> Entries { get; private set; } = [];
    private int _offset;
    private readonly List<string> _lines = new();
    private ImmutableArray<CombatJournalEntry> _layoutEntries;
    private float _layoutWidth;
    private int _visibleLineCount;
    internal void Render(SKCanvas canvas, float width, float height, ImmutableArray<CombatJournalEntry> entries)
    {
        Entries = entries.IsDefault ? [] : entries;
        float left = Math.Min(384, Math.Max(8, width - 96));
        float w = Expanded ? Math.Min(540, width - left - 8) : Math.Max(80, width - 2 * 384);
        Bounds = new(left, 48, left + w, 74 + (Expanded ? Math.Min(260, Math.Max(100, height - 180)) : 0));
        using var fill = new SKPaint { Color = new SKColor(8, 25, 36, 242) };
        using var text = new SKPaint { Color = new SKColor(160, 230, 245), TextSize = 12, IsAntialias = true };
        canvas.DrawRect(Bounds, fill);
        canvas.Save(); canvas.ClipRect(Bounds);
        canvas.DrawText($"{(Expanded ? "−" : "+")} Журнал · {Entries.Length}", left + 6, 66, text);
        canvas.Restore();
        if (!Expanded) return;
        if (_layoutEntries != Entries || _layoutWidth != w)
        {
            _lines.Clear();
            foreach (var entry in Entries.Reverse())
            {
                foreach (var line in Format(entry))
                {
                    string rest = line;
                    while (rest.Length > 0)
                    {
                        int count = Math.Max(1, (int)text.BreakText(rest, w - 24));
                        _lines.Add(rest[..count]);
                        rest = rest[count..];
                    }
                }
                _lines.Add(string.Empty);
            }
            _layoutEntries = Entries;
            _layoutWidth = w;
        }
        _visibleLineCount = Math.Max(1, (int)((Bounds.Bottom - 6 - 92) / 16) + 1);
        _offset = Math.Clamp(_offset, 0, Math.Max(0, _lines.Count - _visibleLineCount));
        canvas.Save();
        canvas.ClipRect(new SKRect(left + 6, 78, left + w - 6, Bounds.Bottom - 6));
        float y = 92;
        foreach (var line in _lines.Skip(_offset).Take(_visibleLineCount))
        {
            canvas.DrawText(line, left + 10, y, text);
            y += 16;
        }
        canvas.Restore();
    }
    internal bool Contains(float x, float y) => Bounds.Contains(x, y);
    internal bool Click(float x, float y)
    {
        if (!Contains(x, y)) return false;
        if (y <= 74) Expanded = !Expanded;
        return true;
    }
    internal bool Scroll(float x, float y, float delta)
    {
        if (!Expanded || !Contains(x, y)) return false;
        if (delta != 0)
            _offset = Math.Clamp(_offset + (delta > 0 ? 1 : -1), 0, Math.Max(0, _lines.Count - _visibleLineCount));
        return true;
    }
    internal static IReadOnlyList<string> Format(CombatJournalEntry entry)
    {
        string type = entry.Type switch
        {
            CombatEventType.Launch => "Пуск",
            CombatEventType.Intercept => "Перехват",
            CombatEventType.Miss => "Промах",
            CombatEventType.Hit => "Попадание",
            CombatEventType.Destroyed => "Уничтожение",
            CombatEventType.SelfDestruct => "Самоуничтожение",
            _ => "Потеря цели"
        };
        var lines = new List<string> { $"{entry.MotionTimeMs / 1000:0.000} с · {type} · {entry.ProjectileObjectId}",
            $"{entry.ActorObjectId} → {entry.TargetObjectId}" };
        if (entry.ChanceTenths is { } chance) lines.Add($"Шанс {chance / 10m:0.0}%" + (entry.Roll is { } roll ? $" · Бросок {roll}/1000" : " · Без броска"));
        if (entry.RatingBreakdown is { } ratings)
        {
            lines.Add($"ПР: {Describe(ratings.DefenseOperator)}");
            lines.Add(ratings.TorpedoOperator is { } attack ? $"Торпеда: {Describe(attack)}" : $"Торпеда: legacy R={ratings.TorpedoRating:0.##}");
        }
        else if (entry.TorpedoOperator is { } op) lines.Add(Describe(op));
        if (entry.Damage is { } damage) lines.Add($"Урон {damage}");
        return lines;
        static string Describe(WeaponOperatorSnapshot op) => $"{op.DisplayName}: база {op.BaseRating:0.##} × навык {op.Skill}/50 = R {op.EffectiveRating:0.##}";
    }
}
