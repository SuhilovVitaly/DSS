using DeepSpaceSaga.Client.UI.Screens.GameSession;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportStoreTests
{
    [Fact]
    public void ExactlyOneCardIsExpanded()
    {
        var store = new CombatReportStore();
        store.Receive([Report(1), Report(2), Report(3)]);
        Assert.Equal(3, store.ExpandedSequence);
        Assert.True(store.Expand(1));
        Assert.Equal(1, store.ExpandedSequence);
        Assert.True(store.Expand(1));
        Assert.Equal(1, store.ExpandedSequence);
        Assert.False(store.Expand(99));
        Assert.Equal(1, store.ExpandedSequence);
    }

    [Fact]
    public void NewestEventReplacesManualExpansion()
    {
        var store = new CombatReportStore();
        store.Receive([Report(2), Report(1)]);
        store.Expand(1);
        store.Receive([Report(4), Report(3)]);
        Assert.Equal(4, store.ExpandedSequence);
        Assert.Equal(new long[] { 4, 3, 2, 1 }, store.Reports.Select(r => r.Sequence));
        store.MoveSelection(1);
        Assert.Equal(3, store.KeyboardSelection);
        Assert.Equal(4, store.ExpandedSequence);
    }

    [Fact]
    public void DuplicateDoesNotResetView()
    {
        var store = new CombatReportStore();
        store.Receive([Report(1), Report(2)]);
        store.Expand(1); store.ScrollOffset = 42;
        Assert.False(store.Receive([Report(2), Report(1)]));
        Assert.Equal(42, store.ScrollOffset);
        Assert.Equal(1, store.ExpandedSequence);
    }

    [Fact]
    public void HistoryEvictsOldestAfter200()
    {
        var store = new CombatReportStore();
        store.Receive(Enumerable.Range(1, 200).Select(i => Report(i)));
        store.Expand(1);
        store.Receive([Report(201)]);
        Assert.Equal(200, store.Reports.Count);
        Assert.DoesNotContain(store.Reports, r => r.Sequence == 1);
        Assert.Equal(201, store.ExpandedSequence);
        Assert.Equal(201, store.KeyboardSelection);
    }

    [Fact]
    public void ResetAndMapSelectionAreIndependent()
    {
        var store = new CombatReportStore();
        store.Receive([Report(1)]);
        store.Reset();
        Assert.Empty(store.Reports);
        Assert.Null(store.ExpandedSequence);
        Assert.Null(store.KeyboardSelection);
        Assert.Equal(0, store.ScrollOffset);
        store.Receive([Report(1)]);
        Assert.Single(store.Reports);
    }
}
