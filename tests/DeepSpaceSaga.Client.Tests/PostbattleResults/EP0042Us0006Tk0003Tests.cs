using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

[Collection("InterfaceLog")]
public sealed class EP0042Us0006Tk0003Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BattleResultsLoad_OpensModalWithoutRestartingBattle(bool legacy)
    {
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ??
            "D:/DeepSpaceSaga/artifacts/validation/ep-0042/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        string directory = Path.Combine(root, "restore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using var original = DemoEp0036Us0007Tk0004Tests.Won();
            var snapshot = original.CaptureSnapshot().DemoCampaign!;
            string battleId = snapshot.Rewards!.Loot!.BattleId;
            if (!legacy)
                Assert.True(original.ExecuteDemoCampaign(new OpenBattleResultsCommand("save-open", snapshot.Revision, battleId)).Applied);
            var saved = original.CaptureSaveState();
            string slot = Path.Combine(directory, "restore.json");
            File.WriteAllText(slot, ScenarioLoader.Serialize(saved));
            await using var fixture = await BattleNavigationTests.Fixture.Create(initialSpeed: SimulationSpeed.Speed0);
            int loads = 0;
            SimulationEngine? loaded = null;
            long now = 1000;
            fixture.LoadSlotOverride = _ =>
            {
                loads++;
                loaded = SimulationEngine.CreateFromSaveFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"), slot);
                loaded.LootClockMilliseconds = () => Interlocked.Read(ref now);
                return new LocalGameSessionConnection(loaded, directory);
            };

            await fixture.Route(ScreenEvent.QuickLoad);
            var results = Assert.IsType<BattleResultsScreen>(fixture.Screens.Current);
            Assert.Equal(battleId, results.Flow.State?.BattleId);
            Assert.False(loaded!.IsTacticalBattleActive);
            Assert.Equal(saved.GameState.DemoCampaign!.LootSession!.DrawCount,
                loaded.CaptureSaveState().GameState.DemoCampaign!.LootSession!.DrawCount);
            Assert.Equal(ScreenEvent.QuickSave, results.OnKeyDown(Key.F5));

            using var bitmap = new SKBitmap(1280, 720);
            using var canvas = new SKCanvas(bitmap);
            results.Render(canvas, bitmap.Width, bitmap.Height);
            results.OnMouseDown(results.CollectAllBounds.MidX, results.CollectAllBounds.MidY, MouseButton.Left);
            Assert.True(await results.LastLootAction!);
            Assert.Equal(ScreenEvent.None, results.OnKeyDown(Key.F5));
            Assert.Equal(ScreenEvent.None, results.OnKeyDown(Key.F9));
            await fixture.Route(ScreenEvent.QuickLoad);
            Assert.Equal(1, loads);
            Assert.Same(results, fixture.Screens.Current);

            Interlocked.Add(ref now, 10000);
            await DemoEp0036Us0007Tk0004Tests.WaitFor(() => results.CanSaveLoad);
            await fixture.Route(results.OnKeyDown(Key.F5));
            Assert.True(File.Exists(Path.Combine(directory, "quicksave.json")));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
