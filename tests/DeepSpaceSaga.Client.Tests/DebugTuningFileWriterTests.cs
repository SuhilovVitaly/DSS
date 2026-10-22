using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DebugTuningFileWriterTests : IDisposable
{
    private readonly string _directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep0013/writer-fixtures", Guid.NewGuid().ToString("N"));
    private static string ClientRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
    private static SimulationEngine Engine() => SimulationEngine.CreateFromScenarioFile(Path.Combine(ClientRoot, "Settings.json"),
        Path.Combine(ClientRoot, "Scenarios/PlayerShipOnly/scenario.json"));
    private static DebugTuningSnapshot Catalog()
    {
        using var engine = Engine(); return engine.CaptureSnapshotForTests().DebugTuning!;
    }
    private static DebugTuningSnapshot Change(DebugTuningSnapshot original, int revision) => original with
    {
        Revision = revision,
        Values = original.Values.SetItem(0, original.Values[0] with { Value = 5 + revision * .1 })
    };
    public DebugTuningFileWriterTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task Confirmed_revision_writes_all_twenty_five_values_utf8_schema_and_timestamp()
    {
        var catalog = Catalog();
        await using var writer = new DebugTuningFileWriter(_directory);
        writer.Observe(catalog);
        await writer.FlushAsync();
        Assert.False(File.Exists(writer.CurrentPath));
        writer.Observe(Change(catalog, 1));
        await writer.FlushAsync();
        byte[] bytes = File.ReadAllBytes(writer.CurrentPath);
        Assert.False(bytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 }));
        string json = File.ReadAllText(writer.CurrentPath);
        Assert.Contains("\n  \"schemaVersion\"", json);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.True(document.RootElement.GetProperty("writtenAtUtc").TryGetDateTime(out _));
        Assert.True(DebugTuningFileFormat.TryParse(json, catalog.Defaults, out var patch, out var reason), reason);
        Assert.Equal(25, patch.Values.Length);
        Assert.Equal(Change(catalog, 1).Values.ToArray(), patch.Values.ToArray());
        Assert.Equal(1, writer.Status.Revision); Assert.Null(writer.Status.Error);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp-*"));
    }

    [Fact]
    public async Task Twenty_updates_in_one_second_coalesce_to_at_most_five_writes_and_disposal_flushes_the_final_revision()
    {
        var catalog = Catalog();
        var times = new List<long>();
        var writer = new DebugTuningFileWriter(_directory, commit: (temporary, path) =>
        {
            File.Move(temporary, path, true); times.Add(Stopwatch.GetTimestamp());
        });
        var schedule = Stopwatch.StartNew();
        for (int i = 1; i <= 20; i++)
        {
            double remaining = (i - 1) * 50 - schedule.Elapsed.TotalMilliseconds;
            if (remaining > 0) await Task.Delay(TimeSpan.FromMilliseconds(remaining));
            writer.Observe(Change(catalog, i));
        }
        await writer.DisposeAsync();
        Assert.InRange(times.Count, 1, 5);
        for (int i = 1; i < times.Count; i++)
            Assert.True(Stopwatch.GetElapsedTime(times[i - 1], times[i]).TotalMilliseconds >= 250);
        Assert.Equal(20, writer.Status.Revision);
        Assert.True(DebugTuningFileFormat.TryParse(File.ReadAllText(writer.CurrentPath), catalog.Defaults, out var patch, out _));
        Assert.Equal(7, patch.Values[0].Value);
    }

    [Fact]
    public async Task Failed_replacement_retains_valid_previous_file_and_a_final_flush_can_retry()
    {
        var catalog = Catalog(); bool fail = true;
        string path = Path.Combine(_directory, "debug-tuning.json");
        string original = DebugTuningFileFormat.Serialize(catalog.Values, DateTimeOffset.UtcNow);
        File.WriteAllText(path, original);
        await using var writer = new DebugTuningFileWriter(_directory, commit: (temporary, destination) =>
        {
            if (fail) throw new IOException("Injected replacement failure");
            File.Move(temporary, destination, true);
        });
        writer.Observe(Change(catalog, 1)); await writer.FlushAsync();
        Assert.Equal(original, File.ReadAllText(path)); Assert.NotNull(writer.Status.Error);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp-*"));
        fail = false; await writer.FlushAsync();
        Assert.Equal(1, writer.Status.Revision); Assert.Null(writer.Status.Error);
    }

    [Fact]
    public async Task Deferred_candidate_does_not_write_and_old_session_cannot_overwrite_its_replacement()
    {
        var catalog = Catalog();
        var old = new DebugTuningFileWriter(_directory);
        old.Observe(Change(catalog, 1)); await old.DeactivateAsync();
        await using var next = new DebugTuningFileWriter(_directory, active: false);
        next.Observe(Change(catalog, 2)); await next.FlushAsync();
        Assert.Equal(5.1, JsonDocument.Parse(File.ReadAllText(old.CurrentPath)).RootElement.GetProperty("ships")
            .GetProperty("module.engine.tetrarch").GetProperty("maxSpeedKmS").GetDouble(), 8);
        old.Observe(Change(catalog, 3));
        await old.DisposeAsync();
        next.Activate(); await next.FlushAsync();
        Assert.Equal(2, next.Status.Revision);
        Assert.True(DebugTuningFileFormat.TryParse(File.ReadAllText(next.CurrentPath), catalog.Defaults, out var patch, out _));
        Assert.Equal(5.2, patch.Values[0].Value, 8);
    }

    [Fact]
    public async Task Game_session_receive_path_persists_the_engine_confirmed_revision()
    {
        var engine = Engine();
        var writer = new DebugTuningFileWriter(_directory);
        await using var session = new GameSessionHandle(new LocalGameSessionConnection(engine), new SnapshotBuffer(),
            new CombatMetricsWriter(Path.Combine(_directory, "metrics")), writer);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (session.Buffer.Latest?.Snapshot.DebugTuning is null) await Task.Delay(10, timeout.Token);
        await session.Connection.SendCommandAsync(new("confirmed", 1, "", "", DebugTuningCommandTypes.Apply,
            DebugTuning: new([new("module.engine.tetrarch", "maxSpeedKmS", 5.5)])), timeout.Token);
        while (session.Buffer.Latest!.Snapshot.DebugTuning!.Revision < 1) await Task.Delay(10, timeout.Token);
        await session.FlushDebugTuningAsync();
        Assert.Equal(1, writer.Status.Revision);
        var catalog = session.Buffer.Latest.Snapshot.DebugTuning!;
        Assert.True(DebugTuningFileFormat.TryParse(File.ReadAllText(writer.CurrentPath), catalog.Defaults, out var patch, out _));
        Assert.Equal(25, patch.Values.Length);
        Assert.Equal(5.5, patch.Values[0].Value);
        await Task.WhenAll(session.DisposeAsync().AsTask(), session.DisposeAsync().AsTask());
        Assert.Equal(1, writer.Status.Revision);
    }

    [Fact]
    public async Task Reset_requires_matching_success_receipt_and_does_not_delete_a_later_edit()
    {
        var catalog = Catalog();
        await using var writer = new DebugTuningFileWriter(_directory);
        var changed = Change(catalog, 1); writer.Observe(changed); await writer.FlushAsync();
        string before = File.ReadAllText(writer.CurrentPath);
        writer.RegisterReset("rejected", 1);
        writer.Observe(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], DebugTuning: changed,
            CommandResults: [new("rejected", "", "", DebugTuningCommandTypes.Apply, CommandResultStatus.Rejected, 0, DebugTuningReasonCodes.ValueOutOfRange)]));
        await writer.FlushAsync(); Assert.Equal(before, File.ReadAllText(writer.CurrentPath));
        writer.RegisterReset("reset", 1);
        var defaults = catalog with { Revision = 2 };
        writer.Observe(defaults); await writer.FlushAsync(); Assert.True(File.Exists(writer.CurrentPath));
        // The matching retained receipt is sufficient even after its original frame coalesces.
        writer.Observe(new AuthoritativeSnapshot(2, 0, SimulationSpeed.Speed0, [], DebugTuning: defaults),
            id => id == "reset" ? new(id, "", "", DebugTuningCommandTypes.Apply, CommandResultStatus.Executed, 0) : null);
        await writer.FlushAsync(); Assert.False(File.Exists(writer.CurrentPath)); Assert.True(writer.Status.Deleted);
        writer.Observe(defaults); await writer.FlushAsync(); Assert.False(File.Exists(writer.CurrentPath));
        await writer.DeactivateAsync(); writer.Activate(); await writer.FlushAsync(); Assert.False(File.Exists(writer.CurrentPath));
        writer.RegisterReset("reset-then-edit", 2);
        var final = Change(catalog, 4);
        writer.Observe(new AuthoritativeSnapshot(4, 0, SimulationSpeed.Speed0, [], DebugTuning: final,
            CommandResults: [new("reset-then-edit", "", "", DebugTuningCommandTypes.Apply, CommandResultStatus.Executed, 0)]));
        await writer.FlushAsync(); Assert.True(File.Exists(writer.CurrentPath)); Assert.False(writer.Status.Deleted);
        Assert.True(DebugTuningFileFormat.TryParse(File.ReadAllText(writer.CurrentPath), catalog.Defaults, out var patch, out _));
        Assert.Equal(5.4, patch.Values[0].Value, 8);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
