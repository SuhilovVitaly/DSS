using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal sealed record TacticalMapSnapshotDocument(
    int SchemaVersion,
    DateTimeOffset CapturedAtUtc,
    TacticalMapSnapshotState State);

internal sealed record TacticalMapSnapshotState(
    AuthoritativeSnapshot? AuthoritativeSnapshot,
    long CaptureTimestampTicks,
    long? SnapshotReceivedAtTimestamp,
    long? EffectivePredictionDeltaMs,
    long? PredictedGameTimeMs,
    SimulationSpeed? ClientPredictionSpeed,
    long? ReconciliationForwardJumpMs,
    long? TotalReconciliationForwardJumpMs,
    string? PlayerShipObjectId,
    string? ActiveObjectId,
    string? SelectedObjectId,
    string? NavigationTargetObjectId,
    TacticalMapCameraSnapshot Camera,
    TacticalMapViewportSnapshot Viewport,
    TacticalMapUiSnapshot Ui,
    TacticalMapSettings MapSettings,
    IReadOnlyList<TacticalMapObjectFrame> Objects,
    IReadOnlyList<TacticalMapTrail> Trails,
    IReadOnlyList<TacticalMapTrajectory> Trajectories,
    TacticalMapReconciliationSnapshot Reconciliation);

internal sealed record TacticalMapCameraSnapshot(
    double FocusX,
    double FocusY,
    double PixelsPerWorldUnit,
    bool IsFocusAttachedToPlayer,
    bool IsZoomAnimating,
    double ZoomTargetPpu);

internal sealed record TacticalMapViewportSnapshot(
    int Width,
    int Height,
    float UiScale,
    float MouseX,
    float MouseY,
    float UiMouseX,
    float UiMouseY,
    bool HasMousePosition);

internal sealed record TacticalMapUiSnapshot(
    bool PanelVisible,
    bool IsPanningMap,
    bool IsCtrlDown,
    int MapClusterCount,
    int ClusteredObjectCount);

internal sealed record TacticalMapObjectFrame(
    ObjectMotionSnapshot Authoritative,
    ObjectMotionSnapshot Rendered,
    bool IsPlayerShip,
    float ScreenX,
    float ScreenY,
    bool IsActive,
    bool IsSelected,
    bool IsClustered);

internal sealed record TacticalMapTrail(
    string ObjectId,
    int Capacity,
    IReadOnlyList<TacticalMapPoint> Points);

internal sealed record TacticalMapTrajectory(
    string ObjectId,
    string Kind,
    IReadOnlyList<TacticalMapPoint> Points);

internal sealed record TacticalMapPoint(double X, double Y, long? Timestamp = null);

internal sealed record TacticalMapReconciliationSnapshot(
    bool HasSnapshotBaseline,
    ulong LastSnapshotBaselineSequence,
    long LastSnapshotBaselineGameTimeMs,
    long LastObservedForwardJumpMs,
    SimulationSpeed PreviousRenderSpeed,
    IReadOnlyList<ObjectMotionSnapshot> LastSnapshotBaselineObjects,
    IReadOnlyList<TacticalMapCorrection> VisualCorrections,
    IReadOnlyList<TacticalMapAnchoredPose> PausedVisualAnchors);

internal sealed record TacticalMapCorrection(
    string ObjectId,
    double OffsetX,
    double OffsetY,
    double DirectionOffset,
    double ElapsedSeconds);

internal sealed record TacticalMapAnchoredPose(
    string ObjectId,
    ObjectMotionSnapshot Pose);

internal static class TacticalMapSnapshotWriter
{
    internal const string DefaultDirectory = "TacticalMapSnapshots";
    internal const int CurrentSchemaVersion = 1;

    private static readonly object Sync = new();
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    static TacticalMapSnapshotWriter()
    {
        SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    internal static string Write(TacticalMapSnapshotDocument document, string directory = DefaultDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        string json = JsonSerializer.Serialize(document, SerializerOptions);
        string fullDirectory = Path.GetFullPath(directory);
        Directory.CreateDirectory(fullDirectory);

        string timestamp = document.CapturedAtUtc.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        string sequence = document.State.AuthoritativeSnapshot?.SnapshotSequence.ToString(CultureInfo.InvariantCulture) ?? "none";
        string baseName = $"tactical-map-{timestamp}-seq{sequence}";

        lock (Sync)
        {
            for (int suffix = 0; ; suffix++)
            {
                string name = suffix == 0 ? $"{baseName}.json" : $"{baseName}-{suffix}.json";
                string path = Path.Combine(fullDirectory, name);
                if (File.Exists(path))
                    continue;

                string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
                try
                {
                    File.WriteAllText(temporaryPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    File.Move(temporaryPath, path);
                    return path;
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
            }
        }
    }
}
