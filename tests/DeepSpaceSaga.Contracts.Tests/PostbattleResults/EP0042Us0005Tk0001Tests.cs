using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class EP0042Us0005Tk0001Tests
{
    [Fact]
    public void BattleResultsCapacity_ZeroDistinctFromUnknown()
    {
        var legacy = JsonSerializer.Deserialize<LootSessionSnapshot>(
            """{"BattleId":"battle","Phase":"Open","Objects":[],"HasUnclaimed":false}""")!;
        Assert.Null(legacy.FreePrisonerCabins);
        var available = legacy with { FreeCargoKg = 0, FreePrisonerCabins = 0 };
        var restored = JsonSerializer.Deserialize<LootSessionSnapshot>(JsonSerializer.Serialize(available))!;
        Assert.Equal(0, restored.FreePrisonerCabins);
        Assert.Equal(0, restored.FreeCargoKg);
    }
}
