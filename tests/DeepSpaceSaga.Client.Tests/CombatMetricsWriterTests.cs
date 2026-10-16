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
            var route = new TorpedoRoute(1000, 1, TorpedoRoutePhase.Straight, true, [new(0, 0, 90, 20, 0, 1000)]);
            var flight = new CountermeasureSnapshot("player", "defense", "torpedo", CountermeasurePhase.Guiding,
                route, 1000, 580, new(new("crew", "Crew", WeaponSkillType.CountermeasureDefense, 20, 58, 78), 70));
            var contact = new ObjectMotionSnapshot("countermeasure", 0, 0, 20, 90, Countermeasure: flight);
            var snapshot = new AuthoritativeSnapshot(1, 300000, SimulationSpeed.Speed1, [contact], "player",
                CombatJournal: [new(1, 1000, CombatEventType.Launch, "player", "torpedo", "countermeasure", 0, 0)],
                SimulationTimeMs: 1000);
            var writer = new CombatMetricsWriter(directory);
            writer.Record(snapshot);
            writer.RecordVisual(snapshot, [new(contact, contact with { X = 5 }, false)], 25, 0);
            writer.RecordVisual(snapshot, [new(contact, contact, false)], 30, Stopwatch.Frequency / 100);
            writer.Record(snapshot with { SnapshotSequence = 2 });
            await writer.DisposeAsync();
            Assert.Null(writer.Error);
            var lines = File.ReadAllLines(writer.FilePath!);
            using var header = JsonDocument.Parse(lines[0]);
            Assert.Equal(1, header.RootElement.GetProperty("schemaVersion").GetInt32());
            using var first = JsonDocument.Parse(lines[1]);
            var frame = first.RootElement.GetProperty("frame");
            Assert.Equal("authoritative", frame.GetProperty("kind").GetString());
            Assert.Equal(1000, frame.GetProperty("motionTimeMs").GetInt64());
            Assert.Equal(580, frame.GetProperty("objects")[0].GetProperty("countermeasure").GetProperty("frozenChanceTenths").GetInt32());
            Assert.Equal(1, frame.GetProperty("events").GetArrayLength());
            using var visual = JsonDocument.Parse(lines[2]);
            Assert.Equal(5, visual.RootElement.GetProperty("frame").GetProperty("objects")[0].GetProperty("x").GetDouble());
            using var second = JsonDocument.Parse(lines[3]);
            Assert.Empty(second.RootElement.GetProperty("frame").GetProperty("events").EnumerateArray());
            Assert.Equal(4, lines.Length);
            Assert.Equal(0, snapshot.Objects[0].X);
            Assert.Equal(0, snapshot.Objects[0].Countermeasure!.Route.ElapsedMs);
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
                [new("torp", "module.torpedo.launcher.basic", "Torpedo", 1, [], LauncherCombat: new(10, 90, 150))]));
            await writer.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(writer.Error);
        }
        finally { File.Delete(path); }
    }
}
