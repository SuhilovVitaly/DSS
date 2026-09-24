using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

// Value records and a fixed ring keep continuous recording allocation-free.
internal readonly record struct TacticalMapStageTimes(
    double CoordinatesAndHitTestMs, double GridMs, double TrailsMs, double ForecastsMs,
    double MarkersAndLabelsMs, double CommandPanelMs, double InfoPanelsMs);

internal readonly record struct TacticalMapTrackedObject(
    string ObjectId, double AuthoritativeX, double AuthoritativeY, double AuthoritativeDirection,
    double RawPredictedX, double RawPredictedY, double RawPredictedDirection, double RenderedX, double RenderedY,
    double Direction, double SpeedKmS, float ScreenX, float ScreenY,
    string? Command, double? RouteElapsedMs, double? RouteDurationMs,
    double CorrectionX, double CorrectionY, double CorrectionDirectionDegrees, double CorrectionAgeSeconds);

internal readonly record struct TacticalMapWindowTiming(
    double CallbackIntervalMs, double CpuBeforeFlushMs, double CpuFlushMs, double CpuCallbackMs,
    bool IsFocused, bool VSync, int ScreenCount, int FramebufferWidth, int FramebufferHeight,
    double PresentWaitMs = 0, string? GraphicsRenderer = null, string? GraphicsVersion = null,
    int GpuResources = 0, long GpuCacheBytes = 0, long GpuCacheLimitBytes = 0);

internal readonly record struct TacticalMapFrameProfile(
    long FrameId, long TimestampTicks, double FrameIntervalMs, double RenderCpuMs,
    TacticalMapStageTimes Stages, long RenderAllocatedBytes, long ThreadAllocatedBytesSincePreviousFrame,
    int Gen0Collections, int Gen1Collections, int Gen2Collections, double ProcessGcPauseMs,
    ulong? SnapshotSequence, bool NewSnapshot, long? SnapshotReceivedAtTicks, double? SnapshotAgeMs,
    long? MotionTimeMs, long? CalendarTimeMs, long? PredictionDeltaMs, long? PredictedGameTimeMs,
    SimulationSpeed? Speed, long? ForwardJumpMs, long? TotalForwardJumpMs,
    double CameraX, double CameraY, double CameraStepPixels, double PixelsPerWorldUnit,
    bool FollowPlayer, bool ZoomAnimating, bool Panning, int ViewportWidth, int ViewportHeight,
    int ObjectCount, int ClusterCount, int TrailPointCount, int ForecastPointCount,
    double? PlayerTrajectoryStartX, double? PlayerTrajectoryStartY, double? PlayerTrajectoryEndX, double? PlayerTrajectoryEndY,
    int CorrectionCount,
    double LargestCorrectionPixels, TacticalMapTrackedObject? Player, TacticalMapTrackedObject? Target,
    bool CaptureRequested, bool CaptureWriterBusy,
    double CaptureCopyMs = 0, TacticalMapWindowTiming? Window = null);

internal sealed record TacticalMapProfileSummary(
    int FrameCount, double HistorySeconds, double FrameIntervalP50Ms, double FrameIntervalP95Ms,
    double FrameIntervalP99Ms, double MaximumFrameIntervalMs, double MaximumRenderCpuMs,
    long[] WorstFrameIds);

internal sealed record TacticalMapProfileCapture(
    string SessionId, long StopwatchFrequency, int Capacity, double MaximumHistorySeconds,
    string Runtime, string OS, string Architecture, string ClientVersion, string ClientModuleId,
    string MeasurementNotes, TacticalMapFrameProfile[] Frames, TacticalMapProfileSummary? Summary = null)
{
    internal TacticalMapProfileCapture WithSummary()
    {
        double[] intervals = Frames.Where(f => f.FrameIntervalMs > 0).Select(f => f.FrameIntervalMs).Order().ToArray();
        double Percentile(double p) => intervals.Length == 0 ? 0 : intervals[(int)Math.Ceiling(p * intervals.Length) - 1];
        return this with
        {
            Summary = new(Frames.Length,
                Frames.Length < 2 ? 0 : (Frames[^1].TimestampTicks - Frames[0].TimestampTicks) / (double)StopwatchFrequency,
                Percentile(.5), Percentile(.95), Percentile(.99), Percentile(1),
                Frames.Length == 0 ? 0 : Frames.Max(f => f.RenderCpuMs),
                Frames.OrderByDescending(f => f.FrameIntervalMs).Take(10).Select(f => f.FrameId).ToArray())
        };
    }
}

internal sealed class TacticalMapFrameRecorder
{
    internal const int Capacity = 4096;
    internal const double HistorySeconds = 30;
    private readonly TacticalMapFrameProfile[] _frames = new TacticalMapFrameProfile[Capacity];
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private int _next;
    private int _count;

    internal void Add(TacticalMapFrameProfile frame)
    {
        _frames[_next] = frame;
        _next = (_next + 1) % Capacity;
        _count = Math.Min(_count + 1, Capacity);
        while (_count > 1 && frame.TimestampTicks - _frames[(_next - _count + Capacity) % Capacity].TimestampTicks
               > HistorySeconds * Stopwatch.Frequency)
            _count--;
    }

    internal void CompleteWindow(TacticalMapWindowTiming timing)
    {
        if (_count == 0) return;
        int index = (_next - 1 + Capacity) % Capacity;
        _frames[index] = _frames[index] with { Window = timing };
    }

    internal void RecordCaptureCost(double milliseconds)
    {
        if (_count == 0) return;
        int index = (_next - 1 + Capacity) % Capacity;
        _frames[index] = _frames[index] with { CaptureCopyMs = milliseconds };
    }

    internal TacticalMapProfileCapture Capture()
    {
        var frames = new TacticalMapFrameProfile[_count];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = _frames[(_next - _count + i + Capacity) % Capacity];
        var assembly = typeof(GameSessionScreen).Assembly;
        return new(_sessionId, Stopwatch.Frequency, Capacity, HistorySeconds,
            RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            assembly.ManifestModule.ModuleVersionId.ToString(),
            "Frame intervals are unclamped render-start intervals and include scheduling, input and presentation waits. " +
            "Stage/flush times measure CPU work, NOT GPU execution. Window PresentWaitMs measures SwapBuffers, " +
            "including driver/GPU/display waits, not physical scanout time. CpuCallbackMs excludes that wait. " +
            "GC counters and pause time are process-wide; allocations are on the UI thread. " +
            "Snapshot time and poses belong to the same rendered map frame. Capture is serviced on the next render. " +
            "Window timing and capture copy cost of the capture frame are not yet available; later captures include them. " +
            "History retains at most 30 seconds and 4096 frames (less time on high-refresh displays).",
            frames);
    }
}
