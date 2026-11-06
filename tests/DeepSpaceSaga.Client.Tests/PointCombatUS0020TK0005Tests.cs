using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class PointCombatUS0020TK0005Tests
{

    [Fact]
    public void Reload_journal_and_report_use_captured_launcher_name()
    {
        string Localize(string key) => key.EndsWith(".Body") || key == "Combat.ReloadCompleted" ? "Торпедный аппарат {launcher} перезаряжен" : key;
        var report = new BattleReportSnapshot(1, 10000, BattleReportKind.PlayerTorpedoReloadCompleted,
            "reload:tube:10000", new(BattleReportReporterRole.TorpedoOperator), LauncherModuleId: "tube", LauncherDisplayName: "A");
        Assert.Equal("Торпедный аппарат A перезаряжен", new CombatReportFormatter(Localize).Format(report).Message);
        var entry = new CombatJournalEntry(1, 10000, CombatEventType.TorpedoReloadCompleted, "player", "", "", 0, 0,
            Result: "A", LauncherModuleId: "tube");
        Assert.Contains("A", Assert.Single(CombatJournalPanel.Format(entry, "player")));
    }
}
