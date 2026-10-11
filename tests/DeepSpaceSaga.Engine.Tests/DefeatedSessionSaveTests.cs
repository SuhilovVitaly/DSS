using System.Reflection;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class DefeatedSessionSaveTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_after_defeat_keeps_existing_bytes(bool existingSlot)
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-defeat-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var engine = NewEngine();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            string path = Path.Combine(directory, "slot.json");
            if (existingSlot) { Directory.CreateDirectory(directory); await File.WriteAllTextAsync(path, "existing bytes"); }
            At(engine, 6000);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync("slot").AsTask());
            Assert.Equal(CommandReasonCodes.SessionDefeated, error.Message);
            if (existingSlot)
            {
                Assert.Equal("existing bytes", await File.ReadAllTextAsync(path));
                Assert.Single(Directory.GetFiles(directory));
            }
            else Assert.False(Directory.Exists(directory));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Queued_postdeath_capture_is_rejected()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-defeat-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var engine = NewEngine();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            await connection.SaveAsync("slot");
            byte[] before = await File.ReadAllBytesAsync(Path.Combine(directory, "slot.json"));
            var gate = (SemaphoreSlim)typeof(LocalGameSessionConnection)
                .GetField("_saveGate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(connection)!;
            await gate.WaitAsync();
            Task pending;
            try
            {
                pending = connection.SaveAsync("slot").AsTask();
                Assert.False(pending.IsCompleted);
                Assert.Equal(SessionOutcomeState.Defeated, At(engine, 6000).EffectiveOutcome.State);
            }
            finally { gate.Release(); }
            Assert.Equal(CommandReasonCodes.SessionDefeated,
                (await Assert.ThrowsAsync<InvalidOperationException>(() => pending)).Message);
            Assert.Equal(before, await File.ReadAllBytesAsync(Path.Combine(directory, "slot.json")));
            Assert.Single(Directory.GetFiles(directory));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Fresh_session_has_no_terminal_state()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-defeat-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var engine = NewEngine();
            await using (var connection = new LocalGameSessionConnection(engine, directory))
            {
                await connection.SaveAsync("before");
                Assert.Equal(SessionOutcomeState.Defeated, At(engine, 6000).EffectiveOutcome.State);
            }
            await using var fresh = LocalGameSessionConnection.CreateFromSaveFile(Path.Combine(ClientRoot, "Settings.json"),
                Path.Combine(directory, "before.json"), directory);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var snapshots = fresh.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
            Assert.True(await snapshots.MoveNextAsync());
            Assert.Equal(SessionOutcomeState.Running, snapshots.Current.EffectiveOutcome.State);
            Assert.True(snapshots.Current.EffectiveOutcome.CanSave);
            Assert.Contains(snapshots.Current.Objects, o => o.ObjectId == Player);
            await fresh.SaveAsync("restored");
            Assert.NotNull(ScenarioLoader.LoadFromFile(Path.Combine(directory, "restored.json"), true));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static SimulationEngine NewEngine()
    {
        var engine = new SimulationEngine(Registry.Value, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(PlayerDefeatTests.LethalScenario(150));
        return engine;
    }
}
