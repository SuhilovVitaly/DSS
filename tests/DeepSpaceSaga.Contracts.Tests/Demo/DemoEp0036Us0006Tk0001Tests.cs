using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0036Us0006Tk0001Tests
{
    [Fact]
    public void DuplicateTypeDifferentSlot_PreservesIdentity()
    {
        var first = new ModuleLootSnapshot("battle/drone/engine/0", "engine", "drive.standard", "item.trophy.engine.drive.standard",
            "Drive", "intact", 1000, 2625, LootAttemptStates.NotAttempted, true, null);
        var second = first with { InstanceId = "battle/drone/engine/1" };
        Assert.NotEqual(first.InstanceId, second.InstanceId);
        Assert.Equal(first.CatalogId, second.CatalogId);
    }

    [Theory]
    [InlineData(2375)]
    [InlineData(2625)]
    public void ChanceBasisPoints_Roundtrip(int chance)
    {
        var row = new ModuleLootSnapshot("instance", "weapon", "gun", "item", "Gun", "intact", 100,
            chance, LootAttemptStates.NotAttempted, true, null);
        Assert.Equal(row, JsonSerializer.Deserialize<ModuleLootSnapshot>(JsonSerializer.Serialize(row)));
    }

    [Fact]
    public void CommandCannotOverrideItemOrChance() => Assert.Equal(
        new[] { "BattleId", "CommandId", "ExpectedRevision", "InstanceId", "ObjectId" },
        typeof(AttemptModuleSalvageCommand).GetProperties().Select(p => p.Name).Order());
}
