using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

internal static class WindowLoopProbe
{
    internal static void Run(string[] args)
    {
        string output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        bool inputEnabled = !args.Contains("--no-input");
        bool visible = args.Contains("--visible");
        bool vsync = !args.Contains("--no-vsync");
        bool submit = args.Contains("--submit");
        bool finish = args.Contains("--finish");
        bool topMost = args.Contains("--topmost");
        int durationIndex = Array.IndexOf(args, "--seconds");
        double duration = durationIndex >= 0 ? double.Parse(args[durationIndex + 1], System.Globalization.CultureInfo.InvariantCulture) : 25;
        if (!double.IsFinite(duration) || duration <= 3) throw new ArgumentException("--seconds must exceed the 3 second warmup.");
        bool legacy = args.Contains("--legacy");
        int assetsIndex = Array.IndexOf(args, "--assets");
        string? assets = assetsIndex >= 0 ? Path.GetFullPath(args[assetsIndex + 1]) : null;
        int imageIndex = Array.IndexOf(args, "--image");
        string? imagePath = imageIndex >= 0 ? Path.GetFullPath(args[imageIndex + 1]) : null;
        if (imagePath is not null) Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);
        int snapshotIndex = Array.IndexOf(args, "--snapshot");
        string? snapshotPath = snapshotIndex >= 0 ? Path.GetFullPath(args[snapshotIndex + 1]) : null;
        string? snapshotSha256 = null;
        TacticalMapSnapshotState? state = null;
        bool showPrediction = true;
        if (snapshotIndex >= 0)
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            byte[] snapshotBytes = File.ReadAllBytes(snapshotPath!);
            snapshotSha256 = Convert.ToHexString(SHA256.HashData(snapshotBytes));
            using var json = JsonDocument.Parse(snapshotBytes);
            state = json.RootElement.GetProperty("state").Deserialize<TacticalMapSnapshotState>(options)!;
            if (json.RootElement.TryGetProperty("showTrajectoryPrediction", out var prediction)) showPrediction = prediction.GetBoolean();
        }
        if (assets is not null) Directory.SetCurrentDirectory(assets);
        using var window = Window.Create(WindowOptions.Default with
        {
            Title = "DSS window timing probe",
            Size = state is null ? new Vector2D<int>(1280, 720) : new(state.Viewport.Width, state.Viewport.Height),
            WindowBorder = WindowBorder.Hidden,
            Position = new(0, 0),
            IsVisible = visible,
            VSync = vsync,
            FramesPerSecond = 0,
            UpdatesPerSecond = 0,
            TopMost = topMost,
            ShouldSwapAutomatically = false,
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3))
        });
        window.Initialize();
        using var gl = window.CreateOpenGL();
        using var input = inputEnabled ? window.CreateInput() : null;
        string renderer = gl.GetStringS(StringName.Renderer);
        string vendor = gl.GetStringS(StringName.Vendor);
        using var glInterface = GRGlInterface.Create();
        var contextOptions = SkiaGpuOptions.Create();
        if (legacy) contextOptions.AllowPathMaskCaching = true;
        if (args.Contains("--no-path-cache")) contextOptions.AllowPathMaskCaching = false;
        using var context = state is null ? null : GRContext.CreateGl(glInterface, contextOptions);
        int cacheIndex = Array.IndexOf(args, "--cache-mb");
        if (cacheIndex >= 0) context?.SetResourceCacheLimit(long.Parse(args[cacheIndex + 1]) * 1024 * 1024);
        using var target = state is null ? null : new GRBackendRenderTarget(window.FramebufferSize.X, window.FramebufferSize.Y, 0, 8,
            new GRGlFramebufferInfo(0, SKColorType.Rgba8888.ToGlSizedFormat()));
        using var surface = context is null ? null : SKSurface.Create(context, target!, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        GameSessionScreen? screen = null;
        var stages = new Dictionary<string, double>();
        if (state is not null)
        {
            long replayOffset = 0;
            var buffer = new SnapshotBuffer(() => Stopwatch.GetTimestamp() + replayOffset);
            buffer.Update(state.AuthoritativeSnapshot!);
            buffer.CurrentSpeed = state.ClientPredictionSpeed ?? state.AuthoritativeSnapshot!.CurrentSpeed;
            if (buffer.CurrentSpeed != SimulationSpeed.Speed0)
                replayOffset = (long)((state.EffectivePredictionDeltaMs ?? 0) * (double)Stopwatch.Frequency / 1000 / (int)buffer.CurrentSpeed);
            screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: state.Viewport.UiScale, mapSettings: state.MapSettings);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var camera = (CameraState)typeof(GameSessionScreen).GetField("_camera", flags)!.GetValue(screen)!;
            camera.SetZoom(state.Camera.PixelsPerWorldUnit);
            camera.SetFocus(state.Camera.FocusX, state.Camera.FocusY);
            typeof(GameSessionScreen).GetField("_isFocusAttachedToPlayer", flags)!.SetValue(screen, state.Camera.IsFocusAttachedToPlayer);
            typeof(GameSessionScreen).GetField("_showTrajectoryPrediction", flags)!.SetValue(screen, showPrediction);
            typeof(GameSessionScreen).GetField("_selectedObjectId", flags)!.SetValue(screen, state.SelectedObjectId);
            typeof(GameSessionScreen).GetField("_navigationTargetId", flags)!.SetValue(screen, state.NavigationTargetObjectId);
            if (legacy)
            {
                var depth = typeof(GameSessionScreen).GetField("_depthRenderer", flags)!.GetValue(screen)!;
                var paint = (SKPaint)depth.GetType().GetField("_targetTrajectoryPaint", flags)!.GetValue(depth)!;
                paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, .55f);
                paint.Color = paint.Color.WithAlpha(170);
                var halo = (SKPaint)depth.GetType().GetField("_targetTrajectoryHaloPaint", flags)!.GetValue(depth)!;
                halo.Color = SKColors.Transparent;
            }
            if (args.Contains("--no-blur") || args.Contains("--no-reticle-blur") || args.Contains("--no-target-blur"))
            {
                var depth = typeof(GameSessionScreen).GetField("_depthRenderer", flags)!.GetValue(screen)!;
                foreach (var field in depth.GetType().GetFields(flags))
                    if (field.GetValue(depth) is SKPaint paint &&
                        (args.Contains("--no-blur") ||
                         (args.Contains("--no-reticle-blur") && field.Name == "_selectionGlowPaint") ||
                         (args.Contains("--no-target-blur") && field.Name == "_targetTrajectoryPaint"))) paint.MaskFilter = null;
            }
            if (args.Contains("--gpu-stages"))
                screen.RenderStageCompleted = name =>
                {
                    if (name == "begin") stages.Clear();
                    long timestamp = Stopwatch.GetTimestamp();
                    surface!.Canvas.Flush();
                    gl.Finish();
                    stages.TryGetValue(name, out double prior);
                    stages[name] = prior + Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
                };
        }
        double drawMs = 0, flushMs = 0;
        window.Render += _ =>
        {
            long begin = Stopwatch.GetTimestamp();
            if (screen is not null) screen.Render(surface!.Canvas, window.Size.X, window.Size.Y);
            else { gl.ClearColor(.02f, .03f, .04f, 1); gl.Clear(ClearBufferMask.ColorBufferBit); }
            long beforeFlush = Stopwatch.GetTimestamp();
            surface?.Canvas.Flush();
            if (submit) context?.Submit();
            if (finish) gl.Finish();
            drawMs = Ms(begin, beforeFlush);
            flushMs = Stopwatch.GetElapsedTime(beforeFlush).TotalMilliseconds;
        };
        var rows = new List<Row>(8000);
        long start = Stopwatch.GetTimestamp(), previous = start;
        bool imageWritten = false;
        while (!window.IsClosing && Stopwatch.GetElapsedTime(start).TotalSeconds < duration)
        {
            long a = Stopwatch.GetTimestamp();
            window.DoEvents();
            long b = Stopwatch.GetTimestamp();
            window.DoUpdate();
            long c = Stopwatch.GetTimestamp();
            window.DoRender();
            long d = Stopwatch.GetTimestamp();
            if (imagePath is not null && !imageWritten && surface is not null && Stopwatch.GetElapsedTime(start).TotalSeconds > 1)
            {
                using var screenshot = surface.Snapshot();
                using var data = screenshot.Encode(SKEncodedImageFormat.Png, 100);
                using var file = File.Create(imagePath);
                data.SaveTo(file);
                imageWritten = true;
            }
            window.SwapBuffers();
            long e = Stopwatch.GetTimestamp();
            int resources = 0; long resourceBytes = 0;
            context?.GetResourceCacheUsage(out resources, out resourceBytes);
            if (!visible || !vsync) Thread.Sleep(5);
            if (Stopwatch.GetElapsedTime(start).TotalSeconds > 3)
                rows.Add(new(Stopwatch.GetElapsedTime(start, a).TotalSeconds,
                    Ms(previous, a), Ms(a, b), Ms(b, c), Ms(c, d), Ms(d, e), drawMs, flushMs,
                    stages.Count == 0 ? null : new Dictionary<string, double>(stages),
                    GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), GC.GetTotalPauseDuration().TotalMilliseconds,
                    resources, resourceBytes));
            previous = a;
        }
        if (rows.Count == 0) throw new InvalidOperationException("Window closed before any measured frames were captured.");
        double[] intervals = rows.Select(r => r.IntervalMs).Order().ToArray();
        double Percentile(double p) => intervals[(int)Math.Ceiling(p * intervals.Length) - 1];
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            renderer,
            vendor,
            inputEnabled,
            visible,
            vsync,
            submit,
            finish,
            topMost,
            legacy,
            assets,
            startedAtUtc,
            snapshotPath,
            snapshotSha256,
            duration,
            WarmupSeconds = 3,
            graphicsVersion = gl.GetStringS(StringName.Version),
            clientModuleId = typeof(GameSessionScreen).Assembly.ManifestModule.ModuleVersionId,
            legacyNotes = legacy ? "Current client with old path-mask caching and target blur restored; not a historical binary." : null,
            pathMaskCaching = contextOptions.AllowPathMaskCaching,
            Summary = new
            {
                Frames = rows.Count,
                EventsMaxMs = rows.Max(r => r.EventsMs),
                SwapMaxMs = rows.Max(r => r.SwapMs),
                IntervalP50Ms = Percentile(.5),
                IntervalP95Ms = Percentile(.95),
                IntervalP99Ms = Percentile(.99),
                IntervalMaxMs = intervals[^1],
                FramesOver50Ms = intervals.Count(ms => ms > 50),
                MaximumGpuCacheBytes = rows.Max(r => r.GpuResourceBytes)
            },
            Rows = rows
        },
            new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(output);
        Console.WriteLine($"GPU={renderer}; events max={rows.Max(r => r.EventsMs):F2}ms; swap max={rows.Max(r => r.SwapMs):F2}ms");
    }

    private static double Ms(long a, long b) => Stopwatch.GetElapsedTime(a, b).TotalMilliseconds;
    private readonly record struct Row(double Seconds, double IntervalMs, double EventsMs, double UpdateMs, double RenderMs, double SwapMs, double DrawMs, double FlushMs, Dictionary<string, double>? GpuStages,
        int Gen0, int Gen1, int Gen2, double GcPauseMs, int GpuResources, long GpuResourceBytes);
}
