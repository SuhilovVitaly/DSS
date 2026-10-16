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
        string role = report.Reporter.Role switch
        {
            BattleReportReporterRole.ScannerOperator => "ScannerOperator",
            BattleReportReporterRole.TorpedoOperator => "TorpedoOperator",
            BattleReportReporterRole.CountermeasureOperator => "CountermeasureOperator",
            _ => "Captain"
        };
        string reporter = string.IsNullOrWhiteSpace(report.Reporter.DisplayName)
            ? Text("Role." + role) : report.Reporter.DisplayName;
        string? damage = report.Damage is { } facts ? Text("Damage")
            .Replace("{module}", facts.ModuleDisplayName ?? Text("UnknownModule"), StringComparison.Ordinal)
            .Replace("{armorBefore}", facts.ArmorBefore.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{armorAfter}", facts.ArmorAfter.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{structureBefore}", facts.StructureBefore.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{structureAfter}", facts.StructureAfter.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) : null;
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
