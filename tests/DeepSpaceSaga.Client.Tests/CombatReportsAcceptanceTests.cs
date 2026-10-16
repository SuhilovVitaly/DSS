using System.Reflection;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;
using static DeepSpaceSaga.Client.Tests.CombatReportScreenFlowTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportsAcceptanceTests
{
    [Fact]
    public async Task TransportToPanelKeepsEveryPendingReport()
    {
        await using var fixture = await CombatReportsAcceptanceFixtures.Create();
        Assert.True(fixture.Expected.Length >= 3);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(fixture.Screen, surface.Canvas);
        Assert.Equal(fixture.Expected.AsEnumerable().Reverse(), fixture.Screen.ReportStore.Reports);
        fixture.Handle.Buffer.Update(fixture.Handle.Buffer.Latest!.Snapshot);
        Frame(fixture.Screen, surface.Canvas); Assert.Equal(fixture.Expected.Length, fixture.Screen.ReportStore.Reports.Count);
        Assert.Null(fixture.Handle.Failure);
    }

    [Fact]
    public void HistoryAndDeliveryLimitsAreDifferent()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(World(1, 4106, Enumerable.Range(11, 4096).Select(i => Report(i)).ToArray()));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor()); screen.PrepareCombatReports();
        Assert.Equal(200, screen.ReportStore.Reports.Count);
        Assert.Equal(4106, screen.ReportStore.ExpandedSequence); Assert.Equal(3907, screen.ReportStore.Reports[^1].Sequence);
        screen.DisposeCombatReports();
    }

    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.2f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1920, 1080, 1.2f)]
    [InlineData(1920, 1080, 1.5f)]
    public void AllSupportedViewportsAndLocales(int width, int height, float scale)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        foreach (string language in new[] { "Russian", "English" })
            foreach (int count in new[] { 0, 200 })
            {
                var buffer = new SnapshotBuffer(); buffer.Update(World(1, 10 + count, Enumerable.Range(11, count).Select(i => Report(i) with
                { Kind = BattleReportKind.EnemyShipHit, TargetDisplayName = new string('W', 90), Reporter = new(BattleReportReporterRole.TorpedoOperator, DisplayName: new string('W', 80)) }).ToArray()));
                var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
                var locale = Localization.LoadLocaleFile(language)!;
                typeof(GameSessionScreen).GetField("_combatReportFormatter", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(screen, new CombatReportFormatter(key => locale[key]));
                Frame(screen, surface.Canvas, width, height);
                Assert.True(screen.CombatReportsBounds.Height >= 80);
                Assert.Equal(screen.ObjectInfoPanel.Bounds.Width, screen.CombatReportsBounds.Width);
                var toggle = screen.ObjectInfoPanel.HideShowButtonRect;
                screen.OnMouseDown(toggle.MidX * scale, toggle.MidY * scale); Frame(screen, surface.Canvas, width, height);
                Assert.True(screen.CombatReportsBounds.Top >= screen.ObjectInfoPanel.Bounds.Bottom);
                Assert.Equal(count, screen.ReportStore.Reports.Count);
                screen.DisposeCombatReports();
            }
    }

    [Fact]
    public async Task NoMapInputLeakOrFocusSteal()
    {
        await using var fixture = await CombatReportsAcceptanceFixtures.Create();
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(fixture.Screen, surface.Canvas);
        Assert.False(fixture.Screen.ReportPanel.HasFocus);
        var before = (fixture.Screen.CameraFocusX, fixture.Screen.CameraFocusY, fixture.Screen.CameraPixelsPerWorldUnit, fixture.Handle.Buffer.CurrentSpeed);
        var bounds = fixture.Screen.CombatReportsBounds;
        fixture.Screen.OnMouseDown(bounds.MidX, bounds.Top + 10); fixture.Screen.OnMouseUp(bounds.MidX, bounds.Top + 10);
        fixture.Screen.OnMouseWheel(bounds.MidX, bounds.Top + 10, -5); fixture.Screen.OnKeyDown(Key.Down); fixture.Screen.OnKeyDown(Key.Enter);
        Assert.Equal(before, (fixture.Screen.CameraFocusX, fixture.Screen.CameraFocusY, fixture.Screen.CameraPixelsPerWorldUnit, fixture.Handle.Buffer.CurrentSpeed));
    }

    [Fact]
    public async Task LegacyJournalAndObjectInfoRemainUsable()
    {
        await using var fixture = await CombatReportsAcceptanceFixtures.Create();
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(fixture.Screen, surface.Canvas);
        var journal = (CombatJournalPanel)typeof(GameSessionScreen).GetField("_combatJournalPanel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Screen)!;
        fixture.Screen.OnMouseDown(journal.Bounds.MidX, journal.Bounds.Top + 10); Assert.True(journal.Expanded);
        Frame(fixture.Screen, surface.Canvas); Assert.True(journal.Bounds.Right < fixture.Screen.CombatReportsBounds.Left);
        var toggle = fixture.Screen.ObjectInfoPanel.HideShowButtonRect;
        fixture.Screen.OnMouseDown(toggle.MidX, toggle.MidY); Frame(fixture.Screen, surface.Canvas);
        Assert.Equal(ObjectInfoPanelState.Closed, fixture.Screen.ObjectInfoPanel.State);
        Assert.Equal(fixture.Expected.Length, fixture.Screen.ReportStore.Reports.Count);
    }
}
