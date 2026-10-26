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
    internal const long DirectoryLimitBytes = 100 * 1024 * 1024;
    private static readonly SemaphoreSlim DiskGate = new(1, 1);
    private static readonly Dictionary<string, long> ActiveFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly long _limitBytes;
    private readonly long _partBytes;
    internal static string DefaultDirectory => UI.InterfaceLog.DiagnosticDirectory is { } diagnostics ? Path.Combine(diagnostics, "CombatMetrics") : AppPaths.Current.CombatMetrics;
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
    internal void RecordPresentation(object record) => Enqueue(record);

    internal CombatMetricsWriter(string? directory = null, long limitBytes = DirectoryLimitBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limitBytes, 4096);
        _directory = Path.GetFullPath(directory ?? DefaultDirectory);
        _limitBytes = limitBytes;
        _partBytes = Math.Min(10 * 1024 * 1024, limitBytes / 4);
        _worker = Task.Run(WriteLoop);
    }

    internal void Record(AuthoritativeSnapshot snapshot)
    {
        if (snapshot.Objects.Any(o => o.OwnProjectile is not null || o.ObservedProjectile is not null) ||
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
            o.OwnProjectile,
            o.ObservedProjectile,
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

    private static bool IsCombatContact(ObjectMotionSnapshot o) => o.OwnProjectile is not null || o.ObservedProjectile is not null ||
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
                string line = JsonSerializer.Serialize(new { DroppedRecords, Frame = record }, Options);
                long lineBytes = System.Text.Encoding.UTF8.GetByteCount(line) + 1;
                await DiskGate.WaitAsync();
                try
                {
                    Directory.CreateDirectory(_directory);
                    if (writer is not null && bytes + lineBytes > _partBytes)
                    {
                        await writer.DisposeAsync();
                        ActiveFiles.Remove(FilePath!);
                        writer = null;
                    }
                    string? header = writer is null ? JsonSerializer.Serialize(new
                    {
                        Kind = "session",
                        SchemaVersion = 2,
                        Utc = DateTimeOffset.UtcNow,
                        Build = typeof(CombatMetricsWriter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                        TimestampFrequency = Stopwatch.Frequency,
                        WorldUnitsPerKm = 10,
                        TimeUnit = "physical milliseconds",
                        VisualSampleIntervalMs = 200,
                        Part = part
                    }, Options) : null;
                    long headerBytes = header is null ? 0 : System.Text.Encoding.UTF8.GetByteCount(header) + 1;
                    if (!ReserveSpace(lineBytes + headerBytes))
                    {
                        Interlocked.Increment(ref _dropped);
                        continue;
                    }
                    if (header is not null)
                    {
                        FilePath = Path.Combine(_directory, $"{_stem}-{part++:D3}.jsonl");
                        writer = new StreamWriter(new FileStream(FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
                            65536, FileOptions.Asynchronous | FileOptions.SequentialScan))
                        { AutoFlush = true, NewLine = "\n" };
                        ActiveFiles[FilePath] = 0;
                        await writer.WriteLineAsync(header);
                        bytes = headerBytes;
                    }
                    await writer!.WriteLineAsync(line);
                    bytes += lineBytes;
                    ActiveFiles[FilePath!] = bytes;
                }
                finally { DiskGate.Release(); }
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
                await DiskGate.WaitAsync();
                try { await writer.DisposeAsync(); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { Error = error; }
                finally { ActiveFiles.Remove(FilePath!); DiskGate.Release(); }
            }
        }
    }

    // Only our top-level metric files are eligible for removal. Open sessions retain
    // their current part; when space cannot be reclaimed, diagnostics drop records.
    private bool ReserveSpace(long incomingBytes)
    {
        if (incomingBytes > _limitBytes) return false;
        var files = new DirectoryInfo(_directory).GetFiles("combat-*.jsonl")
            .OrderBy(f => f.LastWriteTimeUtc).ThenBy(f => f.Name, StringComparer.Ordinal).ToArray();
        // Windows directory entries can report an old length while a file is open.
        long total = files.Sum(f => ActiveFiles.TryGetValue(f.FullName, out long size) ? size : f.Length);
        foreach (var file in files)
        {
            if (incomingBytes <= _limitBytes && total <= _limitBytes - incomingBytes) return true;
            if (ActiveFiles.ContainsKey(file.FullName)) continue;
            try
            {
                long size = file.Length;
                file.Delete();
                total -= size;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
        return incomingBytes <= _limitBytes && total <= _limitBytes - incomingBytes;
    }

    public async ValueTask DisposeAsync()
    {
        _records.Writer.TryComplete();
        await _worker.ConfigureAwait(false);
    }
}
