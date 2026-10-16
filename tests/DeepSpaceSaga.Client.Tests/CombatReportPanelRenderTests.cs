using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportPanelRenderTests
{
    private static CombatReportFormatter Formatter() => new(key => Localization.LoadLocaleFile("English")![key]);

    [Fact]
    public void OpenCardContainsReportAndSeparatePortrait()
    {
        var store = new CombatReportStore(); store.Receive([Report(11)]);
        using var panel = new CombatReportPanel(); panel.Prepare(new(0, 0, 360, 600), store, Formatter());
        var card = Assert.Single(panel.Layout!.Cards);
        Assert.True(card.Expanded);
        Assert.Equal(16f / 9, card.Illustration.Width / card.Illustration.Height, 4);
        Assert.True(card.Portrait.Top > card.Illustration.Bottom);
        Assert.NotEmpty(card.BodyLines); Assert.NotEmpty(card.ReporterLines);
        Assert.True(card.Bottom > card.BodyTop);
    }

    [Fact]
    public void ClosedCardsHaveOnlyTitlebars()
    {
        var store = new CombatReportStore(); store.Receive([Report(11), Report(12)]);
        using var panel = new CombatReportPanel(); panel.Prepare(new(0, 0, 360, 600), store, Formatter());
        var closed = panel.Layout!.Cards.Single(c => c.Report.Sequence == 11);
        Assert.False(closed.Expanded); Assert.True(closed.Illustration.IsEmpty); Assert.Empty(closed.BodyLines);
        Assert.Equal(closed.Header.Bottom, closed.Bottom);
    }

    [Fact]
    public void LongNamesWrapWithoutOverflow()
    {
        var store = new CombatReportStore();
        store.Receive([Report(11) with { Kind = BattleReportKind.EnemyInRange, TargetDisplayName = new string('W', 300),
            Reporter = new(BattleReportReporterRole.ScannerOperator, DisplayName: new string('W', 200)) }]);
        using var panel = new CombatReportPanel(); panel.Prepare(new(0, 0, 260, 300), store, Formatter());
        var card = Assert.Single(panel.Layout!.Cards);
        Assert.True(card.TitleLines.Length > 1); Assert.True(card.ReporterLines.Length > 1);
        Assert.True(panel.Layout.MaxScroll > 0);
        Assert.True(card.BodyTop > card.Portrait.Top + card.ReporterLines.Length * 16);
    }

    [Fact]
    public void MissingAssetsHaveSemanticFallback()
    {
        var store = new CombatReportStore(); store.Receive([Report(11)]);
        using var panel = new CombatReportPanel(); panel.Prepare(new(0, 0, 360, 600), store, Formatter());
        using var assets = new CombatReportAssets(Path.GetTempPath()); assets.Prepare([]);
        using var surface = SKSurface.Create(new SKImageInfo(360, 600));
        surface.Canvas.Clear(SKColors.Transparent); panel.Render(surface.Canvas, assets);
        using var image = surface.Snapshot(); using var bitmap = SKBitmap.FromImage(image);
        Assert.Contains(bitmap.Pixels, p => p.Alpha != 0);
        var layout = panel.Layout; panel.Prepare(new(0, 0, 360, 600), store, Formatter());
        Assert.Same(layout, panel.Layout);
    }

    [Fact]
    public void ZeroHeightIsSafe()
    {
        var store = new CombatReportStore(); store.Receive([Report(11)]);
        using var panel = new CombatReportPanel(); using var assets = new CombatReportAssets();
        panel.Prepare(new(0, 0, 0, 0), store, Formatter());
        using var surface = SKSurface.Create(new SKImageInfo(1, 1)); panel.Render(surface.Canvas, assets);
        Assert.Empty(panel.Layout!.Cards);
    }
}
