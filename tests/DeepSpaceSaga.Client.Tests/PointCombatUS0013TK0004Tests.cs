using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0013TK0004Tests
{

    [Theory]
    [InlineData(BattleReportKind.EnemyInRange)]
    [InlineData(BattleReportKind.EnemyTorpedoLaunch)]
    [InlineData(BattleReportKind.PlayerTorpedoLaunch)]
    [InlineData(BattleReportKind.PlayerCountermeasureLaunch)]
    [InlineData(BattleReportKind.EnemyTorpedoIntercepted)]
    [InlineData(BattleReportKind.EnemyShipHit)]
    public void Reports_never_render_module_damage_outside_own_damage_kind(BattleReportKind kind)
    {
        var locale = Localization.LoadLocaleFile("English")!;
        var report = new BattleReportSnapshot(1, 1234, kind, "episode", new(BattleReportReporterRole.ScannerOperator, "crew", "Captured Vera"),
            Damage: new("private", "Hidden enemy module", 9999, 9998, 8888, 8887), ProjectileKind: ProjectileKind.Torpedo,
            ProfileId: "fast-profile", BlastRadiusKm: 2.5, HitResult: ProjectileHitResult.Core);
        var model = new CombatReportFormatter(key => locale[key]).Format(report);
        Assert.Null(model.DamageDetails); Assert.Contains("fast-profile", model.Message); Assert.Contains("2.5", model.Message);
        Assert.Contains("Captured Vera", model.Reporter);
        Assert.DoesNotContain("9999", model.Message);
    }

    [Fact]
    public void Own_report_health_and_captured_reporter_survive_bounded_history_and_duplicate_sequences()
    {
        var report = new BattleReportSnapshot(1, 1234, BattleReportKind.PlayerShipDamaged, "episode", new(BattleReportReporterRole.Captain, "captain", "Captured Captain"),
            Damage: new("own-module", "Own engine", 20, 0, 50, 25), HitResult: ProjectileHitResult.Edge);
        var store = new CombatReportStore();
        store.Receive(Enumerable.Range(1, 210).Select(n => report with { Sequence = n }));
        Assert.Equal(CombatReportStore.HistoryLimit, store.Reports.Count);
        Assert.Equal(210, store.Reports[0].Sequence); Assert.Equal(11, store.Reports[^1].Sequence);
        Assert.False(store.Receive([report with { Sequence = 210, Reporter = new(BattleReportReporterRole.Captain, "new", "Replacement") }]));
        var locale = Localization.LoadLocaleFile("English")!;
        var model = new CombatReportFormatter(key => locale[key]).Format(store.Reports[0]);
        Assert.Contains("Captured Captain", model.Reporter); Assert.DoesNotContain("Replacement", model.Reporter);
        Assert.Contains("Own engine", model.DamageDetails); Assert.Contains("50", model.DamageDetails); Assert.Contains("25", model.DamageDetails);
        Assert.Equal(210, store.ExpandedSequence);
    }
}
