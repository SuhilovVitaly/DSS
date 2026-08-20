using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class SaveSlotInfoTests
{
    [Fact]
    public void SaveSlotInfo_is_instantiable()
    {
        var savedAt = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        var slot = new SaveSlotInfo(SlotId: "quicksave", DisplayName: "quicksave", SavedAtUtc: savedAt);

        Assert.Equal("quicksave", slot.SlotId);
        Assert.Equal("quicksave", slot.DisplayName);
        Assert.Equal(savedAt, slot.SavedAtUtc);
    }

    [Fact]
    public void SaveSlotInfo_round_trips_via_json()
    {
        var savedAt = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        var slot = new SaveSlotInfo(SlotId: "My Save 1", DisplayName: "My Save 1", SavedAtUtc: savedAt);

        string json = JsonSerializer.Serialize(slot);
        var roundTripped = JsonSerializer.Deserialize<SaveSlotInfo>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(slot.SlotId, roundTripped!.SlotId);
        Assert.Equal(slot.DisplayName, roundTripped.DisplayName);
        Assert.Equal(slot.SavedAtUtc, roundTripped.SavedAtUtc);
    }
}
