using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0007Tk0005Tests
{
    [Fact]
    public void SavedReceiptsDoNotDuplicateLootAndRetryDoesNotReplay()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var command = new FinishBattleLootCommand("compact-close", state.Revision, state.Rewards!.Loot!.BattleId, true);
        Assert.True(engine.ExecuteDemoCampaign(command).Applied);
        int before = ScenarioLoader.Serialize(engine.CaptureSaveState()).Length;
        for (int i = 0; i < 100; i++)
            Assert.False(engine.ExecuteDemoCampaign(command with { CommandId = "rejected-" + i }).Applied);
        var save = engine.CaptureSaveState();
        Assert.All(save.GameState.DemoCampaign!.Receipts, r => Assert.Null(r.Response.Rewards));
        Assert.InRange(ScenarioLoader.Serialize(save).Length - before, 1, 100_000);
        using var restored = DemoEp0033Us0001Tk0003Tests.Demo();
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        string saved = ScenarioLoader.Serialize(restored.CaptureSaveState());
        var response = restored.ExecuteDemoCampaign(command);
        Assert.True(response.Applied);
        Assert.Equal(LootPhases.Closed, response.Rewards!.Loot!.Phase);
        Assert.Equal(restored.CaptureSnapshot().DemoCampaign!.Revision, response.Revision);
        Assert.Equal(saved, ScenarioLoader.Serialize(restored.CaptureSaveState()));
    }

    private static string DirectoryForTest() => Directory.CreateDirectory(
        "D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/io-" + Guid.NewGuid().ToString("N")).FullName;
    private static string SavePath(string directory) => Path.Combine(directory, SaveSlotNaming.ToFileName(SaveSlotNaming.ContinuationSlotId));

    [Fact]
    public async Task ClosedVictoryReceiptDoesNotWriteAnotherCheckpoint()
    {
        string directory = DirectoryForTest();
        try
        {
            var engine = Ep0035Us15Tk3Tests.Begin();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            var session = engine.TacticalBattleSessionForTests!;
            var enemies = session.Battle.Drones.Where(d => d.Side != session.PlayerSide).ToArray();
            foreach (var drone in enemies.Skip(1)) drone.Status = TacticalDroneStatus.Disabled;
            var command = new DebugDestroyTacticalUnitRequest("last", session.Id, session.Revision, enemies[0].Id);
            Assert.True((await connection.DebugDestroyTacticalUnitAsync(command)).Accepted);
            var campaign = engine.CaptureSnapshot().DemoCampaign!;
            Assert.True((await connection.ExecuteDemoCampaignAsync(new FinishBattleLootCommand("finish", campaign.Revision, session.Id, true))).Applied);
            DemoEp0032Us0001Tk0002Tests.Start(engine, "next-battle");
            Assert.True((await connection.DebugDestroyTacticalUnitAsync(command)).Accepted);
            Assert.True(engine.IsTacticalBattleActive);
            Assert.Null(typeof(LocalGameSessionConnection).GetField("_pendingLootCheckpoint", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(connection));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task LootOpenRoundtripResumesFrozenMap_AndAutosaveDoesNotRestorePrebattle()
    {
        string directory = DirectoryForTest();
        try
        {
            var engine = Ep0035Us15Tk3Tests.Begin();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            var session = engine.TacticalBattleSessionForTests!;
            var enemies = session.Battle.Drones.Where(d => d.Side != session.PlayerSide).ToArray();
            foreach (var drone in enemies.Skip(1)) drone.Status = TacticalDroneStatus.Disabled;
            Assert.True((await connection.DebugDestroyTacticalUnitAsync(new("last", session.Id, session.Revision, enemies[0].Id))).Accepted);
            var first = ScenarioLoader.LoadFromJson(await File.ReadAllTextAsync(SavePath(directory)), true, true);
            Assert.Equal(LootPhases.Open, first.GameState.DemoCampaign!.LootSession!.Phase);
            await connection.RequestAutosaveAsync(engine.CaptureSnapshot().DemoCampaign!.CommitSequence);
            var saved = ScenarioLoader.LoadFromJson(await File.ReadAllTextAsync(SavePath(directory)), true, true);
            Assert.Equal(JsonSerializer.Serialize(first.GameState.DemoCampaign.LootSession), JsonSerializer.Serialize(saved.GameState.DemoCampaign!.LootSession));
            using var restored = DemoEp0033Us0001Tk0003Tests.Demo();
            restored.LoadScenario(saved, true);
            Assert.False(restored.IsTacticalBattleActive);
            Assert.Equal(session.Id, restored.GetTacticalBattleState()!.BattleId);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task CheckpointFailureRetryNoDoubleGrant_AndPartialCollectionRoundtrip()
    {
        string directory = DirectoryForTest();
        try
        {
            var engine = DemoEp0036Us0007Tk0003Tests.Won();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            await connection.SaveAsync(SaveSlotNaming.ContinuationSlotId);
            string original = await File.ReadAllTextAsync(SavePath(directory));
            var campaign = engine.CaptureSnapshot().DemoCampaign!;
            var loot = campaign.Rewards!.Loot!;
            var source = loot.Objects.First(s => s.Consumables.Any(c => c.Remaining > 0));
            var item = source.Consumables.First(c => c.Remaining > 0);
            var command = new CollectLootConsumablesCommand("collect", campaign.Revision, loot.BattleId, source.ObjectId, item.ItemTypeId, 1);
            long before = engine.CaptureSnapshot().InstalledModules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeId == item.ItemTypeId).Sum(c => c.Quantity);
            connection.SaveStageForTests = stage => { if (stage == "before_replace") throw new IOException("injected storage failure"); };
            await Assert.ThrowsAsync<IOException>(() => connection.ExecuteDemoCampaignAsync(command).AsTask());
            Assert.Equal(original, await File.ReadAllTextAsync(SavePath(directory)));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.Equal(before + 1, engine.CaptureSnapshot().InstalledModules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeId == item.ItemTypeId).Sum(c => c.Quantity));
            connection.SaveStageForTests = null;
            Assert.True((await connection.ExecuteDemoCampaignAsync(command)).Applied);
            Assert.Equal(before + 1, engine.CaptureSnapshot().InstalledModules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeId == item.ItemTypeId).Sum(c => c.Quantity));
            using var restored = DemoEp0033Us0001Tk0003Tests.Demo();
            restored.LoadScenario(ScenarioLoader.LoadFromJson(await File.ReadAllTextAsync(SavePath(directory)), true, true), true);
            Assert.Equal(item.Remaining - 1, restored.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Objects.Single(s => s.ObjectId == source.ObjectId)
                .Consumables.Single(c => c.ItemTypeId == item.ItemTypeId).Remaining);
        }
        finally { Directory.Delete(directory, true); }
    }
}
