using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI;

internal sealed class MapFrameEvidence
{
    private readonly string _path;
    private readonly List<double> _intervals = new(600);
    private readonly List<double> _submit = new(600);
    private readonly List<double> _swap = new(600);
    private readonly HashSet<string> _observedSpeeds = new(StringComparer.Ordinal);
    private long _previous;
    private int _frames;
    internal bool Completed { get; private set; }
    internal int FrameCount => _frames;

    internal MapFrameEvidence(string path) => _path = Path.GetFullPath(path);
    internal static MapFrameEvidence? FromEnvironment() =>
        Create(Environment.GetEnvironmentVariable("DSS_MAP_FRAME_REPORT"));
    internal static MapFrameEvidence? Create(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : new(path);

    internal void Record(long presentedAt, double cpuSubmitMs, MapFrameContext context, double swapWaitMs = 0)
    {
        if (Completed) return;
        double interval = _previous == 0 ? 0 : Stopwatch.GetElapsedTime(_previous, presentedAt).TotalMilliseconds;
        _previous = presentedAt;
        _frames++;
        _observedSpeeds.Add(context.Speed);
        if (_frames <= 120 || interval <= 0) return;
        _intervals.Add(interval); _submit.Add(cpuSubmitMs); _swap.Add(swapWaitMs);
        if (_intervals.Count < 600) return;
        var frame = Statistics(_intervals);
        var cpu = Statistics(_submit);
        bool displayLimited = context.VSync && context.MonitorRefreshHz is > 0 and < 80;
        string verdict = displayLimited ? "display-limited" : frame.P99Ms <= 12.5 ? "passed" : "failed";
        var report = new
        {
            schemaVersion = 1,
            status = "measured",
            backend = "OpenGL/Skia native window; swap-completion intervals",
            startedFrom = Environment.GetEnvironmentVariable("DSS_MAP_FRAME_CASE") ?? "interactive",
            commit = Environment.GetEnvironmentVariable("DSS_MAP_FRAME_COMMIT") ??
                typeof(MapFrameEvidence).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            moduleId = typeof(MapFrameEvidence).Assembly.ManifestModule.ModuleVersionId,
            machine = new
            {
                cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
                logicalProcessors = Environment.ProcessorCount,
                runtime = RuntimeInformation.FrameworkDescription,
                os = RuntimeInformation.OSDescription
            },
            context,
            warmupFrames = 120,
            measuredFrames = 600,
            frameIntervals = frame,
            cpuSubmit = cpu,
            swapWait = Statistics(_swap),
            observedSpeeds = _observedSpeeds.Order().ToArray(),
            targetFps = 80,
            targetVerdict = verdict,
            limitation = displayLimited ? "VSync monitor cadence is below 80 Hz; 80 FPS is not confirmed." : null,
            timingDefinition = "CPU submit ends before SwapBuffers. Intervals include the actual swap wait; GPU execution/physical scanout are not measured.",
            intervalsMs = _intervals,
            cpuSubmitMs = _submit
        };
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Completed = true;
    }

    internal static FrameStatistics Statistics(IEnumerable<double> source)
    {
        var sorted = source.Order().ToArray();
        double At(double p) => sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * p) - 1)];
        double mean = sorted.Average();
        return new(sorted.Length, mean, At(.5), At(.95), At(.99), sorted[^1], mean > 0 ? 1000 / mean : 0);
    }
}

internal sealed record FrameStatistics(int Count, double MeanMs, double P50Ms, double P95Ms, double P99Ms, double MaxMs, double MeanFps);
internal sealed record MapFrameContext(int Width, int Height, bool VSync, double? MonitorRefreshHz,
    string? Gpu, string? GraphicsVersion, float UiScale, string Speed, ulong? Seed, int? GeneratorVersion,
    int Planets, int Belts, int Objects, double PixelsPerWorldUnit, string? SelectedObjectId,
    MapFrameCounts? MapCounts = null, string? Layers = null, long? CalendarEpochMs = null, long? MotionEpochMs = null,
    string? SelectedFieldId = null, string? SelectedPoiId = null);

internal sealed record MapFrameCounts(int Entities, int Clusters, int HumanStations, int AiBases, int Territories,
    int Fields, int PointsOfInterest, int ConfiguredBeltDecorationSamples, int ConfiguredDebrisDecorationSamples)
{
    internal static MapFrameCounts From(AuthoritativeSnapshot? s) => new(s?.Objects.Length ?? 0,
        s?.ClusterMap?.Clusters.Length ?? 0, s?.ClusterMap?.Stations.Length ?? 0, s?.AiMap?.Bases.Length ?? 0,
        s?.AiMap is { Territories.IsDefaultOrEmpty: false } a ? a.Territories.Length : 0,
        s?.AiMap is { Fields.IsDefaultOrEmpty: false } b ? b.Fields.Length : 0,
        s?.AiMap is { PointsOfInterest.IsDefaultOrEmpty: false } c ? c.PointsOfInterest.Length : 0,
        s?.SolarSystemMap?.Belts.Sum(b => Math.Clamp(b.DecorationSamples, 0, 65536)) ?? 0,
        s?.AiMap is { Fields.IsDefaultOrEmpty: false } d ? d.Fields.Count(f => f.Kind == "Debris") * 64 : 0);
}
