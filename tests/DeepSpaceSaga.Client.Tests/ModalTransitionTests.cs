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

        public ValueTask SaveAsync(string slotId, CancellationToken ct = default) => ValueTask.CompletedTask;

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

    /// <summary>
    /// Same nested-modal shape as <see cref="Nested_modal_preserves_saved_speed_through_delayed_connection"/>,
    /// framed explicitly around the Save window's own pause/resume lifecycle: GameMenu is
    /// opened first (depth 0->1, pause), then the Save window is opened on top of it
    /// (depth 1->2, nested — no additional SetSpeedAsync call), and closing back down
    /// through Save window (depth 2->1) then GameMenu (depth 1->0) restores the speed
    /// that was authoritative before GameMenu was ever opened.
    /// </summary>
    [Fact]
    public async Task Save_window_opened_from_GameMenu_resumes_original_speed_through_delayed_connection()
    {
        var conn = new ControllableConnection();
        var handle = new GameSessionHandle(conn);

        while (handle.Buffer.Latest is null)
            await Task.Delay(10);

        // Set to Speed2 (the speed in effect before GameMenu is opened).
        var startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var speed2Task = handle.SetSpeedAsync(SimulationSpeed.Speed2);
        await startedTcs.Task;
        conn.CompletePending();
        await speed2Task;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed2);

        // Open GameMenu (depth 0 -> 1): save speed, pause to Speed0.
        var savedSpeed = handle.Buffer.CurrentSpeed;
        Assert.True(savedSpeed == SimulationSpeed.Speed2);

        startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var pauseTask = handle.SetSpeedAsync(SimulationSpeed.Speed0);
        await startedTcs.Task;
        conn.CompletePending();
        await pauseTask;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed0);

        // Open Save window on top of GameMenu (depth 1 -> 2): nested — no SetSpeedAsync
        // call at all, simulation stays paused exactly as PushModalAsync's modalDepth>0 branch does.
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed0);

        // Close Save window (depth 2 -> 1): still nested — no SetSpeedAsync call, stays paused.
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed0);

        // Close GameMenu (depth 1 -> 0): restore the speed saved before GameMenu opened.
        startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var restoreTask = handle.SetSpeedAsync(savedSpeed);
        await startedTcs.Task;
        conn.CompletePending();
        await restoreTask;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed2);
    }

    /// <summary>
    /// Same nested-modal shape as <see cref="Save_window_opened_from_GameMenu_resumes_original_speed_through_delayed_connection"/>,
    /// framed around the Load window's own pause/resume lifecycle: GameMenu is opened
    /// first (depth 0->1, pause), then the Load window is opened on top of it (depth
    /// 1->2, nested — no additional SetSpeedAsync call), and closing back down through
    /// the Load window (depth 2->1) then GameMenu (depth 1->0) restores the speed that
    /// was authoritative before GameMenu was ever opened. SkiaWindow.OpenLoadWindowAsync
    /// uses the same PushModalAsync/PopModalAsync pair as Save, so this is identical
    /// except for which screen type is pushed.
    /// </summary>
    [Fact]
    public async Task Load_window_opened_from_GameMenu_resumes_original_speed_through_delayed_connection()
    {
        var conn = new ControllableConnection();
        var handle = new GameSessionHandle(conn);

        while (handle.Buffer.Latest is null)
            await Task.Delay(10);

        // Set to Speed2 (the speed in effect before GameMenu is opened).
        var startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var speed2Task = handle.SetSpeedAsync(SimulationSpeed.Speed2);
        await startedTcs.Task;
        conn.CompletePending();
        await speed2Task;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed2);

        // Open GameMenu (depth 0 -> 1): save speed, pause to Speed0.
        var savedSpeed = handle.Buffer.CurrentSpeed;
        Assert.True(savedSpeed == SimulationSpeed.Speed2);

        startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var pauseTask = handle.SetSpeedAsync(SimulationSpeed.Speed0);
        await startedTcs.Task;
        conn.CompletePending();
        await pauseTask;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed0);

        // Open Load window on top of GameMenu (depth 1 -> 2): nested — no SetSpeedAsync
        // call at all, simulation stays paused exactly as PushModalAsync's modalDepth>0 branch does.
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed0);

        // Close Load window (depth 2 -> 1): still nested — no SetSpeedAsync call, stays paused.
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed0);

        // Close GameMenu (depth 1 -> 0): restore the speed saved before GameMenu opened.
        startedTcs = new TaskCompletionSource();
        conn.PendingSetSpeedTcs = startedTcs;
        var restoreTask = handle.SetSpeedAsync(savedSpeed);
        await startedTcs.Task;
        conn.CompletePending();
        await restoreTask;
        Assert.True(handle.Buffer.CurrentSpeed == SimulationSpeed.Speed2);
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
