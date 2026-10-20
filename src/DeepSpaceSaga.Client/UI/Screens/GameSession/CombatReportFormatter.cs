using System.Globalization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal sealed record CombatReportPresentation(string Title, string Message, string Reporter, string Time,
    string? DamageDetails, string VisualKey);

internal sealed class CombatReportFormatter(Func<string, string>? localize = null)
{
    private readonly Func<string, string> _localize = localize ?? Localization.Get;
    internal string Text(string key) => _localize("CombatReports." + key);

    internal CombatReportPresentation Format(BattleReportSnapshot report)
    {
        string visual = VisualKey(report);
        string source = report.SourceDisplayName ?? Text("UnknownContact");
        string target = report.TargetDisplayName ?? Text("UnknownContact");
        string Fill(string text) => text.Replace("{source}", source, StringComparison.Ordinal)
            .Replace("{target}", target, StringComparison.Ordinal);
        string message = Fill(Text(visual + ".Body"));
        if (report.Kind == BattleReportKind.EnemyTorpedoLaunch && report.TargetObjectId is not null)
            message += " " + Fill(Text("EnemyLaunchTarget"));
        string C(string key) => _localize("Combat." + key);
        var facts = new List<string>();
        if (report.ProjectileKind is { } kind) facts.Add($"{C("ProjectileKind")}: {C("Kind." + kind)}");
        if (report.ProfileId is { } profile) facts.Add($"{C("Profile")}: {Localization.Combat("Profile." + profile, _localize)}");
        if (report.BlastRadiusKm is { } radius) facts.Add($"{C("BlastRadius")}: {radius:0.##} {C("Kilometers")}");
        if (report.HitResult is { } result) facts.Add($"{C("HitResult")}: {C("Hit." + result)}");
        if (facts.Count > 0) message += "\n" + string.Join(" · ", facts);
        string role = report.Reporter.Role switch
        {
            BattleReportReporterRole.ScannerOperator => "ScannerOperator",
            BattleReportReporterRole.TorpedoOperator => "TorpedoOperator",
            BattleReportReporterRole.CountermeasureOperator => "CountermeasureOperator",
            _ => "Captain"
        };
        string reporter = string.IsNullOrWhiteSpace(report.Reporter.DisplayName)
            ? Text("Role." + role) : report.Reporter.DisplayName + "\n" + Text("Role." + role);
        string? damage = report.Kind == BattleReportKind.PlayerShipDamaged && report.Damage is { } damageFacts ? Text("Damage")
            .Replace("{module}", damageFacts.ModuleDisplayName ?? Text("UnknownModule"), StringComparison.Ordinal)
            .Replace("{armorBefore}", damageFacts.ArmorBefore.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{armorAfter}", damageFacts.ArmorAfter.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{structureBefore}", damageFacts.StructureBefore.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{structureAfter}", damageFacts.StructureAfter.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) : null;
        double seconds = Math.Floor(report.MotionTimeMs / 1000);
        string time = FormattableString.Invariant($"{Math.Floor(seconds / 3600):00}:{Math.Floor(seconds / 60) % 60:00}:{seconds % 60:00}");
        return new(Fill(Text(visual + ".Title")), message, reporter, time, damage, visual);
    }

    internal static string VisualKey(BattleReportSnapshot report) => report.Kind switch
    {
        BattleReportKind.EnemyInRange => "enemy-in-range",
        BattleReportKind.EnemyTorpedoLaunch => "enemy-torpedo-launch",
        BattleReportKind.PlayerTorpedoLaunch => "player-torpedo-launch",
        BattleReportKind.PlayerCountermeasureLaunch => "player-countermeasure-launch",
        BattleReportKind.EnemyTorpedoIntercepted => "enemy-torpedo-intercepted",
        BattleReportKind.EnemyShipHit when report.Variant == BattleReportVariant.Destroyed => "enemy-ship-destroyed",
        BattleReportKind.EnemyShipHit => "enemy-ship-hit",
        _ => "player-ship-damaged"
    };
}
