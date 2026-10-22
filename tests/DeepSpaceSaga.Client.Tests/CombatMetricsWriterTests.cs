using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatMetricsWriterTests
{
    [Fact]
    public async Task Actual_point_flights_are_logged_from_public_own_and_observed_payloads_without_legacy_fields()
    {
        await using var scene = await PointCombatUiFixture.Create(hostileShots: 1);
        Assert.Equal(CommandResultStatus.Executed, (await scene.Launch(x: 200, y: 100, profile: 3)).Status);
        await scene.Wait(() => scene.Presented.Objects.Any(o => o.OwnProjectile is not null));
        string directory = Path.Combine(Path.GetTempPath(), "dss-point-metrics-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using (var writer = new CombatMetricsWriter(directory))
            {
                // Observed combat must enable telemetry even without local weapon panels.
                writer.Record(scene.Presented with { InstalledModules = [] });
                writer.RecordVisual(scene.Presented, scene.Screen.RenderStates, 0, 0);
            }
            var records = Directory.GetFiles(directory, "*.jsonl").SelectMany(File.ReadAllLines).Select(line => JsonDocument.Parse(line)).ToArray();
            try
            {
                var frames = records.Where(r => r.RootElement.TryGetProperty("frame", out _)).Select(r => r.RootElement.GetProperty("frame")).ToArray();
                var facts = frames.Single(r => r.GetProperty("kind").GetString() == "authoritative").GetProperty("objects").EnumerateArray().ToArray();
                var own = Assert.Single(facts, o => o.GetProperty("ownProjectile").ValueKind == JsonValueKind.Object);
                var foreign = Assert.Single(facts, o => o.GetProperty("observedProjectile").ValueKind == JsonValueKind.Object);
                Assert.Equal("fast", own.GetProperty("ownProjectile").GetProperty("profileId").GetString());
                Assert.Equal(JsonValueKind.Null, foreign.GetProperty("ownProjectile").ValueKind);
                Assert.False(foreign.GetProperty("observedProjectile").TryGetProperty("detonationX", out _));
                Assert.All(facts, o => { Assert.False(o.TryGetProperty("torpedo", out _)); Assert.False(o.TryGetProperty("countermeasure", out _)); });
                var visual = frames.Single(r => r.GetProperty("kind").GetString() == "visual").GetProperty("objects").EnumerateArray();
                Assert.Contains(visual, o => o.GetProperty("objectId").GetString() == own.GetProperty("objectId").GetString());
            }
            finally { foreach (var record in records) record.Dispose(); }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Directory_quota_removes_oldest_metrics_and_keeps_valid_rotated_records()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-metrics-quota-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Assert.Equal(100 * 1024 * 1024, CombatMetricsWriter.DirectoryLimitBytes);
            string old = Path.Combine(directory, "combat-old-000.jsonl");
            await File.WriteAllTextAsync(old, new string('x', 6000));
            File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-2));
            string newer = Path.Combine(directory, "combat-newer-000.jsonl");
            await File.WriteAllTextAsync(newer, "{}\n");
            File.SetLastWriteTimeUtc(newer, DateTime.UtcNow.AddDays(-1));
            string unrelated = Path.Combine(directory, "notes.txt");
            await File.WriteAllTextAsync(unrelated, "Keep me");
            await using (var first = new CombatMetricsWriter(directory, 4096))
            {
                first.Record(MetricsSnapshot(1));
            }
            Assert.False(File.Exists(old));
            Assert.True(File.Exists(newer));
            await using (var second = new CombatMetricsWriter(directory, 4096))
            {
                for (int i = 2; i < 32; i++) second.Record(MetricsSnapshot(i));
            }
            var files = new DirectoryInfo(directory).GetFiles("combat-*.jsonl");
            Assert.InRange(files.Sum(f => f.Length), 1, 4096);
            Assert.True(files.Length > 1);
            Assert.Equal("Keep me", await File.ReadAllTextAsync(unrelated));
            foreach (var file in files)
                foreach (string line in await File.ReadAllLinesAsync(file.FullName))
                    using (JsonDocument.Parse(line)) { }
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Concurrent_sessions_share_quota_and_oversized_records_are_dropped()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-metrics-quota-" + Guid.NewGuid().ToString("N"));
        try
        {
            var first = new CombatMetricsWriter(directory, 8192);
            var second = new CombatMetricsWriter(directory, 8192);
            for (int i = 0; i < 32; i++) { first.Record(MetricsSnapshot(i)); second.Record(MetricsSnapshot(i)); }
            first.Record(MetricsSnapshot(33) with { PlayerShipObjectId = new string('x', 9000) });
            await Task.WhenAll(first.DisposeAsync().AsTask(), second.DisposeAsync().AsTask());
            Assert.Null(first.Error); Assert.Null(second.Error);
            Assert.True(first.DroppedRecords > 0);
            Assert.InRange(new DirectoryInfo(directory).GetFiles("combat-*.jsonl").Sum(f => f.Length), 0, 8192);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static AuthoritativeSnapshot MetricsSnapshot(long sequence) => new((ulong)sequence, 0, SimulationSpeed.Speed0, [],
        InstalledModules: [new("torp", "module.torpedo.launcher.basic", "Torpedo", 1, [], LauncherCombat: new(150))]);

    [Fact]
    public async Task Real_session_automatically_records_authoritative_and_rendered_combat_state()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-combat-metrics-" + Guid.NewGuid().ToString("N"));
        try
        {
            string client = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
            var connection = LocalGameSessionConnection.CreateFromScenarioFile(Path.Combine(client, "Settings.json"),
                Path.Combine(client, "Scenarios/CombatMukha/scenario.json"));
            var metrics = new CombatMetricsWriter(directory);
            await using var handle = new GameSessionHandle(connection, new SnapshotBuffer(), metrics);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (handle.Buffer.Latest is null)
            {
                Assert.Null(handle.Failure);
                await Task.Delay(10, timeout.Token);
            }
            var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
            using var bitmap = new SKBitmap(1280, 720);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 1280, 720);
            await handle.DisposeAsync();
            Assert.Null(metrics.Error);
            var records = File.ReadAllLines(metrics.FilePath!).Skip(1).Select(line =>
            {
                using var json = JsonDocument.Parse(line);
                return json.RootElement.GetProperty("frame").GetProperty("kind").GetString();
            }).ToArray();
            Assert.Contains("authoritative", records);
            Assert.Contains("visual", records);
            screen.OnDeactivated();
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Records_authoritative_geometry_events_and_throttled_visuals_without_mutating_snapshot()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-combat-metrics-" + Guid.NewGuid().ToString("N"));
        try
        {
            var flight = new OwnProjectileSnapshot(ProjectileKind.Countermissile, "player", "defense", 0, 0, 0, 0, 150, 0, 1000, 2000, 15, "wide", 1, 0, 0);
            var contact = new ObjectMotionSnapshot("countermeasure", 0, 0, 15, 90, OwnProjectile: flight);
            var snapshot = new AuthoritativeSnapshot(1, 300000, SimulationSpeed.Speed1, [contact], "player",
                CombatJournal: [new(1,1000,CombatEventType.Launch,"player","","countermeasure",0,0,
                    ProjectileKind:ProjectileKind.Countermissile,ProfileId:"wide",BlastRadiusKm:1)], SimulationTimeMs: 1000);
            var writer = new CombatMetricsWriter(directory);
            writer.Record(snapshot);
            writer.RecordVisual(snapshot, [new(contact, contact with { X = 5 }, false)], 25, 0);
            writer.RecordVisual(snapshot, [new(contact, contact, false)], 30, Stopwatch.Frequency / 100);
            writer.Record(snapshot with { SnapshotSequence = 2 });
            await writer.DisposeAsync();
            Assert.Null(writer.Error);
            var lines = File.ReadAllLines(writer.FilePath!);
            using var header = JsonDocument.Parse(lines[0]);
            Assert.Equal(2, header.RootElement.GetProperty("schemaVersion").GetInt32());
            using var first = JsonDocument.Parse(lines[1]);
            var frame = first.RootElement.GetProperty("frame");
            Assert.Equal("authoritative", frame.GetProperty("kind").GetString());
            Assert.Equal(1000, frame.GetProperty("motionTimeMs").GetInt64());
            Assert.Equal("wide", frame.GetProperty("objects")[0].GetProperty("ownProjectile").GetProperty("profileId").GetString());
            Assert.Equal(150, frame.GetProperty("objects")[0].GetProperty("ownProjectile").GetProperty("detonationX").GetDouble());
            Assert.Equal(1, frame.GetProperty("events").GetArrayLength());
            using var visual = JsonDocument.Parse(lines[2]);
            Assert.Equal(5, visual.RootElement.GetProperty("frame").GetProperty("objects")[0].GetProperty("x").GetDouble());
            using var second = JsonDocument.Parse(lines[3]);
            Assert.Empty(second.RootElement.GetProperty("frame").GetProperty("events").EnumerateArray());
            Assert.Equal(4, lines.Length);
            Assert.Equal(0, snapshot.Objects[0].X);
            Assert.Equal(flight, snapshot.Objects[0].OwnProjectile);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Unwritable_metrics_destination_does_not_throw_or_block_session_shutdown()
    {
        string path = Path.Combine(Path.GetTempPath(), "dss-combat-metrics-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, "This is a file, not a directory.");
        try
        {
            var writer = new CombatMetricsWriter(path);
            writer.Record(new(1, 0, SimulationSpeed.Speed0, [], InstalledModules:
                [new("torp", "module.torpedo.launcher.basic", "Torpedo", 1, [], LauncherCombat: new(150))]));
            await writer.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(writer.Error);
        }
        finally { File.Delete(path); }
    }
}
