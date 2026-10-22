using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0013TK0004Tests
{
    [Theory]
    [InlineData(CombatEventType.Launch)]
    [InlineData(CombatEventType.TorpedoDetonated)]
    [InlineData(CombatEventType.CountermissileDetonated)]
    [InlineData(CombatEventType.ManuallyDetonated)]
    [InlineData(CombatEventType.ShotDown)]
    [InlineData(CombatEventType.Hit)]
    [InlineData(CombatEventType.Miss)]
    [InlineData(CombatEventType.TorpedoHitRoll)]
    public void Journal_and_reports_render_new_events_without_chance_or_roll(CombatEventType type)
    {
        var entry = new CombatJournalEntry(1, 1000, type, "player", "enemy", "projectile", 0, 0,
            ChanceTenths: 870, Roll: 991, Damage: 7777, Attack: 99, Defense: 88,
            ProjectileKind: ProjectileKind.Torpedo, ProfileId: "fast-profile", BlastRadiusKm: 2.5, HitResult: ProjectileHitResult.Edge);
        string text = string.Join('\n', CombatJournalPanel.Format(entry));
        Assert.Contains("fast-profile", text); Assert.Contains("2.5", text);
        Assert.DoesNotContain("87.0%", text); Assert.DoesNotContain("991/1000", text); Assert.DoesNotContain("7777", text);
        foreach (var word in new[] { "chance", "roll", "шанс", "бросок" }) Assert.DoesNotContain(word, text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Journal_drops_foreign_module_payload_without_own_identity()
    {
        var entry = new CombatJournalEntry(1, 1000, CombatEventType.ModuleHit, "player", "enemy", "shot", 0, 0,
            Damage: 7777, HitModuleId: "enemy-private-module", ArmorBefore: 12345, ArmorAfter: 12344, StructureBefore: 45678, StructureAfter: 45677);
        string text = string.Join('\n', CombatJournalPanel.Format(entry));
        foreach (var secret in new[] { "enemy-private-module", "7777", "12345", "45678" }) Assert.DoesNotContain(secret, text);
    }

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

    [Fact]
    public void Journal_own_health_is_visible_only_in_matching_identity_and_cache_is_invalidated()
    {
        var entry = new CombatJournalEntry(1, 1000, CombatEventType.ModuleHit, "enemy", "player", "shot", 0, 0,
            Damage: 25, HitModuleId: "own-engine", ArmorBefore: 20, ArmorAfter: 0, StructureBefore: 50, StructureAfter: 25);
        var panel = new CombatJournalPanel();
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, 1280, 720, [entry], playerShipObjectId: "player"); panel.Click(640, 60);
        panel.Render(canvas, 1280, 720, [entry], playerShipObjectId: "player");
        Assert.Contains("own-engine", string.Join('\n', panel.VisibleLines));
        Assert.Contains("20 → 0", string.Join('\n', panel.VisibleLines));
        Assert.Contains("50 → 25", string.Join('\n', panel.VisibleLines));
        panel.Render(canvas, 1280, 720, panel.Entries, playerShipObjectId: "other-player");
        Assert.DoesNotContain("own-engine", string.Join('\n', panel.VisibleLines));
        Assert.DoesNotContain("50 → 25", string.Join('\n', panel.VisibleLines));
    }

    [Theory]
    [InlineData("English", 1f)]
    [InlineData("English", 1.5f)]
    [InlineData("Russian", 1f)]
    [InlineData("Russian", 1.5f)]
    public void Actual_report_panel_wraps_public_facts_in_both_locales_and_scales(string language, float scale)
    {
        var locale = Localization.LoadLocaleFile(language)!;
        var report = new BattleReportSnapshot(1, 1234, BattleReportKind.EnemyShipHit, "episode", new(BattleReportReporterRole.TorpedoOperator, "crew", "Captured Vera"),
            TargetDisplayName: "Enemy", Damage: new("private", "Private enemy module", 7777, 7776, 8888, 8887),
            ProjectileKind: ProjectileKind.Torpedo, ProfileId: "fast-profile", BlastRadiusKm: 2.5, HitResult: ProjectileHitResult.Edge);
        var formatter = new CombatReportFormatter(key => locale[key]); var store = new CombatReportStore(); store.Receive([report]);
        using var panel = new CombatReportPanel(); using var assets = new CombatReportAssets();
        panel.Prepare(new(940, 80, 1260, 680), store, formatter); assets.Prepare([report]);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap); canvas.Scale(scale);
        panel.Render(canvas, assets);
        var card = Assert.Single(panel.Layout!.Cards);
        string lines = string.Join('\n', card.BodyLines);
        Assert.Contains("fast-profile", lines); Assert.Contains(locale["Combat.Hit.Edge"], lines);
        Assert.DoesNotContain("7777", lines); Assert.DoesNotContain("Private enemy module", lines);
        Assert.Contains("Captured Vera", string.Join('\n', card.ReporterLines));
        Assert.True(card.Expanded); Assert.True(card.Bottom > card.BodyTop);
        foreach (var word in new[] { "chance", "roll", "шанс", "бросок" }) Assert.DoesNotContain(word, lines, StringComparison.OrdinalIgnoreCase);
        if (language == "English" && scale == 1 && Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, "journal-reports-headless.png")); png.SaveTo(file);
        }
    }
}
