using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class EP0042Us0003Tk0001Tests
{
    [Fact]
    public void LootOperationContract_LegacyAndNewSnapshotRoundTrip()
    {
        var legacy = JsonSerializer.Deserialize<LootSessionSnapshot>(
            """{"BattleId":"battle","Phase":"Open","Objects":[],"HasUnclaimed":true}""")!;
        Assert.Empty(legacy.Operations);

        var row = new LootRowKey("drone", LootRowKind.Module, "slot-2");
        var snapshot = legacy with { Operations = [new("op", row, 5000, 3200)] };
        var restored = JsonSerializer.Deserialize<LootSessionSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(snapshot.Operations[0], Assert.Single(restored.Operations));
        var request = new StartLootOperationsCommand("single", 4, snapshot.BattleId, row);
        Assert.Equal(request, JsonSerializer.Deserialize<StartLootOperationsCommand>(JsonSerializer.Serialize(request)));
        Assert.Null(new StartLootOperationsCommand("all", 4, snapshot.BattleId).Row);
    }
}
