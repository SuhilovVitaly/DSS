using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us16Tk2Tests
{
    [Fact]
    public async Task FailedPendingWriteNeverStartsAndFailedFinishedWriteRetriesOnlyStorage()
    {
        string directory = "D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/storage-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        try
        {
            var engine = DemoEp0033Us0001Tk0003Tests.Demo();
            engine.LoadScenario(Ep0035Us3Tk2Tests.CapturedScenario(engine), true);
            var campaign = engine.CaptureSnapshot().DemoCampaign!;
            engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", campaign.Revision, campaign.PendingSensorReport!.BatchId));
            campaign = engine.CaptureSnapshot().DemoCampaign!;
            var begin = new BeginEncounterBattleCommand("begin", campaign.Revision, campaign.WorldEncounter!.Binding.EncounterId);
            await using var connection = new LocalGameSessionConnection(engine, directory);
            await connection.SaveAsync(SaveSlotNaming.ContinuationSlotId);
            string path = Path.Combine(directory, SaveSlotNaming.ToFileName(SaveSlotNaming.ContinuationSlotId));
            string original = await File.ReadAllTextAsync(path);
            connection.SaveStageForTests = stage => { if (stage == "before_replace") throw new IOException("injected"); };
            await Assert.ThrowsAsync<IOException>(() => connection.ExecuteDemoCampaignAsync(begin).AsTask());
            Assert.False(engine.IsTacticalBattleActive);
            Assert.Equal(original, await File.ReadAllTextAsync(path));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            connection.SaveStageForTests = null;
            Assert.True((await connection.ExecuteDemoCampaignAsync(begin)).Applied);
            string pending = await File.ReadAllTextAsync(path);
            Assert.Equal(WorldEncounterPhases.PendingBattle, ScenarioLoader.LoadFromJson(pending, true, true).GameState.DemoCampaign!.WorldEncounter!.Phase);
            await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync(SaveSlotNaming.ContinuationSlotId).AsTask());
            Assert.Equal(pending, await File.ReadAllTextAsync(path));
            var session = engine.TacticalBattleSessionForTests!;
            session.Battle.EndWithTimeout(1);
            connection.SaveStageForTests = stage => { if (stage == "before_replace") throw new IOException("injected result"); };
            await Assert.ThrowsAsync<IOException>(() => connection.ExitTacticalBattleAsync(new("finish", session.Id)).AsTask());
            Assert.Equal(pending, await File.ReadAllTextAsync(path));
            Assert.Equal("encounter_checkpoint_pending", engine.TrySetSpeed(SimulationSpeed.Speed1));
            long committed = engine.CaptureSnapshot().DemoCampaign!.CommitSequence;
            connection.SaveStageForTests = null;
            Assert.True((await connection.ExitTacticalBattleAsync(new("retry", session.Id))).Accepted);
            Assert.Equal(committed, engine.CaptureSnapshot().DemoCampaign!.CommitSequence);
            var finished = ScenarioLoader.LoadFromJson(await File.ReadAllTextAsync(path), true, true).GameState.DemoCampaign!;
            Assert.Single(finished.EncounterResults);
            Assert.Equal(WorldEncounterPhases.Finished, finished.WorldEncounter!.Phase);
            Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed0));
        }
        finally
        {
            string target = Path.GetFullPath(directory);
            Assert.StartsWith("D:\\DeepSpaceSaga\\artifacts\\validation\\ep-0035\\20261117-resume\\", target, StringComparison.OrdinalIgnoreCase);
            Directory.Delete(target, true);
        }
    }
}
