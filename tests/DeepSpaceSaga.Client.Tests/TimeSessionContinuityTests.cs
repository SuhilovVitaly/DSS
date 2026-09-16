using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

public class TimeSessionContinuityTests
{
    [Fact]
    public void Old_transport_snapshot_cannot_undo_an_hourly_command_reply()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new(2, GameCalendar.HourMs, SimulationSpeed.Speed0, [], CurrentStationDistrict: StationDistrict.Market));
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, []));
        Assert.Equal(GameCalendar.HourMs, buffer.Latest!.Snapshot.GameTimeMs);
        Assert.Equal(StationDistrict.Market, buffer.Latest.Snapshot.CurrentStationDistrict);
    }

    [Fact]
    public async Task Pending_travel_blocks_double_send_without_changing_time_locally()
    {
        var connection = new DelayedTravelConnection();
        await using var session = new GameSessionHandle(connection);
        var travel = session.TravelStationAsync(StationDistrict.Market).AsTask();
        await session.TravelStationAsync(StationDistrict.Market);
        Assert.Equal(1, connection.CallCount);
        Assert.True(session.StationTravelPending);
        Assert.True(session.Buffer.Latest is null || session.Buffer.Latest.Snapshot.GameTimeMs == 0);
        connection.Reply.SetResult(new(true, null, new(2, GameCalendar.HourMs, SimulationSpeed.Speed0, [],
            CurrentStationDistrict: StationDistrict.Market)));
        await travel;
        Assert.False(session.StationTravelPending);
        Assert.True(session.KeepPausedAfterStationTravel);
        Assert.Equal(GameCalendar.HourMs, session.Buffer.Latest!.Snapshot.GameTimeMs);
    }

    [Fact]
    public void Slot_metadata_reads_game_time_and_tolerates_corrupt_saves()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-game-time-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "valid.json"), "{\"gameState\":{\"gameTimeMs\":203400000}}");
            File.WriteAllText(Path.Combine(dir, "broken.json"), "[]");
            var slots = SaveSlotRepository.ListSlots(dir);
            Assert.Equal(203400000, slots.Single(s => s.SlotId == "valid").GameTimeMs);
            Assert.Null(slots.Single(s => s.SlotId == "broken").GameTimeMs);
        }
        finally { Directory.Delete(dir, true); }
    }

    private sealed class DelayedTravelConnection : IGameSessionConnection
    {
        public int CallCount;
        public TaskCompletionSource<StationTravelResult> Reply { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask<StationTravelResult> TravelStationAsync(StationTravelCommand command, CancellationToken cancellationToken = default)
        { CallCount++; return new(Reply.Task); }
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new(1, 0, SimulationSpeed.Speed0, ImmutableArray<ObjectMotionSnapshot>.Empty);
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}
