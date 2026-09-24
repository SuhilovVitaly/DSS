using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class TacticalMapSnapshotTests
{
    [Fact]
    public void Snapshot_button_writes_authoritative_and_render_state_with_camera_selection_and_speed()
    {
        string directory = Path.Combine(Path.GetTempPath(), "DSS-TacticalMapSnapshotTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            long clock = 0;
            var buffer = new SnapshotBuffer(() => clock);
            buffer.Update(new AuthoritativeSnapshot(
                17,
                12_345,
                SimulationSpeed.Speed2,
                ImmutableArray.Create(
                    new ObjectMotionSnapshot("PLAYER", 10_000, 10_000, 1.5, 90,
                        ActiveEngineCommandType: ShipEngineCommandTypes.Accelerate,
                        RenderObjectType: SpaceObjectType.PlayerShip),
                    new ObjectMotionSnapshot("TARGET", 10_050, 10_000, .5, 270,
                        RenderObjectType: SpaceObjectType.Station)),
                "PLAYER"));

            var screen = new GameSessionScreen(
                buffer,
                new LinearMotionPredictor(),
                timestampProvider: () => clock,
                tacticalMapSnapshotDirectory: directory);

            using var bitmap = new SKBitmap(1920, 1080);
            using var canvas = new SKCanvas(bitmap);
            clock += Stopwatch.Frequency * 50 / 1000;
            screen.Render(canvas, 1920, 1080);

            // Select the target first so the capture proves that the click-state is
            // written independently from the authoritative snapshot's object list.
            screen.OnMouseDown(1010, 540);
            screen.OnMouseDown(screen.MapViewButtonRects[4].MidX, screen.MapViewButtonRects[4].MidY);

            string? path = screen.LastTacticalMapSnapshotPath;
            Assert.NotNull(path);
            using var document = JsonDocument.Parse(File.ReadAllText(path!));
            JsonElement state = document.RootElement.GetProperty("state");

            Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal(17UL, state.GetProperty("authoritativeSnapshot").GetProperty("snapshotSequence").GetUInt64());
            Assert.Equal("PLAYER", state.GetProperty("playerShipObjectId").GetString());
            Assert.Equal("TARGET", state.GetProperty("selectedObjectId").GetString());
            Assert.Equal("Speed2", state.GetProperty("clientPredictionSpeed").GetString());
            Assert.Equal(2, state.GetProperty("objects").GetArrayLength());
            Assert.Contains(state.GetProperty("objects").EnumerateArray(), item =>
                item.GetProperty("authoritative").GetProperty("objectId").GetString() == "TARGET" &&
                item.GetProperty("isSelected").GetBoolean());
            Assert.True(state.GetProperty("camera").GetProperty("pixelsPerWorldUnit").GetDouble() > 0);
            Assert.Equal(1920, state.GetProperty("viewport").GetProperty("width").GetInt32());
            Assert.Equal(1080, state.GetProperty("viewport").GetProperty("height").GetInt32());
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Writer_does_not_overwrite_two_captures_with_the_same_timestamp_and_sequence()
    {
        string directory = Path.Combine(Path.GetTempPath(), "DSS-TacticalMapSnapshotTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var document = new TacticalMapSnapshotDocument(
                TacticalMapSnapshotWriter.CurrentSchemaVersion,
                new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero),
                new TacticalMapSnapshotState(
                    new AuthoritativeSnapshot(4, 0, SimulationSpeed.Speed0, ImmutableArray<ObjectMotionSnapshot>.Empty),
                    0,
                    null,
                    0,
                    0,
                    SimulationSpeed.Speed0,
                    0,
                    0,
                    null,
                    null,
                    null,
                    null,
                    new(0, 0, 1, true, false, 1),
                    new(0, 0, 1, 1, 1, 1, 1, false),
                    new(true, false, false, 0, 0),
                    new(),
                    [],
                    [],
                    [],
                    new(false, 0, 0, 0, SimulationSpeed.Speed0, [], [], [])));

            string first = TacticalMapSnapshotWriter.Write(document, directory);
            string second = TacticalMapSnapshotWriter.Write(document, directory);

            Assert.NotEqual(first, second);
            Assert.True(File.Exists(first));
            Assert.True(File.Exists(second));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
