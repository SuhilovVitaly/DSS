using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>Always-on, bounded, asynchronous evidence for combat motion and presentation.</summary>
internal sealed class CombatMetricsWriter : IAsyncDisposable
{
    internal static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "CombatMetrics");
    private readonly string _directory;
    private readonly string _stem = $"combat-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}";
    private readonly Channel<object> _records = Channel.CreateBounded<object>(new BoundedChannelOptions(128)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly Task _worker;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    private long _lastEventId;
    private long _dropped;
    private long _lastVisualTimestamp;
    private bool _hasVisualTimestamp;
    private int _combatEnabled;
    internal string? FilePath { get; private set; }
    internal Exception? Error { get; private set; }
    internal long DroppedRecords => Interlocked.Read(ref _dropped);

    internal CombatMetricsWriter(string? directory = null)
    {
        _directory = directory ?? DefaultDirectory;
        _worker = Task.Run(WriteLoop);
    }

    internal void Record(AuthoritativeSnapshot snapshot)
    {
        if (snapshot.Objects.Any(o => o.Torpedo is not null || o.Countermeasure is not null) ||
            (!snapshot.InstalledModules.IsDefaultOrEmpty && snapshot.InstalledModules.Any(m => m.LauncherCombat is not null || m.Defense is not null)))
            Volatile.Write(ref _combatEnabled, 1);
        if (Volatile.Read(ref _combatEnabled) == 0) return;
        var journal = snapshot.CombatJournal.IsDefault ? [] : snapshot.CombatJournal;
        if (!journal.IsEmpty && journal[^1].EventId < _lastEventId) _lastEventId = 0;
        var events = journal.Where(e => e.EventId > _lastEventId).ToArray();
        if (!journal.IsEmpty) _lastEventId = journal[^1].EventId;
        var objects = snapshot.Objects.Where(IsCombatContact).Select(o => new
        {
            o.ObjectId,
            o.ObjectType,
            o.RenderObjectType,
            o.RelationToPlayer,
            o.X,
            o.Y,
            o.SpeedKmS,
            o.Direction,
            o.Torpedo,
            o.Countermeasure,
            o.HostileCombat
        }).ToArray();
        var modules = snapshot.InstalledModules.IsDefaultOrEmpty ? [] : snapshot.InstalledModules
            .Where(m => m.LauncherCombat is not null || m.Defense is not null)
            .Select(m => new { m.ModuleId, m.StructureCurrent, m.LauncherCombat, m.Defense }).ToArray();
        Enqueue(new
        {
            Kind = "authoritative",
            Utc = DateTimeOffset.UtcNow,
            Timestamp = Stopwatch.GetTimestamp(),
            snapshot.SnapshotSequence,
            snapshot.GameTimeMs,
            snapshot.MotionTimeMs,
            snapshot.CurrentSpeed,
            snapshot.PlayerShipObjectId,
            Objects = objects,
            Modules = modules,
            Events = events
        });
    }

    internal void RecordVisual(AuthoritativeSnapshot snapshot, IReadOnlyList<ObjectRenderState> states, long predictionMs, long timestamp)
    {
        if (Volatile.Read(ref _combatEnabled) == 0 ||
            (_hasVisualTimestamp && timestamp >= _lastVisualTimestamp && timestamp - _lastVisualTimestamp < Stopwatch.Frequency / 5)) return;
        _hasVisualTimestamp = true;
        _lastVisualTimestamp = timestamp;
        var objects = states.Where(s => IsCombatContact(s.Source)).Select(s => new
        {
            s.Source.ObjectId,
            X = s.Pose.X,
            Y = s.Pose.Y,
            Direction = s.Pose.Direction,
            ConfirmedX = s.Source.X,
            ConfirmedY = s.Source.Y,
            ConfirmedDirection = s.Source.Direction
        }).ToArray();
        Enqueue(new
        {
            Kind = "visual",
            Utc = DateTimeOffset.UtcNow,
            Timestamp = timestamp,
            snapshot.SnapshotSequence,
            snapshot.MotionTimeMs,
            PredictionDeltaMs = predictionMs,
            Objects = objects
        });
    }

    private static bool IsCombatContact(ObjectMotionSnapshot o) => o.Torpedo is not null || o.Countermeasure is not null ||
        o.RenderObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip or SpaceObjectType.Wreck;

    private void Enqueue(object record)
    {
        if (Error is not null) return;
        if (!_records.Writer.TryWrite(record)) Interlocked.Increment(ref _dropped);
    }

    private async Task WriteLoop()
    {
        StreamWriter? writer = null;
        long bytes = 0;
        int part = 0;
        try
        {
            await foreach (var record in _records.Reader.ReadAllAsync())
            {
                if (writer is null || bytes >= 64 * 1024 * 1024)
                {
                    if (writer is not null) await writer.DisposeAsync();
                    Directory.CreateDirectory(_directory);
                    FilePath = Path.Combine(_directory, $"{_stem}-{part++:D3}.jsonl");
                    writer = new StreamWriter(new FileStream(FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
                        65536, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    { AutoFlush = true };
                    var header = new
                    {
                        Kind = "session",
                        SchemaVersion = 1,
                        Utc = DateTimeOffset.UtcNow,
                        Build = typeof(CombatMetricsWriter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                        TimestampFrequency = Stopwatch.Frequency,
                        WorldUnitsPerKm = 10,
                        TimeUnit = "physical milliseconds",
                        VisualSampleIntervalMs = 200,
                        Part = part - 1
                    };
                    await writer.WriteLineAsync(JsonSerializer.Serialize(header, Options));
                    bytes = 0;
                }
                string line = JsonSerializer.Serialize(new { DroppedRecords, Frame = record }, Options);
                bytes += System.Text.Encoding.UTF8.GetByteCount(line) + 1;
                await writer.WriteLineAsync(line);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            Error = error; // A diagnostic failure must never fail the session.
        }
        finally
        {
            if (writer is not null)
            {
                try { await writer.DisposeAsync(); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { Error = error; }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _records.Writer.TryComplete();
        await _worker;
    }
}
