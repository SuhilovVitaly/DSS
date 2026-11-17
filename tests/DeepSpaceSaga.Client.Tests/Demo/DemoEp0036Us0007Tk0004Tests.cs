using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0007Tk0004Tests
{
    internal static SimulationEngine Won(bool finish = true)
    {
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(AppContext.BaseDirectory + "/Settings.json", AppContext.BaseDirectory + "/Scenarios/Demo/scenario.json");
        var save = engine.CaptureSaveState();
        var campaign = save.GameState.DemoCampaign!;
        var world = campaign.PirateWorld!; var group = world.Groups[0];
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == save.GameState.PlayerShipObjectId ? s with
                { PositionX = group.X, PositionY = group.Y + 1500, SpeedMps = 0, Orbit = null, IsDocked = false, DockedStationObjectId = null } : s).ToArray(),
                DemoCampaign = campaign with
                {
                    PirateWorld = world with
                    {
                        Groups = world.Groups.SetItem(0, group with
                        { Phase = "captured", CapturedMotionTimeMs = world.LastMotionTimeMs, MotionPlan = null })
                    }
                }
            }
        }, true);
        var current = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", current.Revision, current.PendingSensorReport!.BatchId)).Applied);
        current = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new BeginEncounterBattleCommand("begin", current.Revision, current.WorldEncounter!.Binding.EncounterId)).Applied);
        if (!finish) return engine;
        var battle = engine.TacticalBattleSessionForTests!;
        foreach (var drone in battle.Battle.Drones.Where(d => d.Side == "B")) drone.Status = TacticalDroneStatus.Disabled;
        battle.Battle.CheckVictory();
        Assert.True(engine.ExitTacticalBattle(new("to-loot", battle.Id)).Accepted);
        return engine;
    }

    internal static async Task WaitFor(Func<bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!predicate()) { Assert.True(DateTime.UtcNow < deadline, "Session projection timeout"); await Task.Delay(20); }
    }

    [Fact]
    public async Task FailedOpeningCheckpointRecoversFrozenMap()
    {
        string directory = Directory.CreateDirectory("D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/io-" + Guid.NewGuid().ToString("N")).FullName;
        try
        {
            using var engine = Won(false);
            await using var connection = new LocalGameSessionConnection(engine, directory);
            await using var handle = new GameSessionHandle(connection);
            await WaitFor(() => handle.DemoCampaign?.WorldEncounter is not null);
            var flow = new TacticalBattleFlow(handle) { CampaignEncounter = handle.DemoCampaign!.WorldEncounter };
            Assert.True(await flow.StartEncounterAsync());
            var screen = new TacticalBattleScreen(flow, 1);
            var battle = engine.TacticalBattleSessionForTests!;
            var enemies = battle.Battle.Drones.Where(d => d.Side != battle.PlayerSide).ToArray();
            foreach (var drone in enemies.Skip(1)) drone.Status = TacticalDroneStatus.Disabled;
            connection.SaveStageForTests = stage => { if (stage == "before_replace") throw new IOException("opening checkpoint failure"); };
            var command = new DebugDestroyTacticalUnitRequest("last", battle.Id, battle.Revision, enemies[0].Id);
            var response = await flow.DebugDestroyAsync(command);
            Assert.Equal(TacticalBattleRejectionCode.Unavailable, response!.Code);
            await WaitFor(() => handle.DemoCampaign?.Rewards?.Loot?.Phase == LootPhases.Open);
            Assert.Equal(TacticalBattlePhase.Over, screen.CaptureFrame(1280, 720)!.Battle.Phase);
            connection.SaveStageForTests = null;
            Assert.True((await flow.DebugDestroyAsync(command))!.Accepted);
            Assert.Equal(TacticalBattlePhase.Over, flow.State!.Phase);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task ClickFrozenDroneOpensItsWindow_AndEscapeWarns()
    {
        using var engine = Won();
        await using var connection = new LocalGameSessionConnection(engine);
        await using var handle = new GameSessionHandle(connection);
        await WaitFor(() => handle.DemoCampaign?.Rewards?.Loot is not null);
        var flow = new TacticalBattleFlow(handle) { CampaignEncounter = handle.DemoCampaign!.WorldEncounter };
        Assert.True(await flow.StartEncounterAsync());
        var screen = new TacticalBattleScreen(flow, 1);
        var frame = screen.CaptureFrame(1280, 720)!;
        var source = handle.DemoCampaign.Rewards!.Loot!.Objects.First(s => s.Kind == "DisabledDrone");
        var point = frame.View.Project(source.X, source.Y);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(point.X, point.Y, MouseButton.Left));
        Assert.Equal(source.ObjectId, screen.LootWindow!.Presentation.Selected!.ObjectId);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape, KeyModifiers.None, false));
        Assert.True(screen.LootWindow.ConfirmDiscard);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape, KeyModifiers.None, false));
        Assert.False(screen.LootWindow.ConfirmDiscard);
        Assert.Equal(LootPhases.Open, engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Phase);
        screen.OnKeyDown(Key.Escape, KeyModifiers.None, false);
        Assert.Equal(ScreenEvent.CloseTacticalBattle, screen.OnKeyDown(Key.Enter, KeyModifiers.None, false));
        Assert.True(await flow.ExitAsync());
        Assert.Equal(LootPhases.Closed, engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Phase);
    }

    [Fact]
    public void LoadRestoresMapCoordinates_AndWindowFitsViewport()
    {
        using var engine = Won();
        var save = engine.CaptureSaveState();
        engine.LoadScenario(DeepSpaceSaga.Engine.Scenario.ScenarioLoader.LoadFromJson(
            DeepSpaceSaga.Engine.Scenario.ScenarioLoader.Serialize(save), true, true), true);
        var loot = engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!;
        var view = new TacticalBattleView(1280, 720, 0, 0, 1);
        foreach (var source in loot.Objects)
            Assert.NotNull(TacticalBattleMapRenderer.PickLootSource(view, loot, view.Project(source.X, source.Y)));
        var presentation = new BattleLootPresentation(_ => throw new InvalidOperationException());
        presentation.Observe(loot, 0); presentation.Select(loot.Objects[0].ObjectId);
        var window = new BattleLootWindow(presentation);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        window.Draw(surface.Canvas, 1280, 720, false);
        Assert.True(new SKRect(0, 0, 1280, 720).Contains(window.Bounds));
        Assert.False(window.RequestExit());
        window.Draw(surface.Canvas, 1280, 720, false);
        Assert.False(window.ConfirmBounds.IntersectsWith(window.CancelBounds));
    }
}
