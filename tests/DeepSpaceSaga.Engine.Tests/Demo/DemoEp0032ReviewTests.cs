using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032ReviewTests
{
    private const string Root = DemoEp0032Us0001Tk0002Tests.ContentRoot;
    private static string Temp()
    {
        var path = "D:/DeepSpaceSaga/artifacts/validation/ep-0032-review/fixtures/" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public async Task FirstSortiePreservesLivingContinuationAfterCarrierDeathWithoutF5()
    {
        string directory = Temp();
        try
        {
            var engine = DemoEp0032Us0001Tk0002Tests.Tutorial();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            engine.SetSpeed(SimulationSpeed.Speed0);
            var carrier = engine.CaptureSnapshot().CarrierOperations!;
            var started = await connection.StartTacticalBattleAsync(new("start", 1, "initial")
            { SourceShipId = carrier.ShipId, ExpectedCarrierRevision = carrier.Revision });
            Assert.True(started.Accepted);
            string path = Path.Combine(directory, SaveSlotNaming.ToFileName(SaveSlotNaming.ContinuationSlotId));
            string alive = File.ReadAllText(path);
            engine.TacticalBattleSessionForTests!.Battle.Carriers.Single(c => c.Side == engine.TacticalBattleSessionForTests.PlayerSide).Hull = 0;
            await connection.ExitTacticalBattleAsync(new("exit", started.State!.BattleId));
            await connection.RequestAutosaveAsync(engine.CaptureSnapshot().DemoCampaign!.CommitSequence);
            Assert.Equal(alive, File.ReadAllText(path));
            Assert.False(File.Exists(Path.Combine(directory, "quicksave.json")));
            await connection.SaveAsync("terminal-manual");
            File.SetLastWriteTimeUtc(Path.Combine(directory, "terminal-manual.json"), DateTime.UtcNow.AddMinutes(1));
            Assert.Equal(SaveSlotNaming.ContinuationSlotId, SaveSlotRepository.GetLatestValidSlot(directory, Root + "/Settings.json")!.SlotId);
            Assert.Equal(SaveSlotNaming.ContinuationSlotId, SaveSlotRepository.GetLatestValidSlot(directory)!.SlotId);
        }
        finally { Directory.Delete(directory, true); }
    }

}
