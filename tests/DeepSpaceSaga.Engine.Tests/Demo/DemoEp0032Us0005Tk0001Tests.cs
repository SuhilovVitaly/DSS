using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0005Tk0001Tests
{
    [Fact]
    public async Task AutosaveAckIsDurableAndNewestCorruptSlotDoesNotHidePrevious()
    {
        string directory = "D:/DeepSpaceSaga/artifacts/validation/ep-0032/autosave-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        try
        {
            var engine = DemoEp0032Us0001Tk0002Tests.Tutorial();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            await connection.SaveAsync("manual");
            string manual = await File.ReadAllTextAsync(Path.Combine(directory, "manual.json"));
            var start = DemoEp0032Us0001Tk0002Tests.Start(engine, "autosave");
            await Assert.ThrowsAsync<InvalidOperationException>(() => connection.RequestAutosaveAsync(0).AsTask());
            engine.ExitTacticalBattle(new("exit", start.State!.BattleId));
            long sequence = engine.CaptureSnapshot().DemoCampaign!.CommitSequence;
            await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => connection.RequestAutosaveAsync(sequence).AsTask()));
            string path = Path.Combine(directory, SaveSlotNaming.ToFileName(SaveSlotNaming.ContinuationSlotId));
            Assert.Equal(sequence, ScenarioLoader.LoadFromFile(path, true, true).GameState.DemoCampaign!.CommitSequence);
            Assert.Equal(manual, await File.ReadAllTextAsync(Path.Combine(directory, "manual.json")));
            await File.WriteAllTextAsync(Path.Combine(directory, "broken.json"), "{incomplete");
            File.SetLastWriteTimeUtc(Path.Combine(directory, "broken.json"), DateTime.UtcNow.AddMinutes(1));
            Assert.Equal(SaveSlotNaming.ContinuationSlotId, SaveSlotRepository.GetLatestValidSlot(directory,
                DemoEp0032Us0001Tk0002Tests.ContentRoot + "/Settings.json")!.SlotId);
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
