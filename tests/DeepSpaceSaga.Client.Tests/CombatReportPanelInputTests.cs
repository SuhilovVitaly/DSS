using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportPanelInputTests
{
    private static CombatReportFormatter Formatter() => new(key => Localization.LoadLocaleFile("English")![key]);
    private static CombatReportStore Store()
    {
        var store = new CombatReportStore(); store.Receive([Report(11), Report(12), Report(13)]); return store;
    }

    [Fact]
    public void ClickSwitchesExactlyOneExpandedCard()
    {
        var store = Store(); using var panel = new CombatReportPanel(); panel.Prepare(new(0, 0, 360, 900), store, Formatter());
        var old = panel.Layout!.Cards.Single(c => c.Report.Sequence == 11);
        Assert.True(panel.OnMouseDown(old.Header.MidX, old.Header.MidY));
        Assert.Equal(11, store.ExpandedSequence);
        var current = panel.Layout!.Cards.Single(c => c.Report.Sequence == 11);
        panel.OnMouseDown(current.Header.MidX, current.Header.MidY - store.ScrollOffset);
        Assert.Equal(11, store.ExpandedSequence);
    }

    [Fact]
    public void NewEventScrollsToNewestWithoutFocusSteal()
    {
        var store = Store(); using var panel = new CombatReportPanel(); panel.Prepare(new(0, 0, 360, 300), store, Formatter());
        panel.Scroll(10, 100, -10); Assert.True(store.ScrollOffset > 0);
        store.Receive([Report(14)]); panel.Prepare(new(0, 0, 360, 300), store, Formatter());
        Assert.Equal(0, store.ScrollOffset); Assert.Equal(14, store.ExpandedSequence); Assert.False(panel.HasFocus);
    }

    [Fact]
    public void WheelConsumedInsidePanel()
    {
        var store = Store(); using var panel = new CombatReportPanel(); panel.Prepare(new(20, 20, 380, 320), store, Formatter());
        Assert.True(panel.Scroll(30, 30, -1)); Assert.True(store.ScrollOffset > 0);
        Assert.False(panel.Scroll(0, 0, -1));
        Assert.True(panel.OnMouseDown(30, 30));
        Assert.True(panel.OnMouseMove(0, 0)); Assert.True(panel.OnMouseUp(0, 0));
        Assert.False(panel.OnMouseMove(0, 0));
    }

    [Fact]
    public void KeyboardSelectionAndEnter()
    {
        var store = Store(); using var panel = new CombatReportPanel(); panel.Prepare(new(0, 0, 360, 300), store, Formatter());
        Assert.False(panel.OnKeyDown(Key.Down)); panel.OnMouseDown(10, 10);
        Assert.True(panel.OnKeyDown(Key.Down)); Assert.Equal(12, store.KeyboardSelection); Assert.Equal(13, store.ExpandedSequence);
        Assert.True(panel.OnKeyDown(Key.Enter)); Assert.Equal(12, store.ExpandedSequence);
        Assert.False(panel.OnKeyDown(Key.Space)); panel.OnMouseDown(500, 500); Assert.False(panel.HasFocus);
        Assert.False(panel.OnKeyDown(Key.Up));
    }

    [Fact]
    public void ClosedCardBodyHasNoHitRegion()
    {
        var store = Store(); store.Expand(11);
        using var panel = new CombatReportPanel(); panel.Prepare(new(0, 0, 360, 900), store, Formatter());
        var closed = panel.Layout!.Cards.Single(c => c.Report.Sequence == 13);
        Assert.Equal(closed.Header.Bottom, closed.Bottom);
        panel.OnMouseDown(10, panel.Layout.Cards[^1].Bottom + 10);
        Assert.Equal(11, store.ExpandedSequence);
    }
}
