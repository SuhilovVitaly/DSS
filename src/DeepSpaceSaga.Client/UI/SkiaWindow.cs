using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens.Finance;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Client.UI.Screens.ShipDestroyed;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.Save;
using DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;
using DeepSpaceSaga.Client.UI.Screens.Settings;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Client.UI.Screens.Contracts;
using DeepSpaceSaga.Client.UI.Screens.Dialogue;
using DeepSpaceSaga.Client.UI.Screens.Hire;
using DeepSpaceSaga.Client.UI.Screens.Station;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

public sealed class SkiaWindow : IDisposable, IDebugConsoleHost
{
    private readonly IWindow _window;
    private readonly IGameSessionFactory _sessionFactory;
    private readonly ManeuverInputSettings _maneuverInputs;
    private readonly ScreenStack _screens = new();
    private readonly WindowThreadContext _uiContext = new();
    private readonly SessionConnectionLoader _connectionLoader = new();

    private GL? _gl;
    private GRContext? _grContext;
    private GRBackendRenderTarget? _renderTarget;
    private SKSurface? _surface;

    private IInputContext? _input;
    private IMouse? _mouse;
    private IKeyboard? _keyboard;
    private RawImage? _defaultCursorImage;
    private RawImage? _interactiveCursorImage;

    private GameSessionHandle? _session;
    private Task _lastSessionDisposal = Task.CompletedTask;
    private string? _pendingUndockCommandId;
    private GameSessionScreen? _gameSessionScreen;
    private static readonly double[] AllowedUiScales = { 0.8, 1.0, 1.2, 1.5 };
    private readonly SemaphoreSlim _transitionLock = new(1, 1);
    private readonly StationTradeNavigation _stationNavigation;
    private Task? _stationCleanupTask;
    private GameSessionHandle? _shipDestroyedSession;
    private Task? _shipDestroyedTransitionTask;
    private int _modalDepth;
    private SimulationSpeed _savedSpeed = SimulationSpeed.Speed1;
    private bool _quickSaveLoadInFlight;
    private readonly KeyboardEdgeTracker _keyboardEdges;
    private readonly TacticalBattleInputSettings _tacticalInputs;
    private readonly Key[] _keyboardPressedKeys = new Key[64];
    private readonly Key[] _keyboardReleasedKeys = new Key[64];
    private readonly Key[] _keyboardRepeatedKeys = new Key[4];
    private IScreen? _repeatScreen;
    private readonly Action<Key> _handleKeyboardEdge;
    private bool _disposed;
    private bool _closing;
    // Set by main-menu Exit, which bypasses _transitionLock: an in-flight tutorial start must not open its battle afterwards.
    private bool _exitRequested;
    private bool _isFocused = true;
    // Leave room for the elapsed-time diagnostic before the first focus event.
    private long _focusRegainedAtMs = long.MinValue / 2;

    // TEMP DIAG — startup timing investigation, remove once resolved.
    private readonly System.Diagnostics.Stopwatch? _startupStopwatch;
    private bool _firstFrameLogged;
    private long _previousProfileRenderTimestamp;
    private string? _graphicsRenderer;
    private string? _graphicsVersion;
    private readonly MapFrameEvidence? _mapFrameEvidence = MapFrameEvidence.FromEnvironment();
    private double? _monitorRefreshHz;
    private readonly bool _requireSmallerMonitor = Environment.GetEnvironmentVariable("DSS_VALIDATION_SMALL_MONITOR") == "1";
    private readonly TacticalBattleSkinCatalog _battleSkinCatalog;
    private TacticalBattleSkin _battleSkin;
    // Optional native evidence hook: the surface still contains this screen's
    // rendered pixels, before polling transitions or swapping the framebuffer.
    internal Action<SKSurface, IScreen?>? NativeFrameRendered { get; set; }
    internal Action<IScreen?, double>? NativeFrameMeasured { get; set; }
    internal Action<IScreen?, long, double, double, double>? NativeFramePresented { get; set; }

    // EP-0030 debug console: a window-level overlay, never a ScreenStack entry; it does not touch pause or speed.
    private readonly DebugConsoleOverlay _debugConsoleOverlay = new();
    private bool _debugConsoleShown;
    private float _debugConsoleUiScale = 1;
    internal DebugConsoleController DebugConsole { get; }

    public SkiaWindow(IScreen initialScreen, IGameSessionFactory sessionFactory, System.Diagnostics.Stopwatch? startupStopwatch = null)
        : this(initialScreen, sessionFactory, startupStopwatch, createNativeWindow: true) { }

