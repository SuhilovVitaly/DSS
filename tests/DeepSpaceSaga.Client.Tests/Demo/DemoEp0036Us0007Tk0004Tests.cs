using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.DebugConsole;
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
    internal static SimulationEngine Won(bool finish = true, bool begin = true)
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
        if (!begin) return engine;
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
    public async Task KillAllOpensLootFromAnActiveEncounter()
    {
        using var engine = Won(finish: false);
        await using var connection = new LocalGameSessionConnection(engine);
        await using var handle = new GameSessionHandle(connection);
        await WaitFor(() => handle.DemoCampaign?.WorldEncounter is not null);
        var flow = new TacticalBattleFlow(handle) { CampaignEncounter = handle.DemoCampaign!.WorldEncounter };
        Assert.True(await flow.StartEncounterAsync(), flow.Error);
        var screen = new TacticalBattleScreen(flow, 1);
        var battle = engine.TacticalBattleSessionForTests!;
        var friends = battle.Battle.Drones.Where(d => d.Side == battle.PlayerSide).ToArray();
        var friendStates = friends.Select(d => (d.Hull, d.Status)).ToArray();
        var enemyCarrier = Assert.Single(battle.Battle.Carriers.Where(c => c.Side != battle.PlayerSide));
        var enemies = battle.Battle.Drones.Where(d => d.Side != battle.PlayerSide).ToArray();
        Assert.True(enemyCarrier.IsActive);
        Assert.NotEmpty(enemies);
        Assert.All(enemies, d => Assert.True(d.IsActive));

        var registry = DebugCommandCatalog.CreateDefault();
        Assert.True(DebugConsoleController.TryParse("kill all", out var name, out var arguments));
        Assert.True(registry.TryGet(name, out var command));
        var context = new DebugCommandContext(registry, new BattleConsoleHost(handle, screen));
        Assert.True((await command.ExecuteAsync(context, "everything", CancellationToken.None)).IsError);
        Assert.True(enemyCarrier.IsActive);
        var result = await command.ExecuteAsync(context, arguments, CancellationToken.None);

        Assert.False(result.IsError, string.Join("; ", result.Lines));
        Assert.False(enemyCarrier.IsActive);
        Assert.All(enemies, d => Assert.False(d.IsActive));
        Assert.Contains(enemies, d => d.Status == TacticalDroneStatus.Destroyed);
        Assert.Contains(enemies, d => d.Status == TacticalDroneStatus.Disabled);
        Assert.Equal(friendStates, friends.Select(d => (d.Hull, d.Status)).ToArray());
        await WaitFor(() => handle.DemoCampaign?.Rewards?.Loot?.Phase == LootPhases.Open);
        var frame = screen.CaptureFrame(1280, 720)!;
        Assert.Equal(TacticalBattlePhase.Over, frame.Battle.Phase);
        var source = handle.DemoCampaign!.Rewards!.Loot!.Objects.First(s => s.Kind == "DisabledDrone");
        var point = frame.View.Project(source.X, source.Y);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(point.X, point.Y, MouseButton.Left));
        Assert.Equal(source.ObjectId, screen.LootWindow!.Presentation.Selected!.ObjectId);
        Assert.True(Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.EncounterResults).PlayerWon);
    }

    [Fact]
    public async Task KillAllTutorialLootSurvivesSaveWithManagedResults()
    {
        string settings = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        string scenario = Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");
        using var engine = SimulationEngine.CreateFromScenarioFile(settings, scenario);
        await using var handle = new GameSessionHandle(new LocalGameSessionConnection(engine));
        var carrier = await handle.WaitForCarrierOperationsAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(carrier);
        await handle.SetSpeedAsync(SimulationSpeed.Speed0);
        var flow = new TacticalBattleFlow(handle) { IsTutorial = true, RequireBattleResults = true };
        var started = await flow.StartAsync(new("tutorial-start", SkiaWindow.TutorialBattleSeed, SkiaWindow.TutorialBattleScenarioId)
        {
            SourceShipId = carrier.ShipId,
            ExpectedCarrierRevision = carrier.Revision
        });
        Assert.True(started?.Accepted, started?.Code.ToString());
        var screen = new TacticalBattleScreen(flow, 1);
        var registry = DebugCommandCatalog.CreateDefault();
        Assert.True(registry.TryGet("kill", out var command));
        var result = await command.ExecuteAsync(new(registry, new BattleConsoleHost(handle, screen)), "all", CancellationToken.None);
        Assert.False(result.IsError, string.Join("; ", result.Lines));
        await WaitFor(() => handle.DemoCampaign?.BattleResults?.Stage == BattleResultsStage.AwaitingOpen);
        Assert.True(handle.DemoCampaign!.BattleResults!.CanLoot);
        Assert.Null(handle.DemoCampaign.WorldEncounter);
        Assert.Equal(ScreenEvent.OpenBattleResults, screen.OnKeyDown(Key.Enter, KeyModifiers.None, false));
        Assert.True(await flow.OpenResultsAsync());

        var save = engine.CaptureSaveState();
        Assert.True(save.GameState.DemoCampaign!.LootSession!.DebugSortie);
        Assert.Equal(BattleResultsStage.Open, save.GameState.DemoCampaign.BattleResults!.Stage);
        Assert.Empty(save.GameState.DemoCampaign.EncounterResults);

        using var loaded = SimulationEngine.CreateFromScenarioFile(settings, scenario);
        loaded.LoadScenario(save, isSave: true);
        await using var recovered = new GameSessionHandle(new LocalGameSessionConnection(loaded));
        var recoveredFlow = await SkiaWindow.RecoverBattleFlowAsync(recovered);
        Assert.NotNull(recoveredFlow);
        Assert.True(recoveredFlow.RestoreResultsModal);
        Assert.Equal(flow.State!.BattleId, recoveredFlow.State!.BattleId);
        Assert.Contains(recovered.DemoCampaign!.Rewards!.Loot!.Objects, source => source.Kind == "DisabledDrone");
        Assert.Contains(recovered.DemoCampaign.Rewards.Loot.Objects, source => source.Kind == "DestroyedDrone");
    }

    private sealed class BattleConsoleHost(GameSessionHandle session, TacticalBattleScreen screen) : IDebugConsoleHost
    {
        public GameSessionHandle Session => session;
        public TacticalBattleScreen ActiveBattle => screen;
        public bool IsTransitionBusy => false;
        public Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> operation,
            CancellationToken cancellationToken) => operation(cancellationToken);
        public Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession,
            CancellationToken cancellationToken) => throw new NotSupportedException();
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
