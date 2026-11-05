using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Availability = DeepSpaceSaga.Engine.TacticalBattle.Content.TacticalBattleAvailability;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class LocalSessionBattleTests
{
    private static readonly Lazy<TacticalBattleCatalog> Catalog = new(AssemblyTests.Catalog);
    private static readonly JsonElement[] Rows = ReadRows();
    public static IEnumerable<object[]> Cases() => Rows.Select((row, index) => new object[]
        { row.GetProperty("seed").GetInt32(), row.GetProperty("tactic").GetString()!, index });

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task EveryIndependentBattleThroughConnectionMatchesInBothOrderModes(int seed, string tactic, int index)
    {
        var expected = Rows[index];
        var implicitFinal = await Run(seed, tactic, expected, explicitOrders: false);
        var explicitFinal = await Run(seed, tactic, expected, explicitOrders: true);
        Assert.Equal(JsonSerializer.Serialize(implicitFinal), JsonSerializer.Serialize(explicitFinal));
    }

    private static async Task<TacticalBattleSnapshot> Run(int seed, string tactic, JsonElement expected, bool explicitOrders)
    {
        var engine = new SimulationEngine(GameDataRegistry.Create([], [], [], [], tacticalBattle: Availability.Available(Catalog.Value)));
        engine.SetSpeed(SimulationSpeed.Speed0);
        await using var connection = new LocalGameSessionConnection(engine);
        var response = await connection.StartTacticalBattleAsync(new("start", seed, "initial", tactic));
        Assert.True(response.Accepted);
        var frame = response.State!;
        int command = 0;
        while (frame.Phase != TacticalBattlePhase.Over)
        {
            Assert.True(command <= expected.GetProperty("turns").GetInt32() * 2,
                $"seed={seed}, tactic={tactic}, explicit={explicitOrders}: exceeded independent turn count");
            long calls = engine.TacticalBattleSessionForTests!.Battle.Random.Calls;
            string before = JsonSerializer.Serialize(frame);
            Assert.Equal(before, JsonSerializer.Serialize(await connection.GetTacticalBattleStateAsync()));
            Assert.Equal(calls, engine.TacticalBattleSessionForTests.Battle.Random.Calls);
            response = await Next(connection, frame, command++, explicitOrders);
            Assert.True(response.Accepted, $"seed={seed}, tactic={tactic}, explicit={explicitOrders}: {response.Code}");
            frame = response.State!;
        }
        var outcome = frame.Outcome!;
        Assert.Equal(expected.GetProperty("outcome").GetString(), outcome.Kind.ToString().ToUpperInvariant());
        Assert.Equal(expected.GetProperty("turns").GetInt32(), outcome.Turns);
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "A" : "B";
            Assert.Equal(expected.GetProperty("losses")[i].GetInt32(), outcome.Losses[i].Count);
            Assert.Equal(side, outcome.Losses[i].Side);
            Assert.Equal(expected.GetProperty("carrierHull")[i].GetInt32(),
                frame.Units.First(u => u.Side == side && u.Kind == TacticalUnitKind.Carrier).Hull);
        }
        Assert.Equal(expected.GetProperty("shots").GetInt64(), outcome.Statistics.Shots);
        Assert.Equal(expected.GetProperty("hits").GetInt64(), outcome.Statistics.Hits);
        Assert.Equal(expected.GetProperty("rearHits").GetInt64(), outcome.Statistics.RearHits);
        Assert.Equal(expected.GetProperty("systems").GetInt64(), outcome.Statistics.Systems);
        Assert.Equal(expected.GetProperty("disabled").GetInt64(), outcome.Statistics.Disabled);
        Assert.Equal(expected.GetProperty("totalCalls").GetInt64(), outcome.RandomCalls);
        Assert.Equal(expected.GetProperty("turnCalls").GetInt64(), outcome.RandomCalls - expected.GetProperty("assemblyCalls").GetInt64());
        var reason = Assert.Single(outcome.Reasons);
        Assert.Equal(expected.GetProperty("reason").GetProperty("kind").GetString(), reason.Kind.ToString());
        string defeatedSide = expected.GetProperty("reason").GetProperty("side").GetString()!;
        Assert.Equal(frame.Units.First(u => u.Side == defeatedSide && u.Kind == TacticalUnitKind.Carrier).Id, reason.CarrierId);
        Assert.True((await connection.ExitTacticalBattleAsync(new("exit", frame.BattleId))).Accepted);
        Assert.Null(await connection.GetTacticalBattleStateAsync());
        return frame;
    }

    private static ValueTask<TacticalBattleResponse> Next(IGameSessionConnection connection, TacticalBattleSnapshot state, int sequence, bool explicitOrders) =>
        state.Phase == TacticalBattlePhase.Move
        ? connection.MoveTacticalBattleAsync(new("move-" + sequence, state.BattleId, state.Revision, explicitOrders ? state.Suggestions : []))
        : connection.FireTacticalBattleAsync(new("fire-" + sequence, state.BattleId, state.Revision,
            [.. state.Targets.Select(t => new TacticalFireTarget(t.DroneId, t.DefaultTargetId))]));

    [Theory]
    [InlineData(TacticalBattlePhase.Move)]
    [InlineData(TacticalBattlePhase.Fire)]
    [InlineData(TacticalBattlePhase.Over)]
    public async Task RealWorldConnectionRejectsSaveAtEveryActivePhaseAndRestoresItOnExit(TacticalBattlePhase phase)
    {
        string directory = Path.Combine(Path.GetTempPath(), "session-battle-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            var engine = RealEngine();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            string originalWorld = ScenarioLoader.Serialize(engine.CaptureSaveState());
            await connection.SaveAsync("quicksave");
            string slot = Path.Combine(directory, "quicksave.json");
            byte[] originalSlot = await File.ReadAllBytesAsync(slot);
            var state = (await connection.StartTacticalBattleAsync(new("start", 1))).State!;
            for (int sequence = 0; state.Phase != phase && sequence < 100; sequence++)
                state = (await Next(connection, state, sequence, false)).State!;
            Assert.Equal(phase, state.Phase);
            Assert.True(engine.CaptureSnapshot().IsTacticalBattleActive);
            Assert.Equal(SessionOutcomeState.Running, engine.CaptureSnapshot().EffectiveOutcome.State);
            Assert.Equal(SimulationEngine.TacticalBattleActiveReason,
                (await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync("quicksave").AsTask())).Message);
            Assert.Equal(originalSlot, await File.ReadAllBytesAsync(slot));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.True((await connection.ExitTacticalBattleAsync(new("exit", state.BattleId))).Accepted);
            Assert.False(engine.CaptureSnapshot().IsTacticalBattleActive);
            Assert.Equal(originalWorld, ScenarioLoader.Serialize(engine.CaptureSaveState()));
            await connection.SaveAsync("after");
            Assert.Equal(originalSlot, await File.ReadAllBytesAsync(Path.Combine(directory, "after.json")));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task SaveQueuedBeforeBattleChecksActiveStateWhenItAcquiresCaptureGate()
    {
        string directory = Path.Combine(Path.GetTempPath(), "queued-battle-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using var connection = new LocalGameSessionConnection(RealEngine(), directory);
            var gate = (SemaphoreSlim)typeof(LocalGameSessionConnection).GetField("_saveGate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(connection)!;
            await gate.WaitAsync();
            Task pending;
            try
            {
                pending = connection.SaveAsync("queued").AsTask();
                Assert.False(pending.IsCompleted);
                Assert.True((await connection.StartTacticalBattleAsync(new("start", 1))).Accepted);
            }
            finally { gate.Release(); }
            Assert.Equal(SimulationEngine.TacticalBattleActiveReason,
                (await Assert.ThrowsAsync<InvalidOperationException>(() => pending)).Message);
            Assert.False(Directory.Exists(directory));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static SimulationEngine RealEngine()
    {
        string root = Path.GetFullPath(Path.Combine(ReferenceFixtureTests.Root, "../../../.."));
        var engine = SimulationEngine.CreateFromSettingsFile(Path.Combine(root, "src/DeepSpaceSaga.Client/Settings.json"));
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }
    private static JsonElement[] ReadRows()
    {
        using var document = ReferenceFixtureTests.Open("battles");
        return document.RootElement.GetProperty("data").EnumerateArray().Select(row => row.Clone()).ToArray();
    }
}
