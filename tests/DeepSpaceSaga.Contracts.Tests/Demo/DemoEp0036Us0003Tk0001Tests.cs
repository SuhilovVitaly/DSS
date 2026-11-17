using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0036Us0003Tk0001Tests
{
    [Fact]
    public void DtoRoundtrip_PreservesObjectIdentity()
    {
        var source = new LootObjectSnapshot("drone:1", "DisabledDrone", [new("torpedo", 4, true, null)], null,
            [new("weapon:0", "weapon", "weapon.a", "trophy.a", "Weapon", "intact", 120, 3500, LootAttemptStates.NotAttempted, true, null)],
            new("pilot:1", "Pilot", null, true, null), 12, 34);
        var snapshot = new LootSessionSnapshot("battle:1", LootPhases.Open, [source], true);
        var restored = JsonSerializer.Deserialize<LootSessionSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal("battle:1", restored.BattleId);
        Assert.Equal(source.ObjectId, Assert.Single(restored.Objects).ObjectId);
        Assert.Equal(source.Modules[0], restored.Objects[0].Modules[0]);
        Assert.Equal(source.Pilot, restored.Objects[0].Pilot);
    }

    [Fact]
    public void MissingRewards_IsUnavailable() => Assert.Null(JsonSerializer.Deserialize<DemoRewardsSnapshot>("{}")!.Loot);

    [Fact]
    public void CommandCarriesNoClientPriceOrChance()
    {
        DemoCampaignCommand[] commands = [new CollectLootConsumablesCommand("c", 1, "b", "o", "i", 2),
            new AttemptScrapCommand("s", 1, "b", "o"), new FinishBattleLootCommand("f", 1, "b")];
        foreach (var command in commands)
        {
            Assert.IsAssignableFrom<ILootCampaignCommand>(command);
            Assert.DoesNotContain(command.GetType().GetProperties(), p => p.Name.Contains("Chance") || p.Name.Contains("Price") || p.Name.Contains("Result"));
        }
    }
}
