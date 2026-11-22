using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Tests.Demo;

namespace DeepSpaceSaga.Engine.Tests.PostbattleResults;

public sealed class EP0042Us0006Tk0002Tests
{
    [Fact]
    public async Task LootCheckpoint_RetrySameReceiptAndBlockMidBatch()
    {
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ??
            "D:/DeepSpaceSaga/artifacts/validation/ep-0042/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        string directory = Path.Combine(root, "checkpoint-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var engine = DemoEp0036Us0007Tk0003Tests.Won();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            long now = 100;
            engine.LootClockMilliseconds = () => now;
            var loot = Current().Rewards!.Loot!;
            var wreck = loot.Objects.First(source => source.Scrap is { CanAttempt: true });
            var row = new LootRowKey(wreck.ObjectId, LootRowKind.Scrap, wreck.Scrap!.ItemTypeId);
            var opened = await connection.ExecuteDemoCampaignAsync(new OpenBattleResultsCommand("open", Current().Revision, loot.BattleId));
            Assert.True(opened.Applied);
            string checkpoint = Path.Combine(directory, SaveSlotNaming.ToFileName(SaveSlotNaming.ContinuationSlotId));
            string stable = File.ReadAllText(checkpoint);
            Assert.True((await connection.ExecuteDemoCampaignAsync(new StartLootOperationsCommand("start", Current().Revision, loot.BattleId, row))).Applied);
            await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync("quicksave").AsTask());
            await Assert.ThrowsAsync<InvalidOperationException>(() => connection.RequestAutosaveAsync(Current().CommitSequence).AsTask());
            Assert.Equal(stable, File.ReadAllText(checkpoint));

            now += 10000;
            connection.SaveStageForTests = stage =>
            {
                if (stage == "before_replace") throw new IOException("Injected final checkpoint failure");
            };
            var advance = new AdvanceLootOperationsCommand("advance", Current().Revision, loot.BattleId);
            await Assert.ThrowsAsync<IOException>(() => connection.ExecuteDemoCampaignAsync(advance).AsTask());
            Assert.Equal(stable, File.ReadAllText(checkpoint));
            Assert.Empty(Current().Rewards!.Loot!.Operations);
            long draws = engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!.DrawCount;
            await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync("quicksave").AsTask());

            connection.SaveStageForTests = null;
            var retried = await connection.ExecuteDemoCampaignAsync(advance);
            Assert.True(retried.Applied);
            var restored = ScenarioLoader.LoadFromFile(checkpoint, allowNonZeroGameTime: true, isSave: true);
            Assert.Equal(draws, restored.GameState.DemoCampaign!.LootSession!.DrawCount);
            Assert.Equal(BattleResultsStage.Open, restored.GameState.DemoCampaign!.BattleResults!.Stage);
            Assert.Equal(draws, engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!.DrawCount);
            await connection.SaveAsync("quicksave");

            DemoCampaignSnapshot Current() => engine.CaptureSnapshot().DemoCampaign!;
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
