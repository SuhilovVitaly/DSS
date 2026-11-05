using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class WorldGuardTests
{
    private static SimulationEngine Engine()
    {
        string root = Path.GetFullPath(Path.Combine(ReferenceFixtureTests.Root, "../../../.."));
        var engine = SimulationEngine.CreateFromSettingsFile(Path.Combine(root, "src/DeepSpaceSaga.Client/Settings.json"));
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExitAndTacticalDefeatPreserveTheCompleteSavedWorld(bool defeat)
    {
        using var engine = Engine();
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        Assert.False(engine.CaptureSnapshot().IsTacticalBattleActive);
        Assert.True(engine.StartTacticalBattle(new("start", 1)).Accepted);
        Assert.True(engine.CaptureSnapshot().IsTacticalBattleActive);
        if (defeat)
        {
            engine.TacticalBattleSessionForTests!.Battle.Carrier("A").Hull = 0;
            engine.TacticalBattleSessionForTests.Battle.CheckVictory();
            Assert.Equal(TacticalBattleOutcomeCode.B, engine.GetTacticalBattleState()!.Outcome!.Kind);
        }
        else
        {
            for (int i = 0; i < 100; i++)
            {
                var state = engine.GetTacticalBattleState()!;
                if (state.Phase == TacticalBattlePhase.Over) break;
                var response = state.Phase == TacticalBattlePhase.Move
                    ? engine.MoveTacticalBattle(new($"move-{i}", state.BattleId, state.Revision))
                    : engine.FireTacticalBattle(new($"fire-{i}", state.BattleId, state.Revision,
                        [.. state.Targets.Select(t => new TacticalFireTarget(t.DroneId, t.DefaultTargetId))]));
                Assert.True(response.Accepted);
            }
            Assert.Equal(TacticalBattlePhase.Over, engine.GetTacticalBattleState()!.Phase);
        }
        Assert.Equal(SessionOutcomeState.Running, engine.CaptureSnapshot().EffectiveOutcome.State);
        AssertDenied(() => engine.CaptureSaveState()); // over remains active until Exit
        Assert.True(engine.ExitTacticalBattle(new("exit", "start")).Accepted);
        Assert.False(engine.CaptureSnapshot().IsTacticalBattleActive);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void WorldCommandsAreRefusedEvenIfBattleExitsBeforeTheirNextTick()
    {
        using var engine = Engine();
        var before = engine.CaptureSaveState();
        engine.StartTacticalBattle(new("start", 1));
        var command = new PlayerCommand("world", 0, before.GameState.PlayerShipObjectId, "none", "unknown");
        engine.ReceiveCommand(command);
        engine.ExitTacticalBattle(new("exit", "start"));
        var result = Assert.Single(engine.CaptureSnapshot().CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(SimulationEngine.TacticalBattleActiveReason, result.ReasonCode);
        engine.ReceiveCommand(command);
        Assert.Equal(result, Assert.Single(engine.CaptureSnapshot().CommandResults));
        var after = engine.CaptureSaveState();
        Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(after with
        { GameState = after.GameState with { CommandReceipts = before.GameState.CommandReceipts } }));
    }

    [Fact]
    public void QueuedCommandsLoadTravelAndAccelerationCannotMutateBattleWorld()
    {
        using var engine = Engine();
        var before = engine.CaptureSaveState();
        engine.ReceiveCommand(new("queued", 0, before.GameState.PlayerShipObjectId, "none", "unknown"));
        engine.ReceiveDialogueCommand(new("queued-dialogue", DialogueAction.Start, "instance", 0));
        engine.StartTacticalBattle(new("start", 1));
        AssertDenied(() => engine.CaptureSaveState());
        AssertDenied(() => engine.LoadScenario(before, isSave: true));
        AssertDenied(() => engine.ReceiveDialogueCommand(new("dialogue", DialogueAction.Start, "instance", 0)));
        Assert.Equal(SimulationEngine.TacticalBattleActiveReason, engine.TrySetSpeed(SimulationSpeed.Speed2));
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed0));
        var travel = engine.TravelStation(new("travel", StationDistrict.Market));
        Assert.False(travel.Accepted);
        Assert.Equal(SimulationEngine.TacticalBattleActiveReason, travel.Error);
        Assert.Equal(before.GameState.GameTimeMs, travel.Snapshot.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed0, travel.Snapshot.CurrentSpeed);
        Assert.Equal(SimulationEngine.TacticalBattleActiveReason, Assert.Single(travel.Snapshot.CommandResults).ReasonCode);
        Assert.Contains(travel.Snapshot.DialogueEvents, e => e.CommandId == "queued-dialogue" && e.EventCode == SimulationEngine.TacticalBattleActiveReason);
        engine.ExitTacticalBattle(new("exit", "start"));
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed1));
        engine.SetSpeed(SimulationSpeed.Speed0);
        engine.LoadScenario(before, isSave: true);
        Assert.False(engine.CaptureSnapshot().IsTacticalBattleActive);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveAndQuicksaveRefusalWritesNoDirectoryOrSlotBytes(bool existing)
    {
        string directory = Path.Combine(Path.GetTempPath(), "tactical-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            var engine = Engine();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            // No reader is started: the tick loop awaits transport selection.
            if (existing)
            {
                await connection.SaveAsync("quicksave");
            }
            string path = Path.Combine(directory, "quicksave.json");
            byte[]? original = existing ? await File.ReadAllBytesAsync(path) : null;
            engine.StartTacticalBattle(new("start", 1));
            foreach (string slot in new[] { "quicksave", "manual" })
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync(slot).AsTask());
                Assert.Equal(SimulationEngine.TacticalBattleActiveReason, error.Message);
            }
            if (existing)
            {
                Assert.Equal(original, await File.ReadAllBytesAsync(path));
                Assert.Single(Directory.GetFiles(directory));
            }
            else Assert.False(Directory.Exists(directory));
            engine.ExitTacticalBattle(new("exit", "start"));
            await connection.SaveAsync("manual");
            Assert.True(File.Exists(Path.Combine(directory, "manual.json")));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static void AssertDenied(Action action) => Assert.Equal(SimulationEngine.TacticalBattleActiveReason,
        Assert.Throws<InvalidOperationException>(action).Message);
}
