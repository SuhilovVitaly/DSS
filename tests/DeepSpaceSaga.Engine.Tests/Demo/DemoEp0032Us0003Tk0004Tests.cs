using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0003Tk0004Tests
{
    [Theory]
    [InlineData("before_capture")]
    [InlineData("partial_write")]
    [InlineData("before_replace")]
    public async Task FailedCheckpointRetainsPreviousSlotAndRetryDoesNotResettle(string stage)
    {
        string directory = "D:/DeepSpaceSaga/artifacts/validation/ep-0032/storage-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        try
        {
            var engine = DemoEp0032Us0001Tk0002Tests.Tutorial();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            await connection.SaveAsync(SaveSlotNaming.ContinuationSlotId);
            string path = Path.Combine(directory, SaveSlotNaming.ToFileName(SaveSlotNaming.ContinuationSlotId));
            string before = await File.ReadAllTextAsync(path);
            var start = DemoEp0032Us0001Tk0002Tests.Start(engine, "fault");
            var session = engine.TacticalBattleSessionForTests!;
            session.Battle.Drones.First(d => d.Side == session.PlayerSide).Status = TacticalDroneStatus.Destroyed;
            connection.SaveStageForTests = point => { if (point == stage) throw new IOException("injected " + stage); };
            if (stage == "partial_write") connection.WriteSaveTempForTests = async (temp, json, ct) =>
            {
                await File.WriteAllTextAsync(temp, json[..(json.Length / 2)], ct);
                throw new IOException("injected partial write");
            };
            await Assert.ThrowsAsync<IOException>(() => connection.ExitTacticalBattleAsync(new("exit", start.State!.BattleId)).AsTask());
            Assert.Equal(before, await File.ReadAllTextAsync(path));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            var committed = engine.CaptureSnapshot().DemoCampaign!;
            Assert.Single(committed.Company!.PilotLoss!.Memorial);
            connection.SaveStageForTests = null;
            connection.WriteSaveTempForTests = null;
            Assert.True((await connection.ExitTacticalBattleAsync(new("retry", start.State!.BattleId))).Accepted);
            Assert.Equal(committed.CommitSequence, engine.CaptureSnapshot().DemoCampaign!.CommitSequence);
            var durable = ScenarioLoader.LoadFromJson(await File.ReadAllTextAsync(path), true, true);
            Assert.Single(durable.GameState.DemoCampaign!.PilotMemorial);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
