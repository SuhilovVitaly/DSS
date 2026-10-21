using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class PointCombatUS0020TK0005Tests
{
    [Fact]
    public async Task Real_session_reload_report_enqueues_once_after_scene_admission()
    {
        await using var f = await PointCombatUiFixture.Create();
        f.Screen.PrepareCombatReports();
        f.Expose(300); f.Aim(); f.Click(300);
        await f.Wait(() => f.Presented.Objects.Any(o => o.OwnProjectile is not null));
        await f.AdvanceTo(10000);
        try
        {
            await f.Wait(() =>
            {
                f.Screen.PrepareCombatReports();
                return f.Screen.ReportStore.Reports.Any(r => r.Kind == BattleReportKind.PlayerTorpedoReloadCompleted);
            });
        }
        catch (OperationCanceledException)
        {
            var actual = f.Engine.CaptureSnapshot();
            throw new InvalidOperationException(System.Text.Json.JsonSerializer.Serialize(new
            {
                actual.MotionTimeMs,
                Events = actual.CombatJournal.Where(e => e.Type == CombatEventType.TorpedoReloadCompleted),
                actual.CombatReports,
                PresentedReports = f.Presented.CombatReports,
                Reports = f.Screen.ReportStore.Reports,
                Launcher = actual.InstalledModules.Single(m => m.ModuleId == PointCombatUiFixture.Torpedo).LauncherCombat
            }));
        }
        Assert.Single(f.Screen.ReportStore.Reports, r => r.Kind == BattleReportKind.PlayerTorpedoReloadCompleted);
        Assert.Single(f.Screen.RecentAlertSignals, s => s.Kind == CombatAlertKind.TorpedoReloadCompleted);
        f.Screen.PrepareCombatReports(); f.Render();
        Assert.Single(f.Screen.RecentAlertSignals, s => s.Kind == CombatAlertKind.TorpedoReloadCompleted);
    }

    [Fact]
    public void Reload_reports_deduplicate_and_allow_two_simultaneous_sounds()
    {
        var cursor = new ReloadReportNotificationCursor(); var session = Guid.NewGuid();
        BattleReportSnapshot Report(long sequence) => new(sequence, 10000, BattleReportKind.PlayerTorpedoReloadCompleted,
            "reload:" + sequence, new(BattleReportReporterRole.TorpedoOperator), LauncherModuleId: "tube" + sequence);
        Assert.Equal(2, cursor.Take(session, [Report(2), Report(1)]).Count());
        Assert.Empty(cursor.Take(session, [Report(1), Report(2)]));
        Assert.Single(cursor.Take(session, [Report(2), Report(3)]));
        Assert.Single(cursor.Take(Guid.NewGuid(), [Report(1)]));
        var gate = new CombatAudioPlaybackGate();
        Assert.True(gate.TryStart(CombatAlertKind.TorpedoReloadCompleted, 0, 1000, 0));
        Assert.True(gate.TryStart(CombatAlertKind.TorpedoReloadCompleted, 0, 1000, 0));
    }

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
