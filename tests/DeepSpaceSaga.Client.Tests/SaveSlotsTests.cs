using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Tests for <see cref="SaveSlots.ExcludeReserved"/> — the filter
/// <c>SkiaWindow.OpenSaveWindowAsync</c> applies before handing a slot list to
/// <see cref="DeepSpaceSaga.Client.UI.Screens.Save.SaveScreen"/>, so the reserved
/// quicksave slot never shows up as a normal OVERWRITE/DELETE-able row in the Save
/// window (which would let a player silently break F9 quickload).
/// </summary>
public class SaveSlotsTests
{
    private static SaveSlotInfo Slot(string id) => new(id, id, DateTime.UtcNow);

    [Fact]
    public void ExcludeReserved_removes_the_quicksave_slot()
    {
        var slots = new[] { Slot("quicksave"), Slot("My Save"), Slot("Another") };

        var result = SaveSlots.ExcludeReserved(slots);

        Assert.DoesNotContain(result, s => s.SlotId == SaveSlots.Quicksave);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ExcludeReserved_keeps_every_other_slot_and_their_order()
    {
        var slots = new[] { Slot("Alpha"), Slot("Beta") };

        var result = SaveSlots.ExcludeReserved(slots);

        Assert.Equal(new[] { "Alpha", "Beta" }, result.Select(s => s.SlotId));
    }

    [Fact]
    public void ExcludeReserved_is_a_no_op_when_quicksave_is_not_present()
    {
        var slots = new[] { Slot("Alpha") };

        var result = SaveSlots.ExcludeReserved(slots);

        Assert.Single(result);
    }

    [Fact]
    public void ExcludeReserved_handles_an_empty_list()
    {
        var result = SaveSlots.ExcludeReserved(Array.Empty<SaveSlotInfo>());

        Assert.Empty(result);
    }
}
