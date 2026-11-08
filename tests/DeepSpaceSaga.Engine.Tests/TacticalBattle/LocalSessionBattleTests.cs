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
    public async Task NewRuleBattlesRepeatAcrossDirectAndConnectionPaths(int seed, string tactic, int index)
    {
        Assert.Equal(seed, Rows[index].GetProperty("seed").GetInt32()); // corpus identity only
        var implicitFinal = await Run(seed, tactic, explicitOrders: false);
        var explicitFinal = await Run(seed, tactic, explicitOrders: true);
        Assert.Equal(JsonSerializer.Serialize(implicitFinal), JsonSerializer.Serialize(explicitFinal));
    }

    private static async Task<TacticalBattleSnapshot> Run(int seed, string tactic, bool explicitOrders)
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
            Assert.True(command <= Catalog.Value.Rules.SeriesTurnCap * 2,
                $"seed={seed}, tactic={tactic}, explicit={explicitOrders}: exceeded new-rule safety turn cap");
            long calls = engine.TacticalBattleSessionForTests!.Battle.Random.Calls;
            string before = JsonSerializer.Serialize(frame);
            Assert.Equal(before, JsonSerializer.Serialize(await connection.GetTacticalBattleStateAsync()));
            Assert.Equal(calls, engine.TacticalBattleSessionForTests.Battle.Random.Calls);
            response = await Next(connection, frame, command++, explicitOrders);
            Assert.True(response.Accepted, $"seed={seed}, tactic={tactic}, explicit={explicitOrders}: {response.Code}");
            frame = response.State!;
        }
        var direct = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle.Create(Catalog.Value, "initial", seed,
            new Dictionary<string, string> { ["A"] = tactic });
        while (direct.Result is null && direct.Turn < Catalog.Value.Rules.SeriesTurnCap)
        {
            direct.Planning(DeepSpaceSaga.Engine.TacticalBattle.Ai.TacticalBattleAi.Plan);
            direct.Movement();
            if (direct.Result is not null) break;
            bool playerPhase = direct.ActiveDrones("A").Any(d => !DeepSpaceSaga.Engine.TacticalBattle.TacticalBattleSalvo.Candidates(direct, d).IsEmpty)
                || direct.ActiveDrones("B").Any(d => DeepSpaceSaga.Engine.TacticalBattle.TacticalBattleSalvo.Candidates(direct, d)
                    .Any(c => c.Target is DeepSpaceSaga.Engine.TacticalBattle.TacticalDrone));
            direct.Firing((d, candidates) => d.Side == "A" && !playerPhase ? new(null, false)
                : DeepSpaceSaga.Engine.TacticalBattle.TacticalBattleSalvo.DefaultPlan(direct, d, candidates), d => d.Side != "A");
        }
        direct.Run(DeepSpaceSaga.Engine.TacticalBattle.Ai.TacticalBattleAi.Plan);
        Assert.Equal(JsonSerializer.Serialize(DeepSpaceSaga.Engine.TacticalBattle.TacticalBattleSnapshotBuilder.Outcome(direct.Result)),
            JsonSerializer.Serialize(frame.Outcome));
        Assert.Equal(JsonSerializer.Serialize(DeepSpaceSaga.Engine.TacticalBattle.TacticalBattleSnapshotBuilder.Units(direct)),
            JsonSerializer.Serialize(frame.Units));
        Assert.Equal(frame.FireQueue.Length, frame.FireSteps.Length);
        Assert.Equal(frame.Log.Select(e => e.Sequence).Distinct().Count(), frame.Log.Length);
        Assert.True((await connection.ExitTacticalBattleAsync(new("exit", frame.BattleId))).Accepted);
        Assert.Null(await connection.GetTacticalBattleStateAsync());
        return frame;
    }

    private static ValueTask<TacticalBattleResponse> Next(IGameSessionConnection connection, TacticalBattleSnapshot state, int sequence, bool explicitOrders) =>
        state.Phase == TacticalBattlePhase.Move
        ? connection.MoveTacticalBattleAsync(new("move-" + sequence, state.BattleId, state.Revision, explicitOrders ? state.Suggestions : []))
        : connection.FireTacticalBattleAsync(new("fire-" + sequence, state.BattleId, state.Revision,
            [.. state.Targets.Select(t =>
            {
                var unit = state.Units.Single(u => u.Id == t.DroneId);
                bool evade = unit.Hull < Catalog.Value.Rules.Evasion.AiHullFraction * unit.MaxHull
                    && unit.Consciousness >= Catalog.Value.Rules.Evasion.MinConsciousness;
                return new TacticalFireTarget(t.DroneId, evade ? null : t.DefaultTargetId, evade);
            })]));

    [Theory]
    [InlineData(TacticalBattlePhase.Move)]
    [InlineData(TacticalBattlePhase.Fire)]
    [InlineData(TacticalBattlePhase.Over)]
    public async Task RealWorldConnectionRejectsSaveAtEveryActivePhaseAndRestoresItOnExit(TacticalBattlePhase phase)
    {
        string directory = ValidationDirectory("session-battle-save");
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
            var world = engine.CaptureSnapshot();
            Assert.False(Assert.Single(world.Objects, o => o.ObjectId == world.PlayerShipObjectId).IsDestroyed);
            Assert.Equal(SimulationEngine.TacticalBattleActiveReason,
                (await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync("quicksave").AsTask())).Message);
            Assert.Equal(originalSlot, await File.ReadAllBytesAsync(slot));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.True((await connection.ExitTacticalBattleAsync(new("exit", state.BattleId))).Accepted);
            Assert.False(engine.CaptureSnapshot().IsTacticalBattleActive);
            Assert.Equal(originalWorld, ScenarioLoader.Serialize(engine.CaptureSaveState()));
            await connection.SaveAsync("after");
            Assert.Equal(originalSlot, await File.ReadAllBytesAsync(Path.Combine(directory, "after.json")));
            string root = Path.GetFullPath(Path.Combine(ReferenceFixtureTests.Root, "../../../.."));
            using var loaded = EngineContentLoader.CreateEngineFromSaveFile(Path.Combine(root, "src/DeepSpaceSaga.Client/Settings.json"),
                Path.Combine(directory, "after.json"));
            Assert.Equal(originalWorld, ScenarioLoader.Serialize(loaded.CaptureSaveState()));
            Assert.False(loaded.IsTacticalBattleActive);
        }
        finally { DeleteValidationDirectory(directory); }
    }

    [Fact]
    public async Task SaveQueuedBeforeBattleChecksActiveStateWhenItAcquiresCaptureGate()
    {
        string directory = ValidationDirectory("queued-battle-save");
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
        finally { DeleteValidationDirectory(directory); }
    }

    private static string ValidationRoot => Path.GetFullPath(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")
        ?? "D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/local-session");
    private static string ValidationDirectory(string name)
    {
        Directory.CreateDirectory(ValidationRoot);
        return Path.Combine(ValidationRoot, name + "-" + Guid.NewGuid().ToString("N"));
    }
    private static void DeleteValidationDirectory(string directory)
    {
        if (!Path.GetFullPath(directory).StartsWith(ValidationRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Cleanup escaped validation root.");
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
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
