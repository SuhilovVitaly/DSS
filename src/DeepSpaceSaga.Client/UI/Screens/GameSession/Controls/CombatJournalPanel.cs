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
    private string? _layoutPlayerObjectId;
    private int _visibleLineCount;
    internal IEnumerable<string> VisibleLines => _lines.Skip(_offset).Take(_visibleLineCount);
    internal void Render(SKCanvas canvas, float width, float height, ImmutableArray<CombatJournalEntry> entries,
        float maximumRight = float.PositiveInfinity, string? playerShipObjectId = null)
    {
        Entries = entries.IsDefault ? [] : entries;
        float left = Math.Min(384, Math.Max(8, width - 96));
        float w = Math.Max(1, Math.Min(540, Math.Min(width - 8, maximumRight) - left));
        Bounds = new(left, 48, left + w, 74 + (Expanded ? Math.Min(260, Math.Max(100, height - 180)) : 0));
        using var fill = new SKPaint { Color = new SKColor(8, 25, 36, 242) };
        using var text = new SKPaint { Color = new SKColor(160, 230, 245), TextSize = 12, IsAntialias = true };
        canvas.DrawRect(Bounds, fill);
        canvas.Save(); canvas.ClipRect(Bounds);
        canvas.DrawText($"{(Expanded ? "−" : "+")} {T("Journal")} · {Entries.Length}", left + 6, 66, text);
        canvas.Restore();
        if (!Expanded) return;
        if (_layoutEntries != Entries || _layoutWidth != w || _layoutPlayerObjectId != playerShipObjectId)
        {
            _lines.Clear();
            foreach (var entry in Entries.Reverse())
            {
                foreach (var line in Format(entry, playerShipObjectId))
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
            _layoutPlayerObjectId = playerShipObjectId;
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

    internal static IReadOnlyList<string> Format(CombatJournalEntry entry, string? playerShipObjectId = null)
    {
        if (entry.Type == CombatEventType.TorpedoReloadCompleted)
            return [T("ReloadCompleted").Replace("{launcher}", entry.Result ?? entry.LauncherModuleId ?? string.Empty, StringComparison.Ordinal)];
        bool ownActor = playerShipObjectId is not null && entry.ActorObjectId == playerShipObjectId;
        bool ownTarget = playerShipObjectId is not null && entry.TargetObjectId == playerShipObjectId;
        string type = T(entry.Type == CombatEventType.TorpedoHitRoll ? "Event.LegacyResolution" : "Event." + entry.Type);
        var lines = new List<string> { $"{entry.MotionTimeMs / 1000:0.000} {T("Seconds")} · {type} · {entry.ProjectileObjectId}",
            string.IsNullOrEmpty(entry.TargetObjectId) ? entry.ActorObjectId : $"{entry.ActorObjectId} → {entry.TargetObjectId}" };
        if (ownActor && entry.LauncherModuleId is { } module) lines.Add($"{T("Module")}: {module}");
        if (entry.ProjectileKind is { } kind) lines.Add($"{T("ProjectileKind")}: {T("Kind." + kind)}");
        if (entry.ProfileId is { } profile) lines.Add($"{T("Profile")}: {profile}");
        if (entry.BlastRadiusKm is { } radius) lines.Add($"{T("BlastRadius")}: {radius:0.##} {T("Kilometers")}");
        if (entry.HitResult is { } result) lines.Add($"{T("HitResult")}: {T("Hit." + result)}");
        if (entry.DestroyedProjectileIds is { IsDefaultOrEmpty: false } ids)
            lines.Add($"{T("InterceptedProjectiles")}: {string.Join(", ", ids)}");
        if (ownTarget)
        {
            if (entry.Type is CombatEventType.Hit or CombatEventType.Destroyed or CombatEventType.ModuleHit && entry.Damage is { } damage)
                lines.Add($"{T("Damage")} {damage}");
            if (entry.HitModuleId is { } hitModule) lines.Add($"{T("HitModule")}: {hitModule}");
            if (entry.Type == CombatEventType.ModuleHit)
            {
                if (entry.ArmorBefore is { } armorBefore && entry.ArmorAfter is { } armorAfter)
                    lines.Add($"{T("Armor")}: {armorBefore} → {armorAfter}");
                if (entry.StructureBefore is { } structureBefore && entry.StructureAfter is { } structureAfter)
                    lines.Add($"{T("Structure")}: {structureBefore} → {structureAfter}");
            }
            if (entry.Type == CombatEventType.ShipDestroyed && entry.DestroyReason is { } reason)
                lines.Add($"{T("Cause")}: {DescribeDestroyReason(reason)}");
        }
        if (entry.Type == CombatEventType.BurstShotSkipped && entry.Result is { } skippedReason)
            lines.Add($"{T("Cause")}: {(skippedReason == "target_or_module_unavailable" ? T("Reason.BurstTargetOrModuleUnavailable") : skippedReason)}");
        if (entry.Type == CombatEventType.LauncherBreakdown && entry.Result is { Length: > 0 } brokenMode)
            lines.Add($"{T("Cause")}: {T("Mode." + char.ToUpperInvariant(brokenMode[0]) + brokenMode[1..])}");
        string? termination = entry.TerminationKind switch
        {
            CountermeasureTerminationKind.LifetimeExpired => "lifetime_expired",
            CountermeasureTerminationKind.MissedCoastExpired => "missed_coast_expired",
            CountermeasureTerminationKind.TargetLost => "target_lost",
            CountermeasureTerminationKind.OwnerLost => "owner_lost",
            _ => null
        };
        if (termination is not null) lines.Add($"{T("State")}: {GameSessionScreen.DefenseReasonText(termination)}");
        return lines;
        static string DescribeDestroyReason(string reason) => reason.StartsWith("critical:", StringComparison.Ordinal)
            ? T("CriticalModule") + " " + reason["critical:".Length..]
            : reason.StartsWith("threshold:", StringComparison.Ordinal)
                ? T("ModuleThreshold") + " " + reason["threshold:".Length..] : reason;
    }
}
