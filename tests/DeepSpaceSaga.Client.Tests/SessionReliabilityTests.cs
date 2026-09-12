using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

public class SessionReliabilityTests
{
    private static AuthoritativeSnapshot Snapshot(long tick) => new((ulong)tick, tick, SimulationSpeed.Speed1, []);
    private static CommandResult Result(string id, CommandResultStatus status = CommandResultStatus.Rejected) =>
        new(id, "ship", "module", "buy", status, 0);

    [Fact]
    public async Task Slow_reader_gets_latest_world_and_all_distinct_events()
    {
        var mailbox = new SnapshotMailbox();
        mailbox.Publish(Snapshot(1) with { CommandResults = [Result("first")],
            ShipEvents = [new("event", "ship", "module", "completed", null, 1)] });
        for (int i = 2; i <= 500; i++) mailbox.Publish(Snapshot(i));
        mailbox.Publish(Snapshot(501) with { CommandResults = [Result("second")] });
        mailbox.Complete();
        var received = new List<AuthoritativeSnapshot>();
        await foreach (var snapshot in mailbox.ReadAllAsync(default)) received.Add(snapshot);
        var latest = Assert.Single(received);
        Assert.Equal(501, latest.GameTimeMs);
        Assert.Equal(2, latest.CommandResults.Length);
        Assert.Single(latest.ShipEvents);
    }

    [Fact]
    public async Task Mailbox_propagates_original_error_after_pending_snapshot()
    {
        var mailbox = new SnapshotMailbox();
        var error = new InvalidOperationException("simulation failed");
        mailbox.Publish(Snapshot(1));
        mailbox.Complete(error);
        await using var reader = mailbox.ReadAllAsync(default).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => reader.MoveNextAsync().AsTask()));
    }

    [Fact]
    public void Mailbox_refuses_event_overflow_explicitly()
    {
        var mailbox = new SnapshotMailbox();
        mailbox.Publish(Snapshot(1) with { CommandResults = Enumerable.Range(0, SnapshotMailbox.EventLimit)
            .Select(i => Result(i.ToString())).ToImmutableArray() });
        Assert.Throws<InvalidOperationException>(() => mailbox.Publish(Snapshot(2) with { CommandResults = [Result("extra")] }));
    }

    [Fact]
    public void Frame_skipping_preserves_command_outcome_and_ship_event()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(Snapshot(1) with { CommandResults = [Result("trade", CommandResultStatus.Deferred)],
            ShipEvents = [new("event", "ship", "module", "completed", null, 1)] });
        buffer.Update(Snapshot(2) with { CommandResults = [Result("trade")] });
        buffer.Update(Snapshot(3));
        Assert.Equal(CommandResultStatus.Rejected, buffer.FindCommandResult("trade")!.Status);
        Assert.Single(buffer.ReadRecentShipEvents());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Unexpected_stream_end_or_fault_pauses_session(bool fault)
    {
        await using var session = new GameSessionHandle(new Connection { Fault = fault });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (session.Failure is null) await Task.Delay(1, timeout.Token);
        Assert.IsType<IOException>(session.Failure);
        Assert.Equal(SimulationSpeed.Speed0, session.Buffer.CurrentSpeed);
    }

    [Fact]
    public async Task Closing_during_background_load_disposes_unadopted_connection()
    {
        var loader = new SessionConnectionLoader();
        var connection = new Connection();
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var load = loader.CreateAsync(() => { started.SetResult(); release.Wait(); return connection; });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try { await loader.DisposeAsync(); }
        finally { release.Set(); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => load);
        Assert.Equal(1, connection.DisposeCount);
        Assert.False(loader.TryAdopt(connection));
    }

    [Fact]
    public void Async_window_continuation_runs_on_owning_thread()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var context = new WindowThreadContext();
                SynchronizationContext.SetSynchronizationContext(context);
                int owner = Environment.CurrentManagedThreadId;
                async Task Work()
                {
                    await Task.Run(() => Thread.SpinWait(1000));
                    Assert.Equal(owner, Environment.CurrentManagedThreadId);
                }
                var task = Work();
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (!task.IsCompleted && DateTime.UtcNow < deadline) { context.Drain(); Thread.Yield(); }
                Assert.True(task.IsCompleted);
                task.GetAwaiter().GetResult();
            }
            catch (Exception error) { failure = error; }
        });
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        Assert.Null(failure);
    }

    private sealed class Connection : IGameSessionConnection
    {
        public bool Fault { get; init; }
        public int DisposeCount { get; private set; }
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return Snapshot(1);
            if (Fault) throw new IOException("broken stream");
        }
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }
}
