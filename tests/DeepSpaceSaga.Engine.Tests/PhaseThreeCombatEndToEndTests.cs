using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class PhaseThreeCombatEndToEndTests
{
    [Fact]
    public async Task Real_battle_exercises_auto_and_manual_defense_then_defeat_and_file_recovery()
    {
        await using var battle = new Battle(42);
        var initial = battle.At(0);
        Assert.NotNull(initial.SolarSystemMap); Assert.NotNull(initial.ClusterMap); Assert.NotNull(initial.AiMap);
        Assert.Equal(450, initial.Objects.Single(o => o.ObjectId == Battle.Player).HullCombat!.CurrentHp);
        Assert.Equal(2, initial.InstalledModules.Count(m => m.Defense is not null));
        await battle.Send(Battle.Defense2, DefenseCommandTypes.Disable);
        await battle.Connection.SendCommandAsync(new("brake", 100, Battle.Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.Brake));
        Assert.Equal(ShipEngineCommandTypes.Brake, battle.At(0).Objects.Single(o => o.ObjectId == Battle.Player).ActiveEngineCommandType);
        AuthoritativeSnapshot snapshot = initial;
        bool pursuit = false, manual = false;
        long checkpoint = 0;
        for (long time = 1000; time <= 1500000; time += 1000)
        {
            snapshot = battle.At(time);
            pursuit |= snapshot.Objects.Any(o => o.HostileCombat?.Phase == HostileCombatPhase.Pursuit);
            Assert.Equal(SessionOutcomeState.Running, snapshot.EffectiveOutcome.State);
            var auto = snapshot.Objects.FirstOrDefault(o => o.Countermeasure is { OwnerObjectId: Battle.Player, Mode: LaunchMode.Auto });
            if (auto?.Countermeasure is { } flight)
            {
                var result = await battle.Send(Battle.Defense2, DefenseCommandTypes.Fire, flight.TargetTorpedoId);
                Assert.Equal(CommandResultStatus.Executed, result.Status);
                snapshot = battle.At(time);
                Assert.Equal(2, snapshot.Objects.Count(o => o.Countermeasure?.OwnerObjectId == Battle.Player));
                checkpoint = time; manual = true; break;
            }
        }
        Assert.True(pursuit);
        Assert.True(manual, $"No manual opportunity; motion={snapshot.MotionTimeMs}, journal={JsonSerializer.Serialize(snapshot.CombatJournal)}");
        Assert.Contains(snapshot.CombatJournal, e => e.Type == CombatEventType.Launch && e.ActorObjectId == Battle.Enemy && e.Result == "torpedo");
        Assert.Contains(snapshot.CombatJournal, e => e.LaunchMode == LaunchMode.Auto);
        Assert.Contains(snapshot.CombatJournal, e => e.LaunchMode == LaunchMode.Manual);
        Assert.All(snapshot.CommandResults, r => Assert.Equal(Battle.Player, r.ObjectId));

        await battle.Connection.SaveAsync("before-defeat");
        string path = Path.Combine(battle.DirectoryPath, "before-defeat.json");
        byte[] bytes = await File.ReadAllBytesAsync(path);
        using (var resumed = EngineContentLoader.CreateEngineFromSaveFile(Battle.Settings, path))
        {
            for (long time = checkpoint; time <= checkpoint + 10000; time += 1000)
            {
                snapshot = battle.At(time);
                var restored = resumed.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed0, time);
                Assert.Equal(JsonSerializer.Serialize(snapshot.Objects), JsonSerializer.Serialize(restored.Objects));
                Assert.Equal(JsonSerializer.Serialize(snapshot.CombatJournal), JsonSerializer.Serialize(restored.CombatJournal));
                Assert.Equal(JsonSerializer.Serialize(battle.Engine.CaptureSaveState().GameState.DefenseState),
                    JsonSerializer.Serialize(resumed.CaptureSaveStateForTests(time * 300, SimulationSpeed.Speed0, time).GameState.DefenseState));
            }
        }
        await battle.Send(Battle.Defense1, DefenseCommandTypes.Disable);
        await battle.Send(Battle.Defense2, DefenseCommandTypes.Disable);
        for (long time = checkpoint + 11000; time <= checkpoint + 600000 && snapshot.EffectiveOutcome.State == SessionOutcomeState.Running; time += 1000)
            snapshot = battle.At(time);
        Assert.Equal(SessionOutcomeState.Defeated, snapshot.EffectiveOutcome.State);
        Assert.Equal(3, snapshot.CombatJournal.Count(e => e.Type == CombatEventType.Hit && e.TargetObjectId == Battle.Player));
        Assert.Contains(snapshot.CombatJournal, e => e.Roll is not null);
        string terminal = JsonSerializer.Serialize(snapshot.Objects);
        var later = battle.At(checkpoint + 1200000);
        Assert.Equal(snapshot.Outcome, later.Outcome);
        Assert.Equal(snapshot.MotionTimeMs, later.MotionTimeMs);
        Assert.Equal(terminal, JsonSerializer.Serialize(later.Objects));
        await Assert.ThrowsAsync<InvalidOperationException>(() => battle.Connection.SaveAsync("before-defeat").AsTask());
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        await using var recovered = LocalGameSessionConnection.CreateFromSaveFile(Battle.Settings, path, battle.DirectoryPath);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var reader = recovered.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        Assert.Equal(SessionOutcomeState.Running, reader.Current.EffectiveOutcome.State);
        Assert.Equal(2, reader.Current.Objects.Count(o => o.Countermeasure?.OwnerObjectId == Battle.Player));
    }

    internal sealed class Battle : IAsyncDisposable
    {
        internal const string Player = "SPC-0001", Enemy = "SPC-0002", Defense1 = "MOD-PLAYER-COUNTERMEASURE-01", Defense2 = "MOD-PLAYER-COUNTERMEASURE-02";
        internal static string Settings => Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json");
        internal SimulationEngine Engine { get; }
        internal LocalGameSessionConnection Connection { get; }
        internal string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "ep8-full-battle-" + Guid.NewGuid().ToString("N"));
        private long _now, _time, _command;
        internal Battle(ulong seed)
        {
            var registry = SeededWorldBootstrapTests.Registry();
            Engine = new(registry, EngineContentLoader.LoadCrewPortraits(Settings), new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref _now)));
            Engine.ConfigureStationResourceFields(JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!);
            var source = SeededWorldBootstrapTests.Scenario("PlayerShipOnly");
            Engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } }, generation: EngineContentLoader.LoadSolarSystemGenerationConfig(Settings));
            Connection = new(Engine, DirectoryPath);
        }
        internal AuthoritativeSnapshot At(long time, SimulationSpeed speed = SimulationSpeed.Speed1)
        {
            Assert.Equal(0, (time - _time) % (int)speed);
            Engine.SetSpeed(speed);
            Interlocked.Add(ref _now, (time - _time) / (int)speed); _time = time;
            Engine.SetSpeed(SimulationSpeed.Speed0);
            return Engine.CaptureSnapshot();
        }
        internal AuthoritativeSnapshot Pause(long realMilliseconds)
        {
            Engine.SetSpeed(SimulationSpeed.Speed0);
            Interlocked.Add(ref _now, realMilliseconds);
            return Engine.CaptureSnapshot();
        }
        internal async Task<CommandResult> Send(string module, string type, string? target = null)
        {
            string id = "battle-" + (++_command);
            await Connection.SendCommandAsync(new(id, (ulong)_command, Player, module, type, target));
            // The live session loop may consume the receipt first; read the durable receipt through replay.
            await Connection.SendCommandAsync(new(id, (ulong)_command, Player, module, type, target));
            var snapshot = Engine.CaptureSnapshot();
            var result = snapshot.CommandResults.FirstOrDefault(r => r.CommandId == id);
            if (result is not null) return result;
            await Connection.SendCommandAsync(new(id, (ulong)_command, Player, module, type, target));
            return Engine.CaptureSnapshot().CommandResults.Single(r => r.CommandId == id);
        }
        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
        }
    }
}