    internal SkiaWindow(IScreen initialScreen, IGameSessionFactory sessionFactory,
        System.Diagnostics.Stopwatch? startupStopwatch, bool createNativeWindow, string? settingsPath = null)
    {
        _sessionFactory = sessionFactory;
        _maneuverInputs = ManeuverInputSettings.Load(Path.Combine(AppContext.BaseDirectory, ManeuverInputSettings.RelativePath));
        _tacticalInputs = TacticalBattleInputSettings.Load(Path.Combine(AppContext.BaseDirectory, TacticalBattleInputSettings.RelativePath),
            _maneuverInputs, InterfaceLog.Write);
        _keyboardEdges = new(_tacticalInputs, _maneuverInputs);
        _stationNavigation = new(() => _session);
        _handleKeyboardEdge = HandleKeyboardEdge;
        _startupStopwatch = startupStopwatch;
        DebugConsole = new DebugConsoleController(DebugCommandCatalog.CreateDefault(), this);

        SettingsFilePath = Path.GetFullPath(settingsPath ?? Path.Combine(AppContext.BaseDirectory, "Settings.json"));
        _battleSkinCatalog = TacticalBattleSkinCatalog.Load(Path.Combine(Path.GetDirectoryName(SettingsFilePath)!, "Skins"), LogSkinWarning);
        _battleSkin = _battleSkinCatalog.GetPreferred(TacticalBattleSkinPreference.Read(SettingsFilePath, LogSkinWarning));

        _screens.SetRoot(initialScreen);
        _window = null!;
        if (!createNativeWindow) return;

        var options = WindowOptions.Default with
        {
            Title = "Deep Space Saga",
            IsVisible = !_requireSmallerMonitor,
            WindowBorder = WindowBorder.Hidden,
            WindowState = WindowState.Normal,
            // Since the window is deliberately 1px shorter than the monitor (see
            // OnLoad) so Windows doesn't classify it as fullscreen, the taskbar no
            // longer auto-hides behind it on its own — start TopMost so it still
            // covers the taskbar. Toggled off whenever the window loses focus (see
            // OnFocusChanged) so it doesn't stay pinned over the Snipping Tool
            // (Print Screen) or whatever the user alt-tabs to.
            TopMost = true,
            // Present at the monitor's cadence. An independent 80 Hz limiter
            // produces uneven frame delivery on 60/120/144 Hz displays.
            // The simulation clock and snapshot cadence remain independent.
            FramesPerSecond = 0,
            VSync = true,
            // Measure the presentation wait as well as CPU drawing; automatic
            // swapping happens outside OnRender and hid the map's GPU stalls.
            ShouldSwapAutomatically = false,
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3))
        };

        try { _window = Window.Create(options); }
        catch
        {
            _battleSkinCatalog.Dispose();
            throw;
        }
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Update += _ => _uiContext.Drain();
        _window.FramebufferResize += OnFramebufferResize;
        _window.FocusChanged += OnFocusChanged;
        _window.Closing += OnClosing;

        // TEMP DIAG — startup timing investigation, remove once resolved.
        if (_startupStopwatch is not null)
            InterfaceLog.Write($"STARTUP DIAG: after Window.Create — {_startupStopwatch.ElapsedMilliseconds} ms since Main() start");
    }

    public void Run()
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_uiContext);
        try { _window.Run(); }
        finally
        {
            _closing = true;
            _ = _connectionLoader.DisposeAsync();
            _uiContext.Drain();
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private void OnLoad()
    {
        var monitors = Silk.NET.Windowing.Monitor.GetMonitors(_window).ToArray();
        InterfaceLog.Write(
            $"DIAG OnLoad monitors found: {monitors.Length} — " +
            string.Join(" | ", monitors.Select(m =>
                $"idx={m.Index} name={m.Name} origin=({m.Bounds.Origin.X},{m.Bounds.Origin.Y}) " +
                $"boundsSize={m.Bounds.Size.X}x{m.Bounds.Size.Y} videoMode={m.VideoMode.Resolution?.X}x{m.VideoMode.Resolution?.Y}")));
        int selectedMonitorIndex = GetSelectedMonitorIndex();
        var target = selectedMonitorIndex >= 0 && selectedMonitorIndex < monitors.Length
            ? monitors[selectedMonitorIndex]
            : Silk.NET.Windowing.Monitor.GetMainMonitor(_window);

        if (_requireSmallerMonitor)
        {
            static long Area(Silk.NET.Windowing.IMonitor monitor)
            {
                var size = monitor.VideoMode.Resolution ?? monitor.Bounds.Size;
                return (long)size.X * size.Y;
            }

            if (selectedMonitorIndex < 0 || selectedMonitorIndex >= monitors.Length || target is null ||
                monitors.Length < 2 || Area(target) >= monitors.Max(Area))
                throw new InvalidOperationException("Validation requires an explicitly selected monitor smaller than the largest display.");
        }

        if (target is not null)
        {
            _window.Position = target.Bounds.Origin;
            _monitorRefreshHz = target.VideoMode.RefreshRate;

            // Deliberately 1px shorter than the monitor's native resolution. A
            // borderless window whose size exactly matches the monitor is picked up
            // by Windows' "fullscreen optimizations" heuristic and treated like an
            // exclusive-fullscreen app even though this is a plain windowed OpenGL
            // context. That heuristic is what makes every monitor briefly blank and
            // renegotiate its mode at startup, and it's also why Print Screen
            // captures this window as solid black — DWM stops compositing it
            // normally and hands it a direct hardware flip path instead. Being 1px
            // short keeps the window out of that path while staying visually
            // indistinguishable from true fullscreen.
            var resolution = target.VideoMode.Resolution ?? target.Bounds.Size;
            _window.Size = new Silk.NET.Maths.Vector2D<int>(resolution.X, resolution.Y - 1);
        }

        if (_mapFrameEvidence is not null && Environment.GetEnvironmentVariable("DSS_MAP_FRAME_WINDOW") is { } requested)
        {
            var dimensions = requested.Split('x');
            if (dimensions.Length == 2 && int.TryParse(dimensions[0], out int width) && int.TryParse(dimensions[1], out int height) &&
                width >= 640 && height >= 480)
                _window.Size = new Silk.NET.Maths.Vector2D<int>(width, height);
        }

        if (_requireSmallerMonitor) _window.IsVisible = true;

        _gl = _window.CreateOpenGL();
        _graphicsRenderer = _gl.GetStringS(StringName.Renderer);
        _graphicsVersion = _gl.GetStringS(StringName.Version);

        var glInterface = GRGlInterface.Create();
        glInterface.Validate();
        _grContext = GRContext.CreateGl(glInterface, SkiaGpuOptions.Create());

        _input = _window.CreateInput();

        _mouse = _input.Mice.FirstOrDefault();
        if (_mouse is not null)
        {
            _mouse.MouseDown += OnMouseDown;
            _mouse.MouseUp += OnMouseUp;
            _mouse.MouseMove += OnMouseMove;
            _mouse.Scroll += OnMouseScroll;

            _defaultCursorImage = LoadCursorImage("Images/Cursors/cursor.png");
            _interactiveCursorImage = LoadCursorImage("Images/Cursors/cursor-selected.png");

            if (_defaultCursorImage is not null)
            {
                _mouse.Cursor.Type = CursorType.Custom;
                _mouse.Cursor.Image = _defaultCursorImage.Value;
                _mouse.Cursor.HotspotX = 0;
                _mouse.Cursor.HotspotY = 0;
            }
        }

        _keyboard = _input.Keyboards.FirstOrDefault();
        if (_keyboard is not null)
            _keyboard.KeyChar += OnKeyChar;

        // TEMP DIAG — startup timing investigation, remove once resolved.
        if (_startupStopwatch is not null)
            InterfaceLog.Write($"STARTUP DIAG: OnLoad complete — {_startupStopwatch.ElapsedMilliseconds} ms since Main() start");
    }

    /// <summary>Clean up input while native window is still valid.</summary>
    private void OnClosing()
    {
        if (_closing)
            return;

        _closing = true;
        _ = _connectionLoader.DisposeAsync();

        if (_mouse is not null)
        {
            _mouse.MouseDown -= OnMouseDown;
            _mouse.MouseUp -= OnMouseUp;
            _mouse.MouseMove -= OnMouseMove;
            _mouse.Scroll -= OnMouseScroll;
        }

        if (_keyboard is not null)
            _keyboard.KeyChar -= OnKeyChar;

        _input?.Dispose();
        _input = null;

        _ = _session?.DisposeAsync();
        _session = null;
    }

    private static RawImage? LoadCursorImage(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            using var bitmap = SKBitmap.Decode(path);
            if (bitmap is null)
                return null;

            using var resized = bitmap.Resize(new SKSizeI(26, 26), SKFilterQuality.High);
            if (resized is null)
                return null;

            var pixels = new byte[resized.Width * resized.Height * 4];
            var ptr = resized.GetPixels();
            System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);

            for (int i = 0; i < pixels.Length; i += 4)
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);

            return new RawImage(resized.Width, resized.Height, pixels);
        }
        catch
        {
            return null;
        }
    }

    private void OnRender(double deltaTime)
    {
        if (_grContext is null || _gl is null || _closing)
            return;

        long callbackStart = Stopwatch.GetTimestamp();
        double callbackIntervalMs = _previousProfileRenderTimestamp == 0 ? 0
            : Stopwatch.GetElapsedTime(_previousProfileRenderTimestamp, callbackStart).TotalMilliseconds;
        _previousProfileRenderTimestamp = callbackStart;

        // TEMP DIAG — startup timing investigation, remove once resolved.
        bool isFirstFrame = !_firstFrameLogged;
        var diagSw = isFirstFrame ? System.Diagnostics.Stopwatch.StartNew() : null;
        if (isFirstFrame && _startupStopwatch is not null)
            InterfaceLog.Write($"STARTUP DIAG: OnRender first entered — {_startupStopwatch.ElapsedMilliseconds} ms since Main() start");

        if (_surface is null)
        {
            CreateRenderSurface();
            if (_surface is null)
                return;
        }

        if (isFirstFrame)
            InterfaceLog.Write($"STARTUP DIAG: first-frame CreateRenderSurface done — {diagSw!.ElapsedMilliseconds} ms into OnRender");

        var canvas = _surface.Canvas;

        var windowSize = _window.Size;
        var fbSize = _window.FramebufferSize;

        if (windowSize.X <= 0 || windowSize.Y <= 0)
            return;

        float scaleX = (float)fbSize.X / windowSize.X;
        float scaleY = (float)fbSize.Y / windowSize.Y;

        RequestStationCleanup();
        canvas.Save();
        canvas.Scale(scaleX, scaleY);

        int index = RenderScreenStack(canvas, windowSize.X, windowSize.Y, out var profiledScreen);

        if (isFirstFrame)
            InterfaceLog.Write($"STARTUP DIAG: first-frame screen.Render (recording) done — {diagSw!.ElapsedMilliseconds} ms into OnRender");

        canvas.Restore();
        long flushStart = Stopwatch.GetTimestamp();
        canvas.Flush();
        long flushEnd = Stopwatch.GetTimestamp();
        NativeFrameMeasured?.Invoke(_screens.Current, Stopwatch.GetElapsedTime(callbackStart, flushEnd).TotalMilliseconds);
        NativeFrameRendered?.Invoke(_surface, _screens.Current);

        if (isFirstFrame)
            InterfaceLog.Write($"STARTUP DIAG: first-frame canvas.Flush done — {diagSw!.ElapsedMilliseconds} ms into OnRender");

        // TEMP DIAG — startup timing investigation, remove once resolved.
        if (!_firstFrameLogged)
        {
            _firstFrameLogged = true;
            if (_startupStopwatch is not null)
                InterfaceLog.Write($"STARTUP DIAG: first frame flushed — {_startupStopwatch.ElapsedMilliseconds} ms since Main() start");
        }

        PollKeyboard();
        PollGameSessionAutoTransition();
        if (_mapFrameEvidence is { FrameCount: 119 } && Environment.GetEnvironmentVariable("DSS_MAP_FRAME_IMAGE") is { } imagePath)
        {
            using var image = _surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(imagePath, encoded.ToArray());
        }
        long presentStart = Stopwatch.GetTimestamp();
        _window.SwapBuffers();
        double presentWaitMs = Stopwatch.GetElapsedTime(presentStart).TotalMilliseconds;
        NativeFramePresented?.Invoke(_screens.Current, Stopwatch.GetTimestamp(),
            Stopwatch.GetElapsedTime(callbackStart, presentStart).TotalMilliseconds, presentWaitMs,
            Stopwatch.GetElapsedTime(flushStart, flushEnd).TotalMilliseconds);
        if (_mapFrameEvidence is { Completed: false } evidence && profiledScreen is not null)
        {
            var snapshot = profiledScreen.FrameEvidenceSnapshot;
            var map = snapshot?.SolarSystemMap;
            evidence.Record(Stopwatch.GetTimestamp(), Stopwatch.GetElapsedTime(callbackStart, presentStart).TotalMilliseconds,
                new(fbSize.X, fbSize.Y, _window.VSync, _monitorRefreshHz, _graphicsRenderer, _graphicsVersion,
                    profiledScreen.UiScale, snapshot?.CurrentSpeed.ToString() ?? "unavailable", map?.Seed, map?.GeneratorVersion,
                    map?.Planets.Length ?? 0, map?.Belts.Length ?? 0, snapshot?.Objects.Length ?? 0,
                    profiledScreen.CameraPixelsPerWorldUnit, profiledScreen.SelectedObjectId,
                    MapFrameCounts.From(snapshot), profiledScreen.MapLayers.ToString(), snapshot?.GameTimeMs, snapshot?.MotionTimeMs,
                    profiledScreen.SelectedFieldId, profiledScreen.SelectedPoiId), presentWaitMs);
            if (evidence.Completed && Environment.GetEnvironmentVariable("DSS_MAP_FRAME_EXIT") == "1") _window.Close();
        }
        _grContext.GetResourceCacheUsage(out int gpuResources, out long gpuCacheBytes);
        // No Finish/readback: this is the existing swap, now timed explicitly.
        profiledScreen?.CompleteWindowProfile(new(callbackIntervalMs,
            Stopwatch.GetElapsedTime(callbackStart, flushStart).TotalMilliseconds,
            Stopwatch.GetElapsedTime(flushStart, flushEnd).TotalMilliseconds,
            Stopwatch.GetElapsedTime(callbackStart, presentStart).TotalMilliseconds,
            _isFocused, _window.VSync, index, fbSize.X, fbSize.Y,
            presentWaitMs, _graphicsRenderer, _graphicsVersion,
            gpuResources, gpuCacheBytes, _grContext.GetResourceCacheLimit()));
    }

    /// <summary>Draws the visible stack, the connection-lost status and, above everything, the open debug console.</summary>
    /// <returns>The number of drawn screens.</returns>
    internal int RenderScreenStack(SKCanvas canvas, int width, int height, out GameSessionScreen? profiledScreen)
    {
        // Overlay: render the whole stack bottom-to-top so every overlay panel draws
        // on top of the interactive root screen instead of a blank canvas. The dim
        // rect itself is drawn exactly once, right before the first (bottom-most)
        // overlay — individual screens no longer draw their own, so stacking overlays
        // (e.g. Trade opened from Station) look exactly as dim as a single overlay
        // instead of compounding darker with each nested level.
        int index = 0;
        profiledScreen = null;
        foreach (var screen in _screens.VisibleBottomToTop())
        {
            // A transition can be waiting for an authoritative speed acknowledgement.
            // Do not paint an obsolete market while its serialized removal is pending.
            if (screen is TradeScreen trade && (!ReferenceEquals(trade.OpenedForHandle, _session) || !trade.HasValidVisit) ||
                screen is StationScreen station && (!ReferenceEquals(station.OpenedForHandle, _session) || !station.HasValidVisit))
                continue;
            if (index == 1)
                MenuStyle.DrawDimOverlay(canvas, width, height);

            if (screen is TacticalBattleScreen battle) battle.RenderNative(canvas, width, height, _grContext);
            else screen.Render(canvas, width, height);
            if (screen is GameSessionScreen sessionScreen) profiledScreen = sessionScreen;
            index++;
        }
        if (_session?.Failure is not null)
            canvas.DrawText(Localization.Get("Session.ConnectionLost"), width / 2f,
                height - 20, MenuStyle.TextStatus);

        _debugConsoleOverlay.Render(canvas, DebugConsole, width, height, _debugConsoleUiScale);
        if (_session is { IsPlayerShipDestroyed: false } && _session.Buffer.Latest?.Snapshot.IsTacticalBattleActive != true && _session.AutosaveStatus is { Length: > 0 } saveStatus)
            canvas.DrawText(Localization.Get("Demo.Save." + saveStatus), width / 2f, height - 48, MenuStyle.TextStatus);
        return index;
    }

    /// <summary>
    /// GameSessionScreen has no way to return a ScreenEvent outside a direct input
    /// handler, but a successful navigation.dock is an authoritative outcome the client
    /// only learns about from the next snapshot (Docking.md: the Station screen opens
    /// automatically after a successful Dock, not only when the player clicks the
    /// already-docked station again). Polled once per frame, only while GameSessionScreen
    /// is actually the foreground screen — never while a modal (GameMenu, Station itself,
    /// ...) is already on top of it, matching every other Open*Async guard here.
    /// </summary>
    private void PollGameSessionAutoTransition()
    {
        if (_session?.IsPlayerShipDestroyed == true)
        {
            // The battle modal owns the screen until its authoritative exit/settlement/checkpoint
            // completes; replacing it here would hide a pending retry and lose the result.
            if (_screens.Current is TacticalBattleModalScreen { IsSetup: false }) return;
            if (!ReferenceEquals(_shipDestroyedSession, _session) && (_shipDestroyedTransitionTask?.IsCompleted ?? true))
                _shipDestroyedTransitionTask = HandleScreenEvent(ScreenEvent.None);
            return;
        }
        if (_screens.Current is TacticalBattleModalScreen) return;
        RequestStationCleanup();
        if (_screens.Current is StationScreen && _pendingUndockCommandId is { } pendingId &&
            _session?.Buffer.Latest?.Snapshot is { } stationSnapshot)
        {
            var outcome = stationSnapshot.CommandResults.LastOrDefault(result => result.CommandId == pendingId);
            if (outcome is { Status: CommandResultStatus.Executed })
            {
                _pendingUndockCommandId = null;
                RequestStationCleanup();
            }
            else if (outcome is { Status: CommandResultStatus.Rejected })
                _pendingUndockCommandId = null;
            return;
        }
        if (_screens.Current is DialogueScreen dialogue)
        {
            var transition = dialogue.Poll();
            if (transition != ScreenEvent.None) _ = HandleScreenEvent(transition);
            return;
        }
        if (_screens.Current is not GameSessionScreen gameSessionScreen)
            return;

        var evt = gameSessionScreen.ConsumePendingAutoTransition();
        if (evt != ScreenEvent.None)
            _ = HandleScreenEvent(evt);
    }

    private bool StationOverlayIsInvalid() => _screens.Current switch
    {
        TradeScreen trade => !ReferenceEquals(trade.OpenedForHandle, _session) || !trade.HasValidVisit,
        StationScreen station => !ReferenceEquals(station.OpenedForHandle, _session) || !station.HasValidVisit,
        _ => false
    };

    private void RequestStationCleanup()
    {
        if (_closing || !StationOverlayIsInvalid() || _stationCleanupTask is { IsCompleted: false })
            return;
        _stationCleanupTask = CleanStationOverlaysAsync();
    }

    private async Task CleanStationOverlaysAsync()
    {
        await _transitionLock.WaitAsync();
        try
        {
            await _stationNavigation.RemoveInvalidOverlaysAsync(_screens,
                () => _session?.Buffer.Latest?.Snapshot, PopModalAsync);
            if (_screens.Current is not StationScreen)
                _pendingUndockCommandId = null;
        }
        catch (Exception error)
        {
            InterfaceLog.Write($"Station overlay cleanup failed: {error}");
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    private void OnFramebufferResize(Silk.NET.Maths.Vector2D<int> newSize)
    {
        if (_surface is not null)
            CreateRenderSurface();
    }

    /// <summary>
    /// While an OS overlay owns focus (e.g. Windows' Print Screen key opening the
    /// Snipping Tool over this borderless window), the OS is still mid-transition
    /// delivering/withholding key messages — polling during that window can read
    /// garbage and misfire an edge (a phantom Escape) before focus even returns.
    /// PollKeyboard is skipped entirely while unfocused, and state is re-baselined
    /// the moment focus comes back — see
    /// <see cref="KeyboardEdgeTracker.ResyncToCurrentState"/>.
    /// </summary>
    private void OnFocusChanged(bool isFocused)
    {
        bool? printScreenDown = _keyboard?.IsKeyPressed(Key.PrintScreen);
        InterfaceLog.Write(
            $"DIAG FocusChanged isFocused={isFocused} screen={_screens.Current.GetType().Name} " +
            $"depth={_screens.Count} printScreenDown={printScreenDown}");

        bool focusChanged = _isFocused != isFocused;
        _isFocused = isFocused;

        // Only cover the taskbar (see WindowOptions.TopMost above) while the game
        // itself is the foreground window. Keeping TopMost on while unfocused would
        // pin the game over whatever the user switched to — e.g. the Snipping Tool
        // that Print Screen just opened, or an alt-tabbed app.
        if (_window is not null)
            _window.TopMost = isFocused;

        if (isFocused)
        {
            _focusRegainedAtMs = Environment.TickCount64;
            _keyboardEdges.ResyncToCurrentState(_keyboard, _screens.Current is TacticalBattleModalScreen);
        }
        else
        {
            _keyboardEdges.ResetRepeats();
            _screens.Current.OnInputReset();
        }
        // Polling stops while unfocused and resync intentionally discards missed key releases.
        // Release server-side held controls now, before that release edge can be lost.
        if (focusChanged && _screens.Count > 0 && _screens.Current is GameSessionScreen game)
        {
            if (isFocused) game.OnActivated();
            else game.OnDeactivated();
        }
    }

    private void CreateRenderSurface()
    {
        _surface?.Dispose();
        _surface = null;
        _renderTarget?.Dispose();
        _renderTarget = null;

        if (_grContext is null || _gl is null)
            return;

        var size = _window.FramebufferSize;
        if (size.X <= 0 || size.Y <= 0)
            return;

        var framebufferInfo = new GRGlFramebufferInfo(0, SKColorType.Rgba8888.ToGlSizedFormat());
        _renderTarget = new GRBackendRenderTarget(size.X, size.Y, 0, 8, framebufferInfo);
        _surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
    }

    private async void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (_closing || (button != MouseButton.Left && button != MouseButton.Right && button != MouseButton.Middle))
            return;
        RequestStationCleanup();
        if (StationOverlayIsInvalid() || _stationCleanupTask is { IsCompleted: false })
            return;
        if (DebugConsole.IsOpen)
        {
            _debugConsoleOverlay.OnMouseDown(DebugConsole, mouse.Position.X, mouse.Position.Y, button);
            return;
        }

        // Capture the current screen and (if relevant) its click payload synchronously,
        // in this same call frame, before HandleScreenEvent's first await. This is what
        // makes ScreenEvent.LoadSlotRequested's slot id race-safe: even if a rapid
        // double-click queues two of these calls back-to-back, each one's payload is
        // read here — while _screens.Current is still guaranteed to be the LoadScreen
        // that produced it — never re-derived later from (possibly stale) screen state
        // inside the locked HandleScreenEvent switch. See LoadScreen's doc comment.
        var currentScreen = _screens.Current;
        if (currentScreen is GameSessionScreen game && _keyboard is not null)
            game.SynchronizeModifiers(_keyboard.IsKeyPressed(Key.ControlLeft), _keyboard.IsKeyPressed(Key.ControlRight));
        var screenEvent = currentScreen.OnMouseDown(mouse.Position.X, mouse.Position.Y, button);
        string? payload = screenEvent == ScreenEvent.LoadSlotRequested && currentScreen is LoadScreen loadScreen
            ? loadScreen.LastRequestedSlotId
            : screenEvent == ScreenEvent.ScenarioSelected && currentScreen is ScenarioSelectScreen scenarioSelectScreen
                ? scenarioSelectScreen.LastSelectedScenarioPath
                : null;

        await HandleScreenEvent(screenEvent, payload);
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        RequestStationCleanup();
        if (StationOverlayIsInvalid() || _stationCleanupTask is { IsCompleted: false }) return;
        if (DebugConsole.IsOpen)
        {
            _debugConsoleOverlay.OnMouseUp(DebugConsole, button);
            return;
        }

        if (!_closing && button is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
            _screens.Current.OnMouseUp(mouse.Position.X, mouse.Position.Y, button);
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        if (_closing)
            return;
        RequestStationCleanup();
        if (StationOverlayIsInvalid() || _stationCleanupTask is { IsCompleted: false }) return;

        bool overInteractive;
        if (DebugConsole.IsOpen)
        {
            _debugConsoleOverlay.OnMouseMove(DebugConsole, position.X, position.Y);
            overInteractive = false;
        }
        else overInteractive = _screens.Current.OnMouseMove(position.X, position.Y);

        var targetImage = overInteractive ? _interactiveCursorImage : _defaultCursorImage;
        if (targetImage is not null)
        {
            mouse.Cursor.Type = CursorType.Custom;
            mouse.Cursor.Image = targetImage.Value;
        }
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel scroll)
    {
        if (_closing)
            return;
        RequestStationCleanup();
        if (StationOverlayIsInvalid() || _stationCleanupTask is { IsCompleted: false }) return;
        if (DebugConsole.IsOpen)
        {
            _debugConsoleOverlay.OnMouseWheel(DebugConsole, mouse.Position.X, mouse.Position.Y, MouseWheelInput.NormalizeSilk(scroll.Y));
            return;
        }

        var screenEvent = _screens.Current.OnMouseWheel(
            mouse.Position.X,
            mouse.Position.Y,
            _screens.Current.UsesBrowserWheelDelta ? MouseWheelInput.NormalizeSilk(scroll.Y) : scroll.Y);

        _ = HandleScreenEvent(screenEvent);
    }

    private void OnKeyChar(IKeyboard keyboard, char c)
    {
        if (_closing)
            return;
        RequestStationCleanup();
        if (StationOverlayIsInvalid() || _stationCleanupTask is { IsCompleted: false }) return;

        // The toggle chord itself may produce a character (AltGr layouts); it must reach neither the console nor the screen.
        var modifiers = _keyboard is null ? _keyboardEdges.Modifiers : KeyboardEdgeTracker.ReadModifiers(_keyboard.IsKeyPressed);
        bool chordHeld = modifiers.HasFlag(KeyModifiers.Control) && modifiers.HasFlag(KeyModifiers.Alt);
        if (DebugConsole.IsOpen)
        {
            if (!chordHeld) DebugConsole.OnTextInput(c);
            return;
        }
        if (chordHeld && _keyboard?.IsKeyPressed(Key.P) == true) return;

        _screens.Current.OnTextInput(c);
    }

    private Task? _pendingKeyboardTransition;

    private void PollKeyboard()
    {
        // A command result may have closed the console since the last frame.
        if (!_closing) SyncDebugConsoleState();
        if (_keyboard is null || _closing || !_isFocused)
            return;
        RequestStationCleanup();
        if (StationOverlayIsInvalid() || _stationCleanupTask is { IsCompleted: false }) return;

        if (!ReferenceEquals(_repeatScreen, _screens.Current))
        {
            _keyboardEdges.ResetRepeats();
            _repeatScreen = _screens.Current;
        }
        var (pressedCount, releasedCount) = _keyboardEdges.PollBoth(
            _keyboard, _keyboardPressedKeys, _keyboardReleasedKeys, _screens.Current is TacticalBattleModalScreen);

        if (pressedCount > 0 || releasedCount > 0)
        {
            InterfaceLog.Write(
                $"DIAG PollKeyboard pressed=[{string.Join(',', _keyboardPressedKeys[..pressedCount])}] " +
                $"released=[{string.Join(',', _keyboardReleasedKeys[..releasedCount])}] " +
                $"screen={_screens.Current.GetType().Name} depth={_screens.Count} " +
                $"msSinceFocusRegain={Environment.TickCount64 - _focusRegainedAtMs} " +
                $"printScreenDown={_keyboard.IsKeyPressed(Key.PrintScreen)}");
        }

        bool consoleOwnedKeys = DebugConsole.IsOpen;
        if (_screens.Current is GameSessionScreen game && !consoleOwnedKeys)
        {
            game.SynchronizeModifiers(_keyboard.IsKeyPressed(Key.ControlLeft), _keyboard.IsKeyPressed(Key.ControlRight));
        }
        for (int i = 0; i < pressedCount; i++)
        {
            // Closing re-baselines held keys; the rest of this frame's edges must not leak to the screen.
            if (consoleOwnedKeys && !DebugConsole.IsOpen) break;
            _handleKeyboardEdge(_keyboardPressedKeys[i]);
        }

        // While the console owns the keyboard the screen saw an input reset; releases stay with the console.
        if (!consoleOwnedKeys && !DebugConsole.IsOpen)
            for (int i = 0; i < releasedCount; i++)
                _screens.Current.OnKeyUp(_keyboardReleasedKeys[i]);
        if (_screens.Current.WantsArrowRepeat && ReferenceEquals(_repeatScreen, _screens.Current))
        {
            int count = _keyboardEdges.PollRepeats(Environment.TickCount64, _keyboardRepeatedKeys);
            for (int i = 0; i < count; i++) HandleKeyboardEvent(_keyboardRepeatedKeys[i], repeat: true);
        }
    }

    private void HandleKeyboardEdge(Key key) => HandleKeyboardEvent(key, repeat: false);
    private void HandleKeyboardEvent(Key key, bool repeat)
    {
        // EP-0030: Ctrl+Alt+P toggles the debug console over any screen; while open it consumes every key except F10.
        if (DebugConsoleController.IsToggleChord(key, _keyboardEdges.Modifiers))
        {
            if (!repeat)
            {
                DebugConsole.Toggle();
                SyncDebugConsoleState();
            }
            return;
        }
        if (DebugConsole.IsOpen)
        {
            if (key == Key.F10) CaptureScreenshot();
            else DebugConsole.OnKeyDown(key, _keyboardEdges.Modifiers, repeat);
            SyncDebugConsoleState();
            return;
        }

        if (key == Key.F6 && !repeat && _session is { } autosaveSession)
        {
            _ = autosaveSession.RetryAutosaveAsync();
            return;
        }
        if (key == Key.F10)
        {
            CaptureScreenshot();
            return;
        }

        if (_screens.Current is not TacticalBattleModalScreen && key is Key.Escape or Key.F5 or Key.F9 or Key.F or Key.S)
        {
            if (_pendingKeyboardTransition is null || _pendingKeyboardTransition.IsCompleted)
            {
                var screenEvent = _screens.Current.OnKeyDown(key, _keyboardEdges.Modifiers, repeat);
                _pendingKeyboardTransition = HandleScreenEvent(screenEvent);
            }

            return;
        }

        var action = _screens.Current.OnKeyDown(key, _keyboardEdges.Modifiers, repeat);
        if (action != ScreenEvent.None) _pendingKeyboardTransition = HandleScreenEvent(action);
    }

    /// <summary>
    /// Applies a console open/close (including one requested by a command result) to the screen beneath:
    /// opening mirrors focus loss (no held input, no repeats), closing re-baselines held keys like focus regain.
    /// Speed and pause are never touched.
    /// </summary>
    private void SyncDebugConsoleState()
    {
        bool open = DebugConsole.IsOpen;
        if (open == _debugConsoleShown) return;
        _debugConsoleShown = open;
        if (open)
        {
            _debugConsoleUiScale = (float)GetUiScale();
            _keyboardEdges.ResetRepeats();
            _screens.Current.OnInputReset();
            if (_screens.Current is GameSessionScreen game) game.OnDeactivated();
            return;
        }
        DebugConsoleOverlay.ResetInput(DebugConsole);
        if (!_isFocused) return;
        _keyboardEdges.ResyncToCurrentState(_keyboard, _screens.Current is TacticalBattleModalScreen);
        if (_screens.Current is GameSessionScreen activeGame) activeGame.OnActivated();
    }

    GameSessionHandle? IDebugConsoleHost.Session => _session;

    Task<DebugCommandResult> IDebugConsoleHost.ConfigureFogOfWarAsync(bool? enabled, CancellationToken cancellationToken) =>
        ((IDebugConsoleHost)this).RunExclusiveAsync(token => AppDebugOverrides.FogAsync(enabled,
            value => _sessionFactory.ConfigureFogOfWar(value), _session?.DemoCampaign?.Revision,
            _session is null ? null : (command, ct) => _session.ExecuteDemoCampaignAsync(command, ct).AsTask(), token), cancellationToken);

    Task<DebugCommandResult> IDebugConsoleHost.ConfigurePirateSquadChanceAsync(int? percent, CancellationToken cancellationToken) =>
        ((IDebugConsoleHost)this).RunExclusiveAsync(_ =>
        {
            try
            {
                var settings = _sessionFactory.ConfigurePirateSquadChance(percent);
                return Task.FromResult(DebugCommandResult.Ok($"pirate_squad_chance = {settings.PirateSquadChancePercent}% — для следующей новой кампании"));
            }
            catch (NotSupportedException) { return Task.FromResult(DebugCommandResult.Error("Настройка демо недоступна")); }
        }, cancellationToken);

    bool IDebugConsoleHost.IsTransitionBusy => _transitionLock.CurrentCount == 0;

    TacticalBattleScreen? IDebugConsoleHost.ActiveBattle => _screens.Current as TacticalBattleScreen;

    async Task<DebugCommandResult> IDebugConsoleHost.RunExclusiveAsync(
        Func<CancellationToken, Task<DebugCommandResult>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        // Never wait: a transition in progress answers "busy" instead of queueing behind it.
        if (_closing || !await _transitionLock.WaitAsync(0, cancellationToken)) return DebugCommandResult.Busy();
        try { return await operation(cancellationToken); }
        finally { _transitionLock.Release(); }
    }

    /// <summary>
    /// Debug-only exception to the EP-0027 "tutorial exits to the main menu" rule (EP-0030 D-03): the battle is
    /// exited through the same flow/settlement/checkpoint path as <see cref="ScreenEvent.CloseTacticalBattle"/>, but
    /// an animation in progress or an open exit confirmation does not block it. A destroyed carrier still routes to
    /// the ship-destroyed screen (G-01).
    /// </summary>
    async Task<DebugCommandResult> IDebugConsoleHost.ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken cancellationToken)
    {
        if (!await _transitionLock.WaitAsync(0, cancellationToken)) return DebugCommandResult.Busy();
        try
        {
            if (_closing || _session is null || !ReferenceEquals(expectedSession, _session) ||
                _screens.Current is not TacticalBattleScreen { IsSetup: false, Flow.IsTutorial: true } battle)
                return DebugCommandResult.Error(ExitBattleDebugCommand.NoTutorialBattleText);
            // Only an in-flight battle request blocks the abort; TurnBusy (animation) deliberately does not.
            if (battle.Flow.Pending) return DebugCommandResult.Busy();

            bool unfinished = battle.Flow.State is { Outcome: null };
            // A failed exit/checkpoint keeps the battle open; the next exit_battle retries without re-settling.
            if (!await battle.Flow.ExitAsync())
                return DebugCommandResult.Error("Не удалось завершить бой: " + battle.Flow.Error + ". Повторите exit_battle");
            if (unfinished) battle.Tutorial?.RecordAbandoned();
            await PopModalAsync();
            bool destroyed = EnsureShipDestroyedScreen();
            return new DebugCommandResult(
                [destroyed ? "Бой прерван. Носитель уничтожен — экран гибели" : "Бой завершён, открыта карта"], CloseConsole: true);
        }
        finally { _transitionLock.Release(); }
    }

    private const string ScreenshotsDirectory = "Screenshots";

    /// <summary>
    /// F10 — screenshot. Captures whatever is currently in the flushed render
    /// surface (the frame this same OnRender call just drew) and saves it as a
    /// uniquely-named PNG under <see cref="ScreenshotsDirectory"/> next to
    /// <see cref="InterfaceLog"/>. No modal/UI window is shown; the action (and any
    /// failure) is recorded via <see cref="InterfaceLog"/> instead.
    /// </summary>
    private void CaptureScreenshot()
    {
        if (_surface is null)
            return;

        try
        {
            string directory = Path.Combine(Path.GetDirectoryName(InterfaceLog.FileName) ?? "", ScreenshotsDirectory);
            Directory.CreateDirectory(directory);

            string fileName = $"screenshot_{DateTime.UtcNow:yyyy-MM-dd'T'HH-mm-ss-fff'Z'}.png";
            string path = Path.Combine(directory, fileName);

            using var image = _surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(path);
            data.SaveTo(stream);

            InterfaceLog.Write($"Screenshot saved: {path}");
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"Screenshot failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Serialized navigation transition. Only one screen transition
    /// (modal open/close, new game, main menu) can be in flight at a time.
    /// This prevents races where e.g. MAIN MENU is clicked while a Pause
    /// request is still awaiting confirmation.
    /// </summary>
    /// <param name="payload">
    /// Event-specific data captured synchronously by the caller at dispatch time (e.g.
    /// the target slot id for <see cref="ScreenEvent.LoadSlotRequested"/>). Deliberately
    /// threaded through as a parameter rather than read back from screen state once this
    /// method has acquired <see cref="_transitionLock"/> — see <see cref="OnMouseDown(IMouse, MouseButton)"/>.
    /// </param>
    private async Task HandleScreenEvent(ScreenEvent evt, string? payload = null)
    {
        if (evt != ScreenEvent.None)
        {
            InterfaceLog.Write(
                $"DIAG HandleScreenEvent evt={evt} screen={_screens.Current.GetType().Name} depth={_screens.Count}");
        }

        // The screen that produced this event, captured at dispatch time (before waiting for the lock).
        var origin = _screens.Current;
        if (evt == ScreenEvent.Exit && origin is MainMenuScreen && !_closing)
        {
            // Main-menu Exit never waits behind an in-flight transition (e.g. a tutorial start holding the lock
            // across several awaits) — otherwise it would run only after the battle opened and be blocked there.
            _exitRequested = true;
            _window?.Close();
            return;
        }

        await _transitionLock.WaitAsync();
        try
        {
            if (_closing) return;
            if (evt != ScreenEvent.None && origin is MainMenuScreen && !ReferenceEquals(_screens.Current, origin))
            {
                // A main-menu click queued while a transition (e.g. the tutorial start) replaced the menu is stale:
                // drop it instead of letting the new screen (the tutorial battle's gate) reject it with a notice.
                InterfaceLog.Write($"HandleScreenEvent evt={evt} ignored: issued by a main menu that is no longer shown");
                return;
            }
            if (EnsureShipDestroyedScreen() && evt is not (ScreenEvent.None or ScreenEvent.MainMenu or ScreenEvent.Exit or
                ScreenEvent.OpenLoadWindow or ScreenEvent.CloseLoadWindow or ScreenEvent.LoadSlotRequested or ScreenEvent.QuickLoad or ScreenEvent.NewGame or ScreenEvent.ContinueCheckpoint)) return;
            if (_session?.DemoCampaign?.WorldEncounter is { Phase: not WorldEncounterPhases.Finished } && evt is
                (ScreenEvent.MainMenu or ScreenEvent.NewGame or ScreenEvent.OpenLoadWindow or ScreenEvent.LoadSlotRequested or ScreenEvent.QuickLoad or ScreenEvent.ContinueCheckpoint)) return;
            if (_screens.Current is BattleResultsScreen &&
                evt is not (ScreenEvent.None or ScreenEvent.AcknowledgeBattleResults or ScreenEvent.QuickSave or ScreenEvent.QuickLoad)) return;
            if (_screens.Current is TacticalBattleModalScreen activeBattle && evt is not
                (ScreenEvent.None or ScreenEvent.StartTacticalBattle or ScreenEvent.MoveTacticalBattle or ScreenEvent.FireTacticalBattle or
                ScreenEvent.NewTacticalBattle or ScreenEvent.CloseTacticalBattle or ScreenEvent.OpenBattleResults or ScreenEvent.AcknowledgeBattleResults) &&
                !(evt == ScreenEvent.MainMenu && activeBattle.Flow.IsTutorial) &&
                !(activeBattle is (TacticalBattleScreen { CanUseLootSave: true } or BattleResultsScreen { CanSaveLoad: true }) &&
                    evt is ScreenEvent.QuickSave or ScreenEvent.QuickLoad))
            {
                activeBattle.NotifyWorldActionBlocked();
                return;
            }
            await _stationNavigation.RemoveInvalidOverlaysAsync(_screens,
                () => _session?.Buffer.Latest?.Snapshot, PopModalAsync);
            switch (evt)
            {
                case ScreenEvent.OpenTacticalBattle:
                    if (_session is { } battleSession && _screens.Count == 1 && _screens.Current is GameSessionScreen)
                    {
                        Localization.ApplyLanguage(Localization.ReadLanguageSetting(SettingsFilePath));
                        var captured = battleSession.DemoCampaign?.WorldEncounter;
                        bool openLoot = battleSession.DemoCampaign?.Rewards?.Loot is { Phase: LootPhases.Open };
                        var flow = new TacticalBattleFlow(battleSession)
                        {
                            RequireBattleResults = true,
                            CampaignEncounter = captured is { Phase: not WorldEncounterPhases.Finished } || openLoot ? captured : null
                        };
                        if ((captured is { Phase: WorldEncounterPhases.PendingBattle } || openLoot) && await flow.StartEncounterAsync())
                        {
                            await PushModalAsync(CreateTacticalBattleView(flow), () => ReferenceEquals(_session, battleSession));
                            break;
                        }
                        var setup = CreateTacticalSetupView(flow);
                        await PushModalAsync(setup, () => ReferenceEquals(_session, battleSession));
                        if (ReferenceEquals(_screens.Current, setup))
                        {
                            flow.BeginLoadOptions();
                            _ = LogTacticalUnavailableAsync(flow);
                        }
                    }
                    break;
                case ScreenEvent.StartTacticalBattle:
                    if (_screens.Current is TacticalBattleModalScreen { CanStart: true } preparation)
                    {
                        bool started = preparation.Flow.IsCampaign
                            ? await preparation.Flow.StartEncounterAsync()
                            : (await preparation.Flow.StartAsync(preparation.BuildStartRequest()))?.Accepted == true;
                        if ((started || preparation.Flow.State is not null) && ReferenceEquals(_screens.Current, preparation))
                            _screens.Replace(CreateTacticalBattleView(preparation.Flow));
                    }
                    break;
                case ScreenEvent.MoveTacticalBattle:
                case ScreenEvent.FireTacticalBattle:
                    if (_screens.Current is TacticalBattleModalScreen { IsSetup: false } battle && !battle.Flow.Pending)
                    {
                        if (battle is TacticalBattleScreen view && !view.CanSubmitTurn(evt)) break;
                        var before = battle.Flow.State;
                        var response = evt == ScreenEvent.MoveTacticalBattle
                            ? await battle.Flow.MoveAsync(battle.BuildMoveRequest())
                            : await battle.Flow.FireAsync(battle.BuildFireRequest());
                        if (response is not null && ReferenceEquals(_screens.Current, battle)) battle.OnBattleUpdated(before, response);
                    }
                    break;
                case ScreenEvent.OpenBattleResults:
                    if (ReferenceEquals(origin, _screens.Current) && _screens.Current is TacticalBattleScreen { CanOpenBattleResults: true } ended &&
                        await ended.Flow.OpenResultsAsync())
                        _screens.Replace(new BattleResultsScreen(ended.Flow, ended.UiScale));
                    break;
                case ScreenEvent.AcknowledgeBattleResults:
                    if (ReferenceEquals(origin, _screens.Current) && _screens.Current is BattleResultsScreen results &&
                        results.ConsumeAcknowledgeRequest() && await results.Flow.AcknowledgeResultsAsync(results.ConfirmDiscard))
                    {
                        await PopModalAsync();
                        EnsureShipDestroyedScreen();
                    }
                    break;
                case ScreenEvent.NewTacticalBattle:
                case ScreenEvent.CloseTacticalBattle:
                    if (_screens.Current is TacticalBattleModalScreen modal && !modal.Flow.Pending)
                    {
                        if (modal is TacticalBattleScreen { TurnBusy: true }) break;
                        if (evt == ScreenEvent.NewTacticalBattle && modal is not TacticalBattleScreen { CanStartNewBattle: true }) break;
                        if (!await modal.Flow.ExitAsync()) break;
                        if (evt == ScreenEvent.NewTacticalBattle) _screens.Replace(CreateTacticalSetupView(modal.Flow));
                        else await PopModalAsync();
                        // HP0 routes to the terminal screen; only drones lost keeps the live map.
                        // Ensure runs after the battle screen is gone, so a destroyed ship can never
                        // be resurrected by a repeat New/Close event or an animation tick.
                        EnsureShipDestroyedScreen();
                    }
                    break;
                case ScreenEvent.NewGame:
                    if (_session?.IsPlayerShipDestroyed == true) await ReturnToMainMenuAsync();
                    if (_session is null && (_sessionFactory.IsDemoProfile || !HasAnySaveSlotForNewGame()))
                        await StartTutorialAsync();
                    else
                        OpenScenarioSelect();
                    break;
                case ScreenEvent.ContinueCheckpoint:
                    if (!await LoadSlotCoreAsync(DeepSpaceSaga.Engine.LocalClient.SaveSlotNaming.ContinuationSlotId, "Checkpoint") && _screens.Current is ShipDestroyedScreen terminal)
                        terminal.ShowStatus("Demo.Save.LoadFailed");
                    break;
                case ScreenEvent.ContinueCampaign:
                    try
                    {
                        var slot = await Task.Run(_sessionFactory.GetLatestValidSave);
                        if (slot is not null)
                        {
                            if (!await LoadSlotCoreAsync(slot.SlotId, "Continue") && _screens.Current is MainMenuScreen failedMenu)
                                failedMenu.ShowStatus("Demo.Save.LoadFailed");
                        }
                        else if (_screens.Current is MainMenuScreen menu) menu.ShowStatus("Demo.Save.Empty");
                    }
                    catch (Exception error)
                    {
                        InterfaceLog.Write("Continue failed: " + error.Message);
                        if (_screens.Current is MainMenuScreen menu) menu.ShowStatus("Demo.Save.LoadFailed");
                    }
                    break;
                case ScreenEvent.ScenarioSelected:
                    if (payload is not null)
                        await StartGameSessionAsync(payload);
                    break;
                case ScreenEvent.OpenGameMenu:
                    await OpenGameMenuAsync();
                    break;
                case ScreenEvent.Resume:
                    await CloseOverlayAsync();
                    break;
                case ScreenEvent.MainMenu:
                    // A confirmed mid-tutorial abort must write the real losses/checkpoint before the
                    // session is disposed. A failed write keeps the battle modal and its Retry visible.
                    if (_screens.Current is TacticalBattleModalScreen { IsSetup: false } abortingBattle)
                    {
                        bool unfinished = abortingBattle.Flow.State is { Outcome: null };
                        if (!await abortingBattle.Flow.ExitAsync()) break;
                        // The exit wrote the battle report as "exited"; the tutorial journal marks the abandonment too.
                        if (unfinished && abortingBattle is TacticalBattleScreen { Tutorial: { } tutorial })
                            tutorial.RecordAbandoned();
                    }
                    await ReturnToMainMenuAsync();
                    break;
                case ScreenEvent.Exit:
                    _window.Close();
                    break;
                case ScreenEvent.QuickSave:
                    if (ReferenceEquals(origin, _screens.Current)) await QuickSaveLoadFromCurrentAsync(save: true);
                    break;
                case ScreenEvent.QuickLoad:
                    if (ReferenceEquals(origin, _screens.Current)) await QuickSaveLoadFromCurrentAsync(save: false);
                    break;
                case ScreenEvent.OpenSettings:
                    await OpenSettingsAsync();
                    break;
                case ScreenEvent.CloseSettings:
                    await CloseOverlayAsync();
                    break;
                case ScreenEvent.OpenSaveWindow:
                    await OpenSaveWindowAsync();
                    break;
                case ScreenEvent.CloseSaveWindow:
                    await CloseOverlayAsync();
                    break;
                case ScreenEvent.OpenLoadWindow:
                    await OpenLoadWindowAsync();
                    break;
                case ScreenEvent.CloseLoadWindow:
                    if (_screens.Current is LoadScreen) await CloseOverlayAsync();
                    break;
                case ScreenEvent.LoadSlotRequested:
                    if (payload is not null)
                        await LoadSlotAsync(payload);
                    break;
                case ScreenEvent.OpenFinance:
                    await OpenFinanceAsync();
                    break;
                case ScreenEvent.CloseFinance:
                    await CloseOverlayAsync();
                    break;
                case ScreenEvent.OpenShip:
                    await OpenShipAsync();
                    break;
                case ScreenEvent.CloseShip:
                    await CloseOverlayAsync();
                    break;
                case ScreenEvent.OpenStation:
                    await OpenStationAsync();
                    break;
                case ScreenEvent.TravelDock:
                case ScreenEvent.TravelMarket:
                case ScreenEvent.TravelHabitation:
                case ScreenEvent.TravelAdministration:
                    if (_session is not null && _screens.Current is StationScreen)
                        await _session.TravelStationAsync((StationDistrict)(evt - ScreenEvent.TravelDock));
                    break;
                case ScreenEvent.CloseStation:
                    _pendingUndockCommandId = null;
                    if (_screens.Current is StationScreen)
                        await CloseOverlayAsync();
                    break;
                case ScreenEvent.Undock:
                    if (_session is not null && _screens.Current is StationScreen stationScreen &&
                        _pendingUndockCommandId is null)
                    {
                        var departureSession = _session;
                        if (await stationScreen.ConfirmSelectedRouteAsync() && ReferenceEquals(_session, departureSession) &&
                            ReferenceEquals(_screens.Current, stationScreen))
                            _pendingUndockCommandId = departureSession.SendUndockCommand(stationScreen.SelectedDestinationId);
                    }
                    break;
                case ScreenEvent.OpenDialogue:
                    if (_screens.Current is GameSessionScreen && _session?.Buffer.Latest?.Snapshot.ActiveDialogue is { } active)
                    {
                        // The engine owns this modal pause, including loaded dialogues.
                        _screens.Push(new DialogueScreen(_session.Buffer, _session, active));
                        _modalDepth++;
                    }
                    break;
                case ScreenEvent.OpenTempCharacterImage:
                    if (_screens.Current is GameSessionScreen)
                        await PushModalAsync(new Screens.TempCharacterImage.TempCharacterImageScreen());
                    break;
                case ScreenEvent.CloseTempCharacterImage:
                    if (_screens.Current is Screens.TempCharacterImage.TempCharacterImageScreen)
                        await CloseOverlayAsync();
                    break;
                case ScreenEvent.CloseDialogue:
                    if (_screens.Current is DialogueScreen)
                    {
                        _screens.Pop();
                        _modalDepth--;
                        // Completion/abort restored authoritative speed in the engine.
                    }
                    break;
                case ScreenEvent.OpenTrade:
                    await OpenTradeAsync();
                    break;
                case ScreenEvent.CloseTrade:
                    if (_screens.Current is TradeScreen)
                        await CloseOverlayAsync();
                    await _stationNavigation.RemoveInvalidOverlaysAsync(_screens,
                        () => _session?.Buffer.Latest?.Snapshot, PopModalAsync);
                    break;
                case ScreenEvent.OpenHire:
                    await OpenHireAsync();
                    break;
                case ScreenEvent.CloseHire:
                    await CloseOverlayAsync();
                    break;
                case ScreenEvent.OpenContracts:
                    await OpenContractsAsync();
                    break;
                case ScreenEvent.CloseContracts:
                    await CloseOverlayAsync();
                    break;
                case ScreenEvent.NavigateToStation:
                    await NavigateToStationAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            // This runs on the native GLFW mouse/keyboard callback stack (via async void
            // OnMouseDown/OnMouseScroll/HandleKeyboardEdge) — letting an exception escape
            // unhandled here doesn't crash cleanly, it wedges the whole window: GLFW stops
            // dispatching further input/render callbacks, and the process sits at
            // "Not Responding" forever with no further InterfaceLog output. Catching and
            // logging here is what turns a screen-transition bug into a visible log line
            // instead of a silent freeze.
            InterfaceLog.Write($"HandleScreenEvent evt={evt} failed: {ex}");
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// NEW GAME no longer starts a session directly — it opens the scenario picker (a
    /// full top-level screen replacing MainMenu, not a paused-game overlay, so no
    /// PushModalAsync/speed-save dance is needed: there is never an active session to
    /// pause at this point). The actual session only starts once the player picks a row —
    /// see <see cref="StartGameSessionAsync(string)"/>.
    /// </summary>
    private void OpenScenarioSelect()
    {
        var screen = new ScenarioSelectScreen(() => _sessionFactory.ListScenarios());
        _screens.Replace(screen);
    }

    /// <summary>
    /// Bootstraps the session off the UI thread: <c>CreateSessionFromScenario</c> reads and
    /// parses several JSON files synchronously, and if that disk I/O stalls (e.g. antivirus
    /// scanning freshly built output), running it inline here would block Silk.NET's message
    /// pump — the whole window would stop rendering and responding to input (observed as
    /// Windows "Not Responding") for as long as the stall lasts. GameSessionHandle/
    /// GameSessionScreen construction stays on the UI thread since it's effectively free.
    /// </summary>
    private async Task StartGameSessionAsync(string scenarioPath)
    {
        if (_session is not null)
            return;

        await _lastSessionDisposal;
        if (_closing) return;

        var connection = await _connectionLoader.CreateAsync(() => _sessionFactory.CreateSessionFromScenario(scenarioPath));
        if (!_connectionLoader.TryAdopt(connection)) return;

        var session = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        try
        {
            var predictor = new LinearMotionPredictor();
            var gameScreen = new GameSessionScreen(session.Buffer, predictor, session,
                showTrajectoryPrediction: GetShowTrajectoryPrediction(),
                uiScale: (float)GetUiScale(), mapSettings: TacticalMapSettings.Load(Path.Combine(AppContext.BaseDirectory, "Settings.json")),
                tacticalBattleInputs: _tacticalInputs, maneuverInputs: _maneuverInputs);

            _session = session;
            _gameSessionScreen = gameScreen;
            _modalDepth = 0;
            _savedSpeed = SimulationSpeed.Speed1;
            _screens.Replace(gameScreen);
            session.DebugTuningFiles.Activate();
            session.ActivateAutosave();
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// New Game's «is there any save?» check: asks the factory for the file list only (no slot is parsed). A file-system
    /// error is logged and treated as «no saves», so the button still starts a game instead of doing nothing.
    /// </summary>
    private bool HasAnySaveSlotForNewGame()
    {
        try
        {
            return _sessionFactory.HasAnySaveSlot();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            InterfaceLog.Write($"New Game: save list unavailable, treating as no saves: {error.Message}");
            return false;
        }
    }

    /// <summary>
    /// NEW GAME without any save: start the hidden Tutorial scenario and go straight into its
    /// tactical battle (default request, seed 1, no setup screen). The whole sequence runs under
    /// <see cref="_transitionLock"/> while the main menu stays on screen; the world map is only
    /// placed under the battle modal once the battle has been accepted, so it never becomes the
    /// current screen or receives input. Any failure is logged to InterfaceLog and disposes the session.
    /// When the tutorial itself is unavailable (no tutorial scenario, or the engine rejects the battle as
    /// <see cref="TacticalBattleRejectionCode.Unavailable"/>) the player falls back to the scenario picker,
    /// so a fresh install without saves can always start a game; any other failure returns to the main menu.
    /// </summary>
    private async Task StartTutorialAsync()
    {
        if (_session is not null)
            return;

        await _lastSessionDisposal;
        if (_closing) return;

        GameSessionHandle? session = null;
        bool fallBackToScenarioSelect = false;
        try
        {
            // The tutorial lookup scans the scenarios on disk, so it runs on the loader's background task together with
            // the session creation instead of on the window thread.
            var connection = await _connectionLoader.CreateAsync(() =>
            {
                string? scenarioPath = _sessionFactory.GetTutorialScenarioPath();
                if (scenarioPath is null)
                {
                    fallBackToScenarioSelect = true;
                    throw new FileNotFoundException("Tutorial scenario is not available.");
                }
                return _sessionFactory.CreateSessionFromScenario(scenarioPath);
            });
            if (!_connectionLoader.TryAdopt(connection)) return;

            session = new GameSessionHandle(connection, deferDebugTuningWrites: true);
            // The tutorial starts the player's own persistent wing: wait for the first authoritative
            // carrier surface so the start request carries its real ShipId/revision. A missing or
            // empty wing fails the atomic start instead of granting a synthetic four-drone fallback.
            var carrier = await session.WaitForCarrierOperationsAsync(TutorialCarrierSnapshotTimeout);
            if (carrier is null || carrier.OwnedDrones.IsDefaultOrEmpty)
                throw new InvalidOperationException(carrier is null
                    ? "Tutorial requires the carrier operations snapshot."
                    : "Tutorial requires an owned drone wing.");
            var gameScreen = new GameSessionScreen(session.Buffer, new LinearMotionPredictor(), session,
                showTrajectoryPrediction: GetShowTrajectoryPrediction(),
                uiScale: (float)GetUiScale(), mapSettings: TacticalMapSettings.Load(Path.Combine(AppContext.BaseDirectory, "Settings.json")),
                tacticalBattleInputs: _tacticalInputs, maneuverInputs: _maneuverInputs);
            Localization.ApplyLanguage(Localization.ReadLanguageSetting(SettingsFilePath));
            var flow = new TacticalBattleFlow(session) { IsTutorial = true, RequireBattleResults = true };

            // Same order as a battle opened from the map: authoritative pause first, then the battle.
            session.ClearStationTravelPause();
            await session.SetSpeedAsync(SimulationSpeed.Speed0);
            var started = await flow.StartAsync(new StartTacticalBattleRequest(
                TacticalBattleFlow.NewIdentity("start"), TutorialBattleSeed, TutorialBattleScenarioId)
            {
                SourceShipId = carrier.ShipId,
                ExpectedCarrierRevision = carrier.Revision,
            });
            if (started?.Accepted != true || flow.State is null)
            {
                fallBackToScenarioSelect = started is { Accepted: false, Code: TacticalBattleRejectionCode.Unavailable };
                throw new InvalidOperationException("Tutorial battle was rejected: " + (started?.Code.ToString() ?? "no response"));
            }
            if (_closing || _exitRequested)
            {
                await session.DisposeAsync();
                return;
            }

            _session = session;
            _gameSessionScreen = gameScreen;
            // The tutorial world starts paused, so leaving the finished battle shows a paused map like a fresh New Game.
            _savedSpeed = SimulationSpeed.Speed0;
            _screens.ReplaceAll(gameScreen);
            _screens.Push(CreateTacticalBattleView(flow));
            _modalDepth = 1;
            session.DebugTuningFiles.Activate();
            session.ActivateAutosave(saveInitialState: false);
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"Tutorial start failed: {ex.Message}");
            _session = null;
            _gameSessionScreen = null;
            _modalDepth = 0;
            _savedSpeed = SimulationSpeed.Speed1;
            if (!_closing)
                _screens.ReplaceAll(fallBackToScenarioSelect
                    ? new ScenarioSelectScreen(() => _sessionFactory.ListScenarios())
                    : new MainMenuScreen(_sessionFactory.IsDemoProfile));
            if (session is not null)
            {
                _lastSessionDisposal = session.DisposeAsync().AsTask();
                await _lastSessionDisposal;
            }
        }
    }

    private bool GetShowTrajectoryPrediction()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return true;

            using var doc = JsonDocument.Parse(File.ReadAllText(SettingsFilePath));
            if (doc.RootElement.TryGetProperty("gameSettings", out var gs) &&
                gs.TryGetProperty("showTrajectoryPrediction", out var stp))
                return stp.GetBoolean();

            return true;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Push a modal screen onto the stack.
    /// For the first modal: awaits authoritative Pause BEFORE showing the overlay.
    /// For nested modals (modalDepth &gt; 0): pushes immediately (already paused).
    /// Use this for ALL modal screens (GameMenu, Settings, Save, Load, etc.).
    /// </summary>
    private async Task PushModalAsync(IScreen screen, Func<bool>? stillValid = null)
    {
        var openingSession = _session;
        // First modal: confirm authoritative Pause before showing anything
        if (_modalDepth == 0 && openingSession is not null)
        {
            _savedSpeed = openingSession.Buffer.CurrentSpeed;
            openingSession.ClearStationTravelPause();
            await openingSession.SetSpeedAsync(SimulationSpeed.Speed0);
            // Speed0 is now confirmed — safe to show the overlay
        }

        if (stillValid is not null && (!ReferenceEquals(_session, openingSession) || !stillValid()))
        {
            if (_modalDepth == 0 && openingSession is not null &&
                ReferenceEquals(_session, openingSession))
                await openingSession.SetSpeedAsync(openingSession.KeepPausedAfterStationTravel
                    ? SimulationSpeed.Speed0 : _savedSpeed);
            return;
        }

        if (EnsureShipDestroyedScreen() && screen is not LoadScreen) return;
        _screens.Push(screen);
        _modalDepth++;
    }

    /// <summary>Replaces the whole stack with the terminal screen once per destroyed session.</summary>
    private bool EnsureShipDestroyedScreen()
    {
        if (_session?.IsPlayerShipDestroyed != true) return false;
        // The battle screen is authoritative until its exit/settlement/checkpoint (and any retry)
        // succeeds. Only after the battle screen is gone may the terminal screen replace the world.
        if (_screens.Current is TacticalBattleModalScreen { IsSetup: false }) return false;
        if (ReferenceEquals(_shipDestroyedSession, _session)) return true;
        _shipDestroyedSession = _session;
        _modalDepth = 0;
        _savedSpeed = SimulationSpeed.Speed0;
        _pendingUndockCommandId = null;
        _screens.ReplaceAll(new ShipDestroyedScreen((float)GetUiScale()));
        return true;
    }

    private TacticalBattleModalScreen CreateTacticalSetupView(TacticalBattleFlow flow) =>
        flow.IsCampaign ? new CapturedEncounterScreen(flow, _gameSessionScreen?.UiScale ?? 1)
        : new TacticalBattleSetupScreen(flow, _gameSessionScreen?.UiScale ?? 1) { LaunchKey = _tacticalInputs.TacticalBattle, WorldDebugKey = _maneuverInputs.DebugPanel };
    /// <summary>
    /// Seed of the tutorial battle (scenario «initial», default loadout). Pinned by <c>TutorialBattleWinnabilityTests</c>: on this seed the
    /// player's side wins both by Enter only and by following the officer's lessons (EP-0028-US-0005-TK-0002).
    /// </summary>
    internal const long TutorialBattleSeed = 1;

    /// <summary>The tutorial always launches the owned wing into the canonical first encounter.</summary>
    internal const string TutorialBattleScenarioId = "initial";

    /// <summary>Bounded wait for the first authoritative <c>CarrierOperations</c> snapshot; internal so tests need not wait ten seconds.</summary>
    internal TimeSpan TutorialCarrierSnapshotTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>The tutorial battle's skin (D-03): its hints are laid out for Tabletop.</summary>
    internal const string TutorialSkinId = "tabletop";

    private TacticalBattleModalScreen CreateTacticalBattleView(TacticalBattleFlow flow) =>
        new TacticalBattleScreen(flow, _gameSessionScreen?.UiScale ?? 1, flow.IsTutorial ? TutorialSkin() : _battleSkin)
        {
            LaunchKey = _tacticalInputs.TacticalBattle,
            WorldDebugKey = _maneuverInputs.DebugPanel,
            Tutorial = flow.IsTutorial ? CreateTutorialSession() : null,
        };

    /// <summary>Tabletop for the tutorial battle without touching the saved preference; without the pack, the preferred skin (A-07).</summary>
    private TacticalBattleSkin TutorialSkin()
    {
        if (_battleSkinCatalog.TryGet(TutorialSkinId, out var tabletop))
            return tabletop;
        LogTutorialWarning($"TUTORIAL: skin '{TutorialSkinId}' unavailable, using '{_battleSkin.Id}'");
        return _battleSkin;
    }

    /// <summary>The tutorial battle's session: hints start from <c>gameSettings.tutorialHints</c>; in-battle switches are saved there.</summary>
    private TutorialSession CreateTutorialSession() =>
        new(TutorialScriptLoader.Load(AppContext.BaseDirectory, LogTutorialWarning))
        {
            HintsEnabled = TutorialHintsPreference.Read(SettingsFilePath, LogTutorialHintsWarning),
            PersistHints = TrySetTutorialHints,
            MetricsDirectory = TacticalBattleMetrics.DirectoryPath, // step events next to the battle report
        };
    private static async Task LogTacticalUnavailableAsync(TacticalBattleFlow flow)
    {
        await flow.OptionsTask!;
        if (flow.Options?.Availability.Available != false) return;
        try { InterfaceLog.Write("TACTICAL unavailable: " + flow.Error); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>
    /// Pop the current modal screen.
    /// Last modal awaits the authoritative speed restore before returning.
    /// </summary>
    private async Task PopModalAsync()
    {
        if (_screens.Current is ShipDestroyedScreen) return;
        _screens.Pop();
        _modalDepth--;

        // Last modal closed: restore previous simulation speed
        if (_modalDepth == 0 && _session is not null)
        {
            await _session.SetSpeedAsync(_session.KeepPausedAfterStationTravel ? SimulationSpeed.Speed0 : _savedSpeed);
        }
    }

    private async Task OpenGameMenuAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is GameMenuScreen)
            return;

        await PushModalAsync(new GameMenuScreen());
    }

    /// <summary>
    /// Push the Finance overlay (DB/Documentation/02-FirstRelease/Screens/Finance.md). Opened via the
    /// bottom-center Finance panel button or Ctrl+F. Uses the same generic
    /// PushModalAsync pause-on-open behavior as every other modal — no Finance-specific
    /// speed logic needed.
    /// </summary>
    private async Task OpenFinanceAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is FinanceScreen)
            return;

        var session = _session;
        await PushModalAsync(new FinanceScreen(session?.Buffer,
            service: session is null ? null : (command, token) => session.ExecuteDemoCampaignAsync(command, token),
            campaign: session is null ? null : () => session.DemoCampaign));
    }

    /// <summary>
    /// Push the Ship overlay (DB/Documentation/02-FirstRelease/Screens/Ship.md). Opened via the
    /// bottom-center Ship panel button or Ctrl+S. Uses the same generic
    /// PushModalAsync pause-on-open behavior as every other modal — no Ship-specific
    /// speed logic needed.
    /// </summary>
    private async Task OpenShipAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is ShipScreen)
            return;

        var session = _session;
        var carrier = session?.CarrierOperations;
        // Rearm draws from the cargo hold, not from a fabricated stock: sum the authoritative torpedo
        // stacks and pass them to the screen. Legacy/unsupported sessions get zero stock and no transport.
        int stock = carrier is null ? 0 : TorpedoStock(session!.Buffer.Presented?.Snapshot);
        Func<RearmDroneRequest, CancellationToken, ValueTask<CarrierCommandResponse>>? rearm =
            session is not null && carrier is not null
                ? (request, cancellationToken) => session.RearmDroneAsync(request, cancellationToken)
                : null;
        await PushModalAsync(new ShipScreen(ShipViewModel.From(carrier), stock, rearm,
            campaign: session is null ? null : () => session.DemoCampaign,
            service: session is null ? null : (command, token) => session.ExecuteDemoCampaignAsync(command, token),
            carrier: session is null ? null : () => session.CarrierOperations));
    }

    /// <summary>
    /// Torpedoes available in the cargo hold, summed across installed modules with saturated
    /// addition: oversized cargo stacks cap at <see cref="int.MaxValue"/> instead of wrapping.
    /// </summary>
    internal static int TorpedoStock(AuthoritativeSnapshot? snapshot)
    {
        if (snapshot?.InstalledModules is not { IsDefaultOrEmpty: false } modules)
            return 0;
        long total = 0;
        foreach (var module in modules)
        {
            if (module.Cargo is not { IsDefaultOrEmpty: false } cargo) continue;
            foreach (var stack in cargo)
            {
                if (stack.ItemTypeId != "item.drone-torpedo.t1" || stack.Quantity <= 0) continue;
                long remaining = int.MaxValue - total;
                total += stack.Quantity >= remaining ? remaining : stack.Quantity;
            }
        }
        return (int)total;
    }

    /// <summary>
    /// Push the Station overlay (DB/Documentation/02-FirstRelease/Screens/Station.md). Opened by
    /// left-clicking, on the tactical map, the station the player ship is currently
    /// docked to (GameSessionScreen.OnMouseDown → ScreenEvent.OpenStation). Uses the
    /// same generic PushModalAsync pause-on-open behavior as every other modal — no
    /// Station-specific speed/docking logic needed here.
    /// </summary>
    private async Task OpenStationAsync()
    {
        if (_session is not { } handle || _screens.Current is not GameSessionScreen ||
            !StationTradeNavigation.CanOpen(handle.Buffer.Latest?.Snapshot)) return;
        var station = new StationScreen(handle.Buffer, handle);
        await PushModalAsync(station, () => _screens.Current is GameSessionScreen &&
            ReferenceEquals(_session, handle) && station.HasValidVisit);
    }

    /// <summary>Return from a nested station window to its hub.</summary>
    private async Task NavigateToStationAsync()
    {
        if (_screens.Current is not TradeScreen) return;
        await PopModalAsync();
        await _stationNavigation.RemoveInvalidOverlaysAsync(_screens,
            () => _session?.Buffer.Latest?.Snapshot, PopModalAsync);
        if (_screens.Current is GameSessionScreen)
            await OpenStationAsync();
    }

    /// <summary>
    /// Push the Trade overlay (DB/Documentation/02-FirstRelease/Screens/Trade.md) on top of Station
    /// (StationScreen's `TRADE` button → ScreenEvent.OpenTrade). A nested modal exactly
    /// like Trade/Hire/Contracts — PushModalAsync/PopModalAsync's generic modal-depth
    /// tracking needs no Trade-specific handling. Unlike Hire/Contracts, TradeScreen also
    /// gets the live session handle (`_session`): its lower right-hand action panel sends
    /// Buy/Sell/Refuel commands directly (GameSessionHandle.SendTradeCommand), the same
    /// fire-and-forget pattern GameSessionScreen already uses.
    /// </summary>
    private async Task OpenTradeAsync()
    {
        if (_session is not { } handle || _screens.Current is not StationScreen station ||
            !ReferenceEquals(station.OpenedForHandle, handle) || !station.HasValidVisit ||
            !StationTradeNavigation.CanOpen(handle.Buffer.Latest?.Snapshot)) return;
        var trade = new TradeScreen(handle.Buffer, handle);
        if (!trade.HasValidVisit || trade.OpenedForStationObjectId != station.OpenedForStationObjectId)
            return;
        await PushModalAsync(trade, () => ReferenceEquals(_session, handle) &&
            ReferenceEquals(_screens.Current, station) && station.HasValidVisit && trade.HasValidVisit);
    }

    /// <summary>
    /// Push the Hire overlay (DB/Documentation/02-FirstRelease/Screens/Hire.md) on top of Station
    /// (StationScreen's `HIRE` button → ScreenEvent.OpenHire). A nested modal exactly
    /// like Trade — PushModalAsync/PopModalAsync's generic modal-depth tracking needs no
    /// Hire-specific handling.
    /// </summary>
    private async Task OpenHireAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is HireScreen)
            return;

        var session = _session;
        await PushModalAsync(new HireScreen(session?.Buffer,
            service: session is null ? null : (command, token) => session.ExecuteDemoCampaignAsync(command, token),
            campaign: session is null ? null : () => session.DemoCampaign));
    }

    /// <summary>
    /// Push the Contracts overlay (DB/Documentation/02-FirstRelease/Screens/Contracts.md) on top of
    /// Station (StationScreen's `CONTRACTS` button → ScreenEvent.OpenContracts). Split
    /// out of Hire (passenger contracts vs. crew hiring) — a nested modal exactly like
    /// Trade/Hire, no Contracts-specific handling needed. EP-0034 US-0006: also opened from the map (journal
    /// button / Ctrl+J); in flight it shows the obligations journal and the engine refuses acceptance until the
    /// ship docks. The board reads the session's campaign projection and sends typed commands through it.
    /// </summary>
    private async Task OpenContractsAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is ContractsScreen)
            return;

        var session = _session;
        await PushModalAsync(new ContractsScreen(session?.Buffer, session is null ? null : () => session.DemoCampaign,
            session is null ? null : command => session.ExecuteDemoCampaignAsync(command)));
    }

    private async Task OpenSettingsAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is SettingsScreen)
            return;

        var monitors = _window is null ? [] : Silk.NET.Windowing.Monitor.GetMonitors(_window).ToArray();
        InterfaceLog.Write(
            $"DIAG Monitors found: {monitors.Length} — " +
            string.Join(" | ", monitors.Select(m =>
                $"idx={m.Index} name={m.Name} origin=({m.Bounds.Origin.X},{m.Bounds.Origin.Y}) " +
                $"boundsSize={m.Bounds.Size.X}x{m.Bounds.Size.Y} videoMode={m.VideoMode.Resolution?.X}x{m.VideoMode.Resolution?.Y}")));

        var monitorNames = monitors.Length > 0
            ? monitors.Select((m, i) => $"Monitor {i + 1} ({m.Bounds.Size.X}x{m.Bounds.Size.Y})").ToArray()
            : new[] { "Monitor 1" };

        int selectedMonitorIndex = GetSelectedMonitorIndex();
        if (selectedMonitorIndex < 0 || selectedMonitorIndex >= monitorNames.Length)
            selectedMonitorIndex = 0;

        double uiScale = GetUiScale();
        string language = Localization.ReadLanguageSetting(SettingsFilePath);

        await PushModalAsync(new SettingsScreen(
            monitorNames, selectedMonitorIndex, SaveSelectedMonitorIndex,
            uiScale, SaveUiScale,
            language, SaveLanguage,
            _battleSkinCatalog.Options, _battleSkin.Id, TrySelectBattleSkin,
            TutorialHintsPreference.Read(SettingsFilePath, LogTutorialHintsWarning), TrySetTutorialHints));
    }

    private bool TrySetTutorialHints(bool enabled) =>
        TutorialHintsPreference.TryWrite(SettingsFilePath, enabled, LogTutorialHintsWarning);

    private static void LogTutorialHintsWarning(string message) => LogTutorialWarning("TUTORIAL: " + message);

    private bool TrySelectBattleSkin(string id)
    {
        if (!_battleSkinCatalog.TryGet(id, out var skin) ||
            !TacticalBattleSkinPreference.TryWrite(SettingsFilePath, id, LogSkinWarning))
            return false;
        _battleSkin = skin;
        return true;
    }

    private static void LogSkinWarning(string message)
    {
        try { InterfaceLog.Write("TACTICAL SKIN: " + message); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>TutorialScriptLoader messages already start with "TUTORIAL: ".</summary>
    private static void LogTutorialWarning(string message)
    {
        try { InterfaceLog.Write(message); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>
    /// Push the Save overlay. New Save/Overwrite and Delete are bound directly to
    /// <see cref="_session"/>/<see cref="_sessionFactory"/> — SaveScreen itself has no
    /// knowledge of either type. New Save/Overwrite is fire-and-forget
    /// (<see cref="SaveToSlotAsync"/> properly awaits <c>SaveAsync</c> off of
    /// <see cref="SaveScreen.OnMouseDown"/>'s synchronous return, matching the "render
    /// loop must never await the connection synchronously" rule from
    /// <see cref="IGameSessionConnection"/> and mirroring <see cref="QuickSaveAsync"/>);
    /// SaveScreen is notified once the write lands so it can refresh its slot list. Delete
    /// stays a direct synchronous call — <see cref="IGameSessionFactory.DeleteSaveSlot"/>
    /// has no awaited I/O.
    /// The reserved quicksave slot (<see cref="SaveSlots.Quicksave"/>) is filtered out of
    /// the list shown here so a player can never see/overwrite/delete it from this window
    /// and silently break F9 quickload; SaveScreen separately rejects it as a New-Save name.
    /// </summary>
    private async Task OpenSaveWindowAsync()
    {
        if (_session?.IsPlayerShipDestroyed == true) return;
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is SaveScreen)
            return;

        if (_session is null)
            return;

        var saveScreen = new SaveScreen(
            () => SaveSlots.ExcludeReserved(_sessionFactory.ListSaveSlots()),
            slotId => _ = SaveToSlotAsync(slotId),
            DeleteSaveSlotSafely);

        await PushModalAsync(saveScreen);
    }

    /// <summary>Delete callback for SaveScreen's two-stage Delete button. Synchronous —
    /// filesystem delete has no awaited I/O — but guarded so a locked/missing file never
    /// crashes the render loop, for consistency with how every other save action here
    /// logs and swallows its own failures.</summary>
    private void DeleteSaveSlotSafely(string slotId)
    {
        try
        {
            _sessionFactory.DeleteSaveSlot(slotId);
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"Delete save slot '{slotId}' failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Fire-and-forget target for SaveScreen's New Save/Overwrite actions. Mirrors
    /// <see cref="QuickSaveAsync"/>'s await/log-on-failure shape, but doesn't touch
    /// simulation speed (the modal is already paused) and notifies the still-open
    /// SaveScreen to refresh its list once the write completes. Guarded so a save
    /// finishing after the player has already closed the Save window is a safe no-op.
    /// </summary>
    private async Task SaveToSlotAsync(string slotId)
    {
        if (_session is null || _session.IsPlayerShipDestroyed)
            return;

        try
        {
            await _session.SaveAsync(slotId);
            InterfaceLog.Write($"Save: wrote Saves/{slotId}.json");
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"Save failed for slot '{slotId}': {ex.Message}");
        }

        if (_screens.Current is SaveScreen saveScreen)
            saveScreen.NotifySaveCompleted(slotId);
    }

    /// <summary>
    /// Push the Load overlay. Mirrors <see cref="OpenSaveWindowAsync"/> but deliberately
    /// WITHOUT its <c>if (_session is null) return;</c> guard: Load must work from
    /// <see cref="MainMenuScreen"/>, where there is no session yet. This is safe for the
    /// same reason <see cref="OpenSettingsAsync"/> already works from both MainMenu (no
    /// session) and GameMenu (active session) — <see cref="PushModalAsync"/> itself
    /// null-guards <see cref="_session"/> and simply skips the pause/speed-save step when
    /// there is nothing to pause. Delete is bound directly to
    /// <see cref="_sessionFactory"/>; loading a row is NOT bound here — LoadScreen instead
    /// returns <see cref="ScreenEvent.LoadSlotRequested"/> (with the slot id carried
    /// alongside it, see <see cref="OnMouseDown(IMouse, MouseButton)"/>) so the actual
    /// session swap runs inside <see cref="HandleScreenEvent"/>'s <see cref="_transitionLock"/>
    /// like every other operation that disposes/replaces <see cref="_session"/>.
    /// Unlike <see cref="OpenSaveWindowAsync"/>, the reserved quicksave slot is NOT
    /// filtered out of the list here — a player must be able to load their last
    /// quicksave from this window. <see cref="LoadScreen"/> itself refuses to delete it
    /// (no DELETE button on that row, and the click handler no-ops on it), so it can
    /// never be removed here and silently break F9 quickload/F5 quicksave.
    /// </summary>
    private async Task OpenLoadWindowAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is LoadScreen)
            return;

        var loadScreen = new LoadScreen(
            () => _sessionFactory.ListSaveSlots(),
            DeleteSaveSlotSafely);

        await PushModalAsync(loadScreen);
    }

    private string SettingsFilePath { get; }

    private int GetSelectedMonitorIndex()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return 0;

            using var doc = JsonDocument.Parse(File.ReadAllText(SettingsFilePath));
            if (doc.RootElement.TryGetProperty("gameSettings", out var gs) &&
                gs.TryGetProperty("selectedMonitorIndex", out var smi))
                return smi.GetInt32();

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Persists the chosen monitor immediately (per requirements), but the running
    /// window is not moved — monitor placement is only read once, at startup
    /// (<see cref="OnLoad"/>), so the choice takes effect on the next launch.
    /// </summary>
    private void SaveSelectedMonitorIndex(int index)
    {
        try
        {
            var root = File.Exists(SettingsFilePath)
                ? JsonNode.Parse(File.ReadAllText(SettingsFilePath)) as JsonObject ?? new JsonObject()
                : new JsonObject();

            if (root["gameSettings"] is not JsonObject gameSettings)
            {
                gameSettings = new JsonObject();
                root["gameSettings"] = gameSettings;
            }

            gameSettings["selectedMonitorIndex"] = index;

            File.WriteAllText(SettingsFilePath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            InterfaceLog.Write($"Settings: selectedMonitorIndex saved = {index} (applies after restart)");
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"Settings: failed to save selectedMonitorIndex: {ex.Message}");
        }
    }

    /// <summary>
    /// Falls back to 1.0 (100%) whenever the persisted value is missing, malformed,
    /// or outside the allowed set — an invalid uiScale must never break startup.
    /// </summary>
    private static double ValidateUiScale(double scale) =>
        Array.Exists(AllowedUiScales, v => Math.Abs(v - scale) < 0.001) ? scale : 1.0;

    private double GetUiScale()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return 1.0;

            using var doc = JsonDocument.Parse(File.ReadAllText(SettingsFilePath));
            if (doc.RootElement.TryGetProperty("gameSettings", out var gs) &&
                gs.TryGetProperty("uiScale", out var scale))
                return ValidateUiScale(scale.GetDouble());

            return 1.0;
        }
        catch
        {
            return 1.0;
        }
    }

    /// <summary>
    /// Persists the chosen UI scale and applies it immediately to the currently
    /// open GameSessionScreen, if any (the Settings screen itself never scales).
    /// </summary>
    private void SaveUiScale(double scale)
    {
        try
        {
            var root = File.Exists(SettingsFilePath)
                ? JsonNode.Parse(File.ReadAllText(SettingsFilePath)) as JsonObject ?? new JsonObject()
                : new JsonObject();

            if (root["gameSettings"] is not JsonObject gameSettings)
            {
                gameSettings = new JsonObject();
                root["gameSettings"] = gameSettings;
            }

            gameSettings["uiScale"] = scale;

            File.WriteAllText(SettingsFilePath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            InterfaceLog.Write($"Settings: uiScale saved = {scale}");
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"Settings: failed to save uiScale: {ex.Message}");
        }

        _gameSessionScreen?.SetUiScale((float)scale);
    }

    /// <summary>Persists the chosen language and applies it to subsequent UI lookups.</summary>
    private void SaveLanguage(string language)
    {
        try
        {
            var root = File.Exists(SettingsFilePath)
                ? JsonNode.Parse(File.ReadAllText(SettingsFilePath)) as JsonObject ?? new JsonObject()
                : new JsonObject();

            if (root["gameSettings"] is not JsonObject gameSettings)
            {
                gameSettings = new JsonObject();
                root["gameSettings"] = gameSettings;
            }

            gameSettings["language"] = language;

            File.WriteAllText(SettingsFilePath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            Localization.ApplyLanguage(language);
            InterfaceLog.Write($"Settings: language saved = {language}");
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"Settings: failed to save language: {ex.Message}");
        }
    }

    private async Task CloseOverlayAsync()
    {
        if (_modalDepth <= 0)
            return;

        await PopModalAsync();
    }

    /// <summary>
    /// F5 — quicksave. No modal/UI window is ever shown. Pauses the session first
    /// (authoritative Speed0, awaited before capturing state) and, per F.18,
    /// deliberately leaves the game at Speed0 afterwards — it does not restore the
    /// pre-save speed. Debounced: a save already in flight makes a new F5 a no-op.
    /// </summary>
    private async Task QuickSaveLoadFromCurrentAsync(bool save)
    {
        var results = _screens.Current as BattleResultsScreen;
        if (results is not null && !results.TryBeginSaveLoad()) return;
        try
        {
            if (save) await QuickSaveAsync();
            else await QuickLoadAsync();
        }
        finally { results?.EndSaveLoad(); }
    }

    private async Task QuickSaveAsync()
    {
        if (_session is null || _session.IsPlayerShipDestroyed || _quickSaveLoadInFlight)
            return;

        _quickSaveLoadInFlight = true;
        try
        {
            await _session.SetSpeedAsync(SimulationSpeed.Speed0);
            await _session.SaveAsync(SaveSlots.Quicksave);
            InterfaceLog.Write("QuickSave: wrote Saves/quicksave.json");
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"QuickSave failed: {ex.Message}");
        }
        finally
        {
            _quickSaveLoadInFlight = false;
        }
    }

    /// <summary>
    /// F9 — quickload. No modal/UI window is ever shown. Keeps its own
    /// <see cref="IGameSessionFactory.HasQuickSave"/> pre-check and its own
    /// "QuickLoad: ..." log messages, but delegates the actual session-swap body to
    /// <see cref="LoadSlotCoreAsync"/> — the same core <see cref="LoadSlotAsync"/> uses
    /// for a UI-triggered Load, generalized only by slot id. Debounced via the shared
    /// <see cref="_quickSaveLoadInFlight"/> guard: a load already in flight (from either
    /// F9 or the Load window) makes a new F9 a no-op, so F9 and a UI Load can never race.
    /// </summary>
    private async Task QuickLoadAsync()
    {
        if (_quickSaveLoadInFlight)
            return;

        _quickSaveLoadInFlight = true;
        try
        {
            if (!_sessionFactory.HasQuickSave())
            {
                InterfaceLog.Write("QuickLoad: no save file found, ignored.");
                if (_screens.Current is ShipDestroyedScreen terminal) terminal.ShowStatus("Demo.Save.Empty");
                return;
            }

            if (await LoadSlotCoreAsync(SaveSlots.Quicksave, "QuickLoad"))
                InterfaceLog.Write("QuickLoad: loaded Saves/quicksave.json");
            else if (_screens.Current is ShipDestroyedScreen terminal) terminal.ShowStatus("Demo.Save.LoadFailed");
        }
        finally
        {
            _quickSaveLoadInFlight = false;
        }
    }

    /// <summary>
    /// Load an arbitrary save slot chosen from the Load window (UI-triggered, via
    /// <see cref="ScreenEvent.LoadSlotRequested"/>). Shares <see cref="LoadSlotCoreAsync"/>
    /// and the <see cref="_quickSaveLoadInFlight"/> re-entrancy guard with
    /// <see cref="QuickLoadAsync"/>. Because both are only ever invoked from inside
    /// <see cref="HandleScreenEvent"/>'s <c>_transitionLock</c>-guarded switch, F9 and a UI
    /// Load (or two rapid UI Load clicks) can never actually race each other for this guard
    /// — each call runs to completion, including resetting the flag in its own
    /// <c>finally</c>, before the next queued call is admitted. A rapid double-click on LOAD
    /// is therefore never unsafe, but it is not a no-op either: it produces a redundant
    /// second load of the same (or a different) slot, not a skipped one.
    /// </summary>
    private async Task LoadSlotAsync(string slotId)
    {
        if (_quickSaveLoadInFlight)
            return;

        _quickSaveLoadInFlight = true;
        try
        {
            if (await LoadSlotCoreAsync(slotId, "Load"))
                InterfaceLog.Write($"Load: loaded Saves/{slotId}.json");
        }
        finally
        {
            _quickSaveLoadInFlight = false;
        }
    }

    /// <summary>
    /// Shared session-swap core for <see cref="QuickLoadAsync"/>/<see cref="LoadSlotAsync"/>.
    /// Builds the replacement session/screen fully in local variables first; only on
    /// success is the old session disposed and swapped in — a broken/missing save file
    /// never destroys the currently running session (returns false, logging via
    /// <paramref name="logPrefix"/>, and leaves everything untouched). The new session is
    /// forced to Speed0 regardless of the speed recorded in the save file (per F.19 for
    /// quickload; same behavior generalizes cleanly to any slot). Camera/zoom/focus are
    /// never restored — GameSessionScreen always starts with its default camera.
    /// Uses <see cref="ScreenStack.ReplaceAll"/> (not <see cref="ScreenStack.Replace"/>):
    /// a UI Load can be triggered from a stack 2 or 3 deep (MainMenu -&gt; LoadScreen, or
    /// GameSessionScreen -&gt; GameMenu -&gt; LoadScreen), and ReplaceAll is
    /// behavior-identical to Replace at depth 1, so QuickLoadAsync (always depth ≤ 1) is
    /// unaffected by sharing it here.
    /// Callers are responsible for the <see cref="_quickSaveLoadInFlight"/> guard.
    /// </summary>
    private async Task<bool> LoadSlotCoreAsync(string slotId, string logPrefix)
    {
        if (_screens.Current is BattleResultsScreen { SaveLoadInProgress: false }) return false;
        if (_screens.Current is LoadScreen loading) loading.ClearError();
        GameSessionHandle? newSession = null;
        TacticalBattleFlow? recoveredEncounter = null;
        var previousSession = _session;
        GameSessionScreen newScreen;
        try
        {
            // Offloaded for the same reason as StartGameSessionAsync: a synchronous
            // CreateSessionFromSave call here would block Silk.NET's message pump if disk I/O
            // stalls, freezing the whole window for both the Load screen and QuickLoad (F9).
            await _lastSessionDisposal;
            if (previousSession is not null)
            {
                await previousSession.SuspendAutosaveAsync();
                await previousSession.DeactivateDebugTuningWritesAsync();
            }
            var connection = await _connectionLoader.CreateAsync(() => _sessionFactory.CreateSessionFromSave(slotId));
            if (!_connectionLoader.TryAdopt(connection)) return false;
            newSession = new GameSessionHandle(connection, deferDebugTuningWrites: true);
            var predictor = new LinearMotionPredictor();
            newScreen = new GameSessionScreen(newSession.Buffer, predictor, newSession,
                showTrajectoryPrediction: GetShowTrajectoryPrediction(),
                uiScale: (float)GetUiScale(), mapSettings: TacticalMapSettings.Load(Path.Combine(AppContext.BaseDirectory, "Settings.json")),
                tacticalBattleInputs: _tacticalInputs, maneuverInputs: _maneuverInputs);
            await newSession.SetSpeedAsync(SimulationSpeed.Speed0);
            recoveredEncounter = await RecoverBattleFlowAsync(newSession);
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"{logPrefix} failed: {ex.Message}");
            if (newSession is not null) await newSession.DisposeAsync();
            previousSession?.DebugTuningFiles.Activate();
            previousSession?.ActivateAutosave();
            if (!_closing && _screens.Current is not BattleResultsScreen && logPrefix is not ("Continue" or "Checkpoint"))
            {
                await OpenLoadWindowAsync();
                if (_screens.Current is LoadScreen load) load.ShowError(SessionConnectionLoader.LoadErrorKey(ex));
            }
            return false;
        }

        var oldSession = _session;
        if (_closing) { await newSession.DisposeAsync().ConfigureAwait(false); return false; }
        _session = newSession;
        _gameSessionScreen = newScreen;
        _modalDepth = 0;
        _savedSpeed = SimulationSpeed.Speed0;
        _screens.ReplaceAll(newScreen);
        if (recoveredEncounter is not null)
        {
            _modalDepth = 1;
            _screens.Push(recoveredEncounter.RestoreResultsModal
                ? new BattleResultsScreen(recoveredEncounter, newScreen.UiScale)
                : CreateTacticalBattleView(recoveredEncounter));
        }
        if (oldSession is not null)
        {
            _lastSessionDisposal = oldSession.DisposeAsync().AsTask();
            await _lastSessionDisposal;
        }

        newSession.DebugTuningFiles.Activate();
        newSession.ActivateAutosave(saveInitialState: false);

        return true;
    }

    /// <summary>The actual Load/Continue routing seam; never starts or replays combat.</summary>
    internal static async Task<TacticalBattleFlow?> RecoverBattleFlowAsync(GameSessionHandle session)
    {
        if (await session.GetTacticalBattleStateAsync() is not { } recovered) return null;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (session.DemoCampaign is null) await Task.Delay(10, timeout.Token);
        var campaign = session.DemoCampaign;
        var encounter = campaign.WorldEncounter;
        if (encounter?.Binding.BattleId != recovered.BattleId) encounter = null;
        var results = campaign.BattleResults;
        if (results?.BattleId != recovered.BattleId || recovered.Phase != TacticalBattlePhase.Over) results = null;
        bool frozenLoot = campaign.Rewards?.Loot is { Phase: LootPhases.Open } loot &&
            loot.BattleId == recovered.BattleId && recovered.Phase == TacticalBattlePhase.Over;
        if (encounter?.Phase != WorldEncounterPhases.PendingBattle && !frozenLoot && results is null)
            throw new IOException("Recovered battle has no pending encounter or frozen loot.");
        var flow = new TacticalBattleFlow(session)
        {
            CampaignEncounter = encounter,
            RequireBattleResults = results is not null || frozenLoot || encounter?.RequireBattleResults != false,
            RestoreResultsModal = results?.Stage == BattleResultsStage.Open
        };
        if (!flow.AdoptRecoveredEncounter(recovered)) throw new IOException("Recovered battle identity mismatch.");
        if (frozenLoot && results is null)
        {
            if (!await flow.OpenResultsAsync()) throw new IOException("Could not adopt recovered loot results.");
            flow.RestoreResultsModal = true;
        }
        return flow;
    }

    private async Task ReturnToMainMenuAsync()
    {
        // End the game session explicitly — no resume needed
        var oldSession = _session;
        _session = null;
        _gameSessionScreen = null;

        _modalDepth = 0;
        _savedSpeed = SimulationSpeed.Speed1;

        var mainMenu = new MainMenuScreen(_sessionFactory.IsDemoProfile);
        _screens.ReplaceAll(mainMenu);
        if (oldSession is not null)
        {
            _lastSessionDisposal = oldSession.DisposeAsync().AsTask();
            await _lastSessionDisposal;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        DebugConsole.Dispose();
        _ = _connectionLoader.DisposeAsync();

        // OnClosing might not have fired (e.g. unhandled shutdown).
        // If input still alive, dispose it before window.
        if (!_closing)
        {
            if (_mouse is not null)
            {
                _mouse.MouseDown -= OnMouseDown;
                _mouse.MouseUp -= OnMouseUp;
                _mouse.MouseMove -= OnMouseMove;
                _mouse.Scroll -= OnMouseScroll;
            }

            if (_keyboard is not null)
                _keyboard.KeyChar -= OnKeyChar;

            _input?.Dispose();
            _input = null;
        }

        if (_session is { } closingSession) closingSession.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _lastSessionDisposal.GetAwaiter().GetResult();
        _session = null;

        _screens.DeactivateAll();
        _battleSkinCatalog.Dispose();

        _surface?.Dispose();
        _renderTarget?.Dispose();
        _grContext?.Dispose();
        _gl?.Dispose();
        _window?.Dispose();
    }
}
