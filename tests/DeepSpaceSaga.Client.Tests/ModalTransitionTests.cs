using System.Collections.Immutable;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Tests the modal pause/resume lifecycle with a controllable connection.
/// Verifies that Buffer.CurrentSpeed is not updated until SetSpeedAsync
/// actually completes (critical for correct modal ordering).
/// </summary>
public class ModalTransitionTests
{
    /// <summary>
    /// A fake connection that introduces a controllable delay in SetSimulationSpeedAsync,
    /// simulating network latency. Each SetSpeedAsync call gets its own gate,
    /// so multiple sequential calls can be individually blocked and released.
    /// </summary>
    private sealed class ControllableConnection : IGameSessionConnection
    {
        private TaskCompletionSource? _currentGate;

        public SimulationSpeed LastSpeed { get; private set; } = SimulationSpeed.Speed1;

        /// <summary>Set before calling SetSpeedAsync to observe call-start.</summary>
        public TaskCompletionSource? PendingSetSpeedTcs { get; set; }

        /// <summary>Complete the current SetSpeedAsync call (and prepare for the next).</summary>
        public void CompletePending()
        {
            var gate = _currentGate;
            _currentGate = null;
            gate?.TrySetResult();
        }

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken ct = default)
            => ValueTask.CompletedTask;

        public async ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken ct = default)
        {
            // Signal that a SetSpeedAsync call has started
            var tcs = PendingSetSpeedTcs;
            PendingSetSpeedTcs = null;
            tcs?.TrySetResult();

            // Create a fresh gate for this call
            var gate = new TaskCompletionSource();
            _currentGate = gate;

            // Wait for the gate to be opened before completing
            await gate.Task;

            LastSpeed = speed;
        }

        public ValueTask SetObjectInteractionStateAsync(
            string? activeObjectId, string? selectedObjectId, CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new AuthoritativeSnapshot(0, 0, SimulationSpeed.Speed1,
                ImmutableArray<ObjectMotionSnapshot>.Empty);
        }

        public ValueTask SaveAsync(CancellationToken ct = default) => ValueTask.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Buffer_speed_not_updated_until_SetSpeedAsync_completes()
    {
        var conn = new ControllableConnection();
        var handle = new GameSessionHandle(conn);

        // Wait for initial snapshot
        while (handle.Buffer.Latest is null)
            await Task.Delay(10);

        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed1);

        // Start pause — it will block on _delayGate
        var startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var pauseTask = handle.SetSpeedAsync(SimulationSpeed.Speed0);

        // Wait until SetSpeedAsync is called but not yet completed
        await startedTcs.Task;

        // In-flight: buffer must still be Speed1
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed1,
            "Buffer.CurrentSpeed must not change until SetSpeedAsync completes");

        // Let it complete
        conn.CompletePending();
        await pauseTask;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed0);
    }

    [Fact]
    public async Task Nested_modal_preserves_saved_speed_through_delayed_connection()
    {
        var conn = new ControllableConnection();
        var handle = new GameSessionHandle(conn);

        while (handle.Buffer.Latest is null)
            await Task.Delay(10);

        // Set to Speed2
        var startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var speed2Task = handle.SetSpeedAsync(SimulationSpeed.Speed2);
        await startedTcs.Task;
        conn.CompletePending();
        await speed2Task;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed2);

        // Save speed (simulating PushModalAsync)
        var savedSpeed = handle.Buffer.CurrentSpeed;
        Assert.True(savedSpeed == SimulationSpeed.Speed2);

        // Pause to Speed0
        startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var pauseTask = handle.SetSpeedAsync(SimulationSpeed.Speed0);
        await startedTcs.Task;
        conn.CompletePending();
        await pauseTask;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed0);

        // Restore saved speed (simulating PopModalAsync)
        startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var restoreTask = handle.SetSpeedAsync(savedSpeed);
        await startedTcs.Task;
        conn.CompletePending();
        await restoreTask;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed2);
    }
}
