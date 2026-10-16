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
    internal IEnumerable<string> VisibleLines => _lines.Skip(_offset).Take(_visibleLineCount);
    internal void Render(SKCanvas canvas, float width, float height, ImmutableArray<CombatJournalEntry> entries)
    {
        Entries = entries.IsDefault ? [] : entries;
        float left = Math.Min(384, Math.Max(8, width - 96));
        float w = Math.Max(1, Math.Min(540, width - left - 8));
        Bounds = new(left, 48, left + w, 74 + (Expanded ? Math.Min(260, Math.Max(100, height - 180)) : 0));
        using var fill = new SKPaint { Color = new SKColor(8, 25, 36, 242) };
        using var text = new SKPaint { Color = new SKColor(160, 230, 245), TextSize = 12, IsAntialias = true };
        canvas.DrawRect(Bounds, fill);
        canvas.Save(); canvas.ClipRect(Bounds);
        canvas.DrawText($"{(Expanded ? "−" : "+")} {T("Journal")} · {Entries.Length}", left + 6, 66, text);
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
        foreach (var line in VisibleLines)
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
    private static string T(string key) => GameSessionScreen.CombatText(key, key);

    internal static IReadOnlyList<string> Format(CombatJournalEntry entry)
    {
        string type = T("Event." + entry.Type);
        var lines = new List<string> { $"{entry.MotionTimeMs / 1000:0.000} {T("Seconds")} · {type} · {entry.ProjectileObjectId}",
            $"{entry.ActorObjectId} → {entry.TargetObjectId}" };
        if (entry.LauncherModuleId is { } module) lines.Add($"{T("Module")}: {module}");
        if (entry.LaunchMode is { } mode) lines.Add($"{T("LaunchMode")}: {T(mode == LaunchMode.Manual ? "Manual" : "Auto")}");
        if (entry.Type == CombatEventType.TorpedoHitRoll && entry.Attack is { } attack && entry.Defense is { } defense)
            lines.Add($"{T("Attack")} {attack:0.##} · {T("DefenseRating")} {defense:0.##}");
        else if (entry.Type is CombatEventType.Intercept or CombatEventType.Miss &&
            entry.Accuracy is { } defenseRating && entry.TargetManeuverability is { } attackRating)
        {
            lines.Add($"{T("DefenseRating")} {defenseRating:0.##} · {T("Attack")} {attackRating:0.##}");
            lines.Add(T("InterceptFormula"));
        }
        else if (entry.Accuracy is { } accuracy && entry.TargetManeuverability is { } maneuverability)
            lines.Add($"{T("Accuracy")} {accuracy:0.##} − {T("Maneuverability")} {maneuverability:0.##}");
        if (entry.ChanceTenths is { } chance) lines.Add($"{T("Chance")} {chance / 10m:0.0}%" + (entry.Roll is { } roll ? $" · {T("Roll")} {roll}/1000" : " · " + T("NoRoll")));
        if (entry.RatingBreakdown is { } ratings)
        {
            lines.Add($"{T("Countermeasure")}: {Describe(ratings.DefenseOperator)}");
            lines.Add(ratings.TorpedoOperator is { } torpedoOperator ? $"{T("Torpedo")}: {Describe(torpedoOperator)}" : $"{T("Maneuverability")}: {ratings.TorpedoRating:0.##}");
        }
        else if (entry.TorpedoOperator is { } op) lines.Add(Describe(op));
        if (entry.Type is CombatEventType.Hit or CombatEventType.Destroyed && entry.Damage is { } damage)
            lines.Add($"{T("Damage")} {damage}");
        if (entry.HitModuleId is { } hitModule)
            lines.Add($"{T("HitModule")}: {hitModule}");
        if (entry.Type == CombatEventType.ModuleHit)
        {
            if (entry.ArmorBefore is { } armorBefore && entry.ArmorAfter is { } armorAfter)
                lines.Add($"{T("Armor")}: {armorBefore} → {armorAfter}");
            if (entry.StructureBefore is { } structureBefore && entry.StructureAfter is { } structureAfter)
                lines.Add($"{T("Structure")}: {structureBefore} → {structureAfter}");
        }
        if (entry.Type == CombatEventType.ShipDestroyed && entry.DestroyReason is { } destroyReason)
            lines.Add($"{T("Cause")}: {DescribeDestroyReason(destroyReason)}");
        if (entry.Type == CombatEventType.BurstShotSkipped && entry.Result is { } skippedReason)
            lines.Add($"{T("Cause")}: {(skippedReason == "target_or_module_unavailable" ? T("Reason.BurstTargetOrModuleUnavailable") : skippedReason)}");
        if (entry.Type == CombatEventType.LauncherBreakdown && entry.Result is { Length: > 0 } brokenMode)
            lines.Add($"{T("Mode")}: {T("Mode." + char.ToUpperInvariant(brokenMode[0]) + brokenMode[1..])}");
        string? reason = entry.TerminationKind switch
        {
            CountermeasureTerminationKind.LifetimeExpired => "lifetime_expired",
            CountermeasureTerminationKind.MissedCoastExpired => "missed_coast_expired",
            CountermeasureTerminationKind.TargetLost => "target_lost",
            CountermeasureTerminationKind.OwnerLost => "owner_lost",
            _ => null
        };
        if (reason is not null) lines.Add($"{T("State")}: {GameSessionScreen.DefenseReasonText(reason)}");
        return lines;
        static string Describe(WeaponOperatorSnapshot op) => $"{op.DisplayName}: {T("SkillMetadata")} {op.Skill}";
        static string DescribeDestroyReason(string reason) => reason.StartsWith("critical:", StringComparison.Ordinal)
            ? T("CriticalModule") + " " + reason["critical:".Length..]
            : reason.StartsWith("threshold:", StringComparison.Ordinal)
                ? T("ModuleThreshold") + " " + reason["threshold:".Length..]
                : reason;
    }
}
