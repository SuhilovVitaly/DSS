using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportFormatterTests
{
    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void NamedReporterIncludesLocalizedRole(string language)
    {
        var locale = Localization.LoadLocaleFile(language)!;
        var formatter = new CombatReportFormatter(key => locale[key]);
        foreach (var role in Enum.GetValues<BattleReportReporterRole>())
        {
            var model = formatter.Format(Report(11) with { Reporter = new(role, "crew", "Vera") });
            Assert.Equal("Vera\n" + locale["CombatReports.Role." + role], model.Reporter);
        }
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void EveryKindHasRussianAndEnglishText(string language)
    {
        var locale = Localization.LoadLocaleFile(language)!;
        var formatter = new CombatReportFormatter(key => locale[key]);
        foreach (var kind in Enum.GetValues<BattleReportKind>())
        {
            var model = formatter.Format(Report(11) with { Kind = kind, SourceDisplayName = "Source", TargetDisplayName = "Target", MotionTimeMs = 3723125 });
            Assert.False(string.IsNullOrWhiteSpace(model.Title));
            Assert.False(string.IsNullOrWhiteSpace(model.Message));
            Assert.DoesNotContain("CombatReports.", string.Join(" ", model.Title, model.Message, model.Reporter, model.Time, model.DamageDetails));
            Assert.DoesNotContain("{", string.Join(" ", model.Title, model.Message, model.Reporter, model.Time, model.DamageDetails));
            Assert.Equal("01:02:03", model.Time);
        }
    }

    [Fact]
    public void DestroyedVariantUsesSingleFinalText()
    {
        var locale = Localization.LoadLocaleFile("English")!;
        var formatter = new CombatReportFormatter(key => locale[key]);
        var model = formatter.Format(Report(11) with { Kind = BattleReportKind.EnemyShipHit, Variant = BattleReportVariant.Destroyed, TargetDisplayName = "Mukha" });
        Assert.Equal("enemy-ship-destroyed", model.VisualKey);
        Assert.Equal("Mukha destroyed", model.Title);
    }

    [Theory]
    [InlineData(BattleReportReporterRole.ScannerOperator)]
    [InlineData(BattleReportReporterRole.TorpedoOperator)]
    [InlineData(BattleReportReporterRole.CountermeasureOperator)]
    [InlineData(BattleReportReporterRole.Captain)]
    public void MissingReporterUsesRole(BattleReportReporterRole role)
    {
        var locale = Localization.LoadLocaleFile("Russian")!;
        var model = new CombatReportFormatter(key => locale[key]).Format(Report(11) with { Reporter = new(role) });
        Assert.False(string.IsNullOrWhiteSpace(model.Reporter));
        Assert.DoesNotContain("CombatReports.", model.Reporter);
    }

    [Fact]
    public void HiddenDataHasNoTextPlaceholderLeak()
    {
        var locale = Localization.LoadLocaleFile("English")!;
        var model = new CombatReportFormatter(key => locale[key]).Format(Report(11) with { Kind = BattleReportKind.EnemyTorpedoLaunch });
        Assert.DoesNotContain("Target:", model.Message);
        Assert.DoesNotContain("null", string.Join(" ", model.Title, model.Message, model.Reporter, model.Time, model.DamageDetails));
        Assert.DoesNotContain("{", string.Join(" ", model.Title, model.Message, model.Reporter, model.Time, model.DamageDetails));
        Assert.DoesNotContain("projectile-11", string.Join(" ", model.Title, model.Message, model.Reporter, model.Time, model.DamageDetails));
    }

    [Fact]
    public void DeletedParticipantUsesCapturedName()
    {
        var locale = Localization.LoadLocaleFile("Russian")!;
        var report = Report(11) with
        {
            Kind = BattleReportKind.EnemyShipHit,
            TargetDisplayName = "ÐœÑƒÑ…Ð°-2",
            Reporter = new(BattleReportReporterRole.TorpedoOperator, "crew", "Ð’ÐµÑ€Ð°"),
            Damage = new("module", "Ð”Ð²Ð¸Ð³Ð°Ñ‚ÐµÐ»ÑŒ", 60, 0, 100, 80)
        };
        var model = new CombatReportFormatter(key => locale[key]).Format(report);
        Assert.Contains("ÐœÑƒÑ…Ð°-2", model.Message);
        Assert.Equal(report.Reporter.DisplayName + "\n" + locale["CombatReports.Role.TorpedoOperator"], model.Reporter);
        Assert.Null(model.DamageDetails);
        var own = new CombatReportFormatter(key => locale[key]).Format(report with { Kind = BattleReportKind.PlayerShipDamaged });
        Assert.Contains(report.Damage!.ModuleDisplayName!, own.DamageDetails);
        Assert.DoesNotContain("{", own.DamageDetails);
    }

    [Fact]
    public void LocalesHaveEqualKeysAndPlaceholders()
    {
        var en = Localization.LoadLocaleFile("English")!.Where(k => k.Key.StartsWith("CombatReports.")).ToDictionary();
        var ru = Localization.LoadLocaleFile("Russian")!.Where(k => k.Key.StartsWith("CombatReports.")).ToDictionary();
        Assert.Equal(en.Keys.Order(), ru.Keys.Order());
        foreach (var pair in en)
            Assert.Equal(Regex.Matches(pair.Value, "\\{[^}]+\\}").Select(m => m.Value).Order(),
                Regex.Matches(ru[pair.Key], "\\{[^}]+\\}").Select(m => m.Value).Order());
    }
}
