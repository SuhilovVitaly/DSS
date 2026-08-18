using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.Settings;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

public sealed class SkiaWindow : IDisposable
{
    private readonly IWindow _window;
    private readonly IGameSessionFactory _sessionFactory;
    private readonly ScreenStack _screens = new();

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
    private GameSessionScreen? _gameSessionScreen;
    private static readonly double[] AllowedUiScales = { 0.8, 1.0, 1.2, 1.5 };
    private readonly SemaphoreSlim _transitionLock = new(1, 1);
    private int _modalDepth;
    private SimulationSpeed _savedSpeed = SimulationSpeed.Speed1;
    private bool _quickSaveLoadInFlight;
    private readonly KeyboardEdgeTracker _keyboardEdges = new();
    private readonly Key[] _keyboardPressedKeys = new Key[16]; // must cover every key KeyboardEdgeTracker.Poll can report in one call
    private readonly Key[] _keyboardReleasedKeys = new Key[2]; // Ctrl release edges only (left/right)
    private readonly Action<Key> _handleKeyboardEdge;
    private bool _disposed;
    private bool _closing;
    private bool _isFocused = true;
    // Deliberately not long.MinValue: Environment.TickCount64 - long.MinValue overflows,
    // which would wrap around to a small/negative gap and wrongly suppress the very
    // first Escape press of a session (before any real focus event has ever fired).
    private long _focusRegainedAtMs = long.MinValue / 2;
    private const long MenuOpenFocusRegainGuardMs = 1500;

    // TEMP DIAG — startup timing investigation, remove once resolved.
    private readonly System.Diagnostics.Stopwatch? _startupStopwatch;
    private bool _firstFrameLogged;

    public SkiaWindow(IScreen initialScreen, IGameSessionFactory sessionFactory, System.Diagnostics.Stopwatch? startupStopwatch = null)
    {
        _sessionFactory = sessionFactory;
        _handleKeyboardEdge = HandleKeyboardEdge;
        _startupStopwatch = startupStopwatch;

        _screens.SetRoot(initialScreen);

        var options = WindowOptions.Default with
        {
            Title = "Deep Space Saga",
            WindowBorder = WindowBorder.Hidden,
            WindowState = WindowState.Normal,
            // Since the window is deliberately 1px shorter than the monitor (see
            // OnLoad) so Windows doesn't classify it as fullscreen, the taskbar no
            // longer auto-hides behind it on its own — start TopMost so it still
            // covers the taskbar. Toggled off whenever the window loses focus (see
            // OnFocusChanged) so it doesn't stay pinned over the Snipping Tool
            // (Print Screen) or whatever the user alt-tabs to.
            TopMost = true,
            FramesPerSecond = 80,
            VSync = false,
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3))
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
        _window.FocusChanged += OnFocusChanged;
        _window.Closing += OnClosing;

        // TEMP DIAG — startup timing investigation, remove once resolved.
        if (_startupStopwatch is not null)
            InterfaceLog.Write($"STARTUP DIAG: after Window.Create — {_startupStopwatch.ElapsedMilliseconds} ms since Main() start");
    }

    public void Run() => _window.Run();

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

        if (target is not null)
        {
            _window.Position = target.Bounds.Origin;

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

        _gl = _window.CreateOpenGL();

        var glInterface = GRGlInterface.Create();
        glInterface.Validate();
        _grContext = GRContext.CreateGl(glInterface);

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

        if (_mouse is not null)
        {
            _mouse.MouseDown -= OnMouseDown;
            _mouse.MouseUp -= OnMouseUp;
            _mouse.MouseMove -= OnMouseMove;
            _mouse.Scroll -= OnMouseScroll;
        }

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

        canvas.Save();
        canvas.Scale(scaleX, scaleY);

        // Overlay: render underlying screen first so overlay dims on top of it
        if (_screens.Count > 1 && _screens.UnderCurrent is { } under)
        {
            under.Render(canvas, windowSize.X, windowSize.Y);
        }

        _screens.Current.Render(canvas, windowSize.X, windowSize.Y);

        if (isFirstFrame)
            InterfaceLog.Write($"STARTUP DIAG: first-frame screen.Render (recording) done — {diagSw!.ElapsedMilliseconds} ms into OnRender");

        canvas.Restore();
        canvas.Flush();

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

        _isFocused = isFocused;

        // Only cover the taskbar (see WindowOptions.TopMost above) while the game
        // itself is the foreground window. Keeping TopMost on while unfocused would
        // pin the game over whatever the user switched to — e.g. the Snipping Tool
        // that Print Screen just opened, or an alt-tabbed app.
        _window.TopMost = isFocused;

        if (isFocused)
        {
            _focusRegainedAtMs = Environment.TickCount64;
            _keyboardEdges.ResyncToCurrentState(_keyboard);
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
        if (_closing || (button != MouseButton.Left && button != MouseButton.Right))
            return;

        var screenEvent = _screens.Current.OnMouseDown(mouse.Position.X, mouse.Position.Y, button);
        await HandleScreenEvent(screenEvent);
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        if (button == MouseButton.Left && !_closing)
            _screens.Current.OnMouseUp(mouse.Position.X, mouse.Position.Y);
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        if (_closing)
            return;

        bool overInteractive = _screens.Current.OnMouseMove(position.X, position.Y);

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

        var screenEvent = _screens.Current.OnMouseWheel(
            mouse.Position.X,
            mouse.Position.Y,
            scroll.Y);

        _ = HandleScreenEvent(screenEvent);
    }

    private Task? _pendingKeyboardTransition;

    private void PollKeyboard()
    {
        if (_keyboard is null || _closing || !_isFocused)
            return;

        var (pressedCount, releasedCount) = _keyboardEdges.PollBoth(
            _keyboard, _keyboardPressedKeys, _keyboardReleasedKeys);

        if (pressedCount > 0 || releasedCount > 0)
        {
            InterfaceLog.Write(
                $"DIAG PollKeyboard pressed=[{string.Join(',', _keyboardPressedKeys[..pressedCount])}] " +
                $"released=[{string.Join(',', _keyboardReleasedKeys[..releasedCount])}] " +
                $"screen={_screens.Current.GetType().Name} depth={_screens.Count} " +
                $"msSinceFocusRegain={Environment.TickCount64 - _focusRegainedAtMs} " +
                $"printScreenDown={_keyboard.IsKeyPressed(Key.PrintScreen)}");
        }

        for (int i = 0; i < pressedCount; i++)
            _handleKeyboardEdge(_keyboardPressedKeys[i]);

        for (int i = 0; i < releasedCount; i++)
            _screens.Current.OnKeyUp(_keyboardReleasedKeys[i]);
    }

    private void HandleKeyboardEdge(Key key)
    {
        if (key == Key.F10)
        {
            CaptureScreenshot();
            return;
        }

        if (key == Key.Escape || key == Key.F5 || key == Key.F9)
        {
            if (_pendingKeyboardTransition is null || _pendingKeyboardTransition.IsCompleted)
            {
                var screenEvent = _screens.Current.OnKeyDown(key);
                _pendingKeyboardTransition = HandleScreenEvent(screenEvent);
            }

            return;
        }

        _screens.Current.OnKeyDown(key);
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
            Directory.CreateDirectory(ScreenshotsDirectory);

            string fileName = $"screenshot_{DateTime.UtcNow:yyyy-MM-dd'T'HH-mm-ss-fff'Z'}.png";
            string path = Path.Combine(ScreenshotsDirectory, fileName);

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
    private async Task HandleScreenEvent(ScreenEvent evt)
    {
        if (evt != ScreenEvent.None)
        {
            InterfaceLog.Write(
                $"DIAG HandleScreenEvent evt={evt} screen={_screens.Current.GetType().Name} depth={_screens.Count}");
        }

        await _transitionLock.WaitAsync();
        try
        {
            switch (evt)
            {
                case ScreenEvent.NewGame:
                    StartGameSession();
                    break;
                case ScreenEvent.OpenGameMenu:
                    await OpenGameMenuAsync();
                    break;
                case ScreenEvent.Resume:
                    await CloseOverlayAsync();
                    break;
                case ScreenEvent.MainMenu:
                    ReturnToMainMenu();
                    break;
                case ScreenEvent.Exit:
                    _window.Close();
                    break;
                case ScreenEvent.QuickSave:
                    await QuickSaveAsync();
                    break;
                case ScreenEvent.QuickLoad:
                    await QuickLoadAsync();
                    break;
                case ScreenEvent.OpenSettings:
                    await OpenSettingsAsync();
                    break;
                case ScreenEvent.CloseSettings:
                    await CloseOverlayAsync();
                    break;
            }
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    private void StartGameSession()
    {
        if (_session is not null)
            return;

        _session = new GameSessionHandle(_sessionFactory.CreateSession());
        var predictor = new LinearMotionPredictor();
        var gameScreen = new GameSessionScreen(_session.Buffer, predictor, _session,
            showTrajectoryPrediction: GetShowTrajectoryPrediction(),
            uiScale: (float)GetUiScale());

        _gameSessionScreen = gameScreen;
        _modalDepth = 0;
        _savedSpeed = SimulationSpeed.Speed1;
        _screens.Replace(gameScreen);
    }

    private static bool GetShowTrajectoryPrediction()
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
    private async Task PushModalAsync(IScreen screen)
    {
        // First modal: confirm authoritative Pause before showing anything
        if (_modalDepth == 0 && _session is not null)
        {
            _savedSpeed = _session.Buffer.CurrentSpeed;
            await _session.SetSpeedAsync(SimulationSpeed.Speed0);
            // Speed0 is now confirmed — safe to show the overlay
        }

        _screens.Push(screen);
        _modalDepth++;
    }

    /// <summary>
    /// Pop the current modal screen.
    /// Last modal awaits the authoritative speed restore before returning.
    /// </summary>
    private async Task PopModalAsync()
    {
        _screens.Pop();
        _modalDepth--;

        // Last modal closed: restore previous simulation speed
        if (_modalDepth == 0 && _session is not null)
        {
            await _session.SetSpeedAsync(_savedSpeed);
        }
    }

    private async Task OpenGameMenuAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is GameMenuScreen)
            return;

        long msSinceFocusRegain = Environment.TickCount64 - _focusRegainedAtMs;
        if (msSinceFocusRegain < MenuOpenFocusRegainGuardMs)
        {
            // Keep the menu hidden: an Escape landing this soon after the window
            // regained OS focus is almost always fallout from whatever stole focus
            // (e.g. Windows' Print Screen -> Snipping Tool) rather than a deliberate
            // request to pause and open the menu. A real Escape press a beat later
            // still opens it normally.
            InterfaceLog.Write($"DIAG OpenGameMenu kept hidden msSinceFocusRegain={msSinceFocusRegain}");
            return;
        }

        await PushModalAsync(new GameMenuScreen());
    }

    private async Task OpenSettingsAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is SettingsScreen)
            return;

        var monitors = Silk.NET.Windowing.Monitor.GetMonitors(_window).ToArray();
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

        await PushModalAsync(new SettingsScreen(
            monitorNames, selectedMonitorIndex, SaveSelectedMonitorIndex,
            uiScale, SaveUiScale));
    }

    private static string SettingsFilePath => Path.Combine(AppContext.BaseDirectory, "Settings.json");

    private static int GetSelectedMonitorIndex()
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
    private static void SaveSelectedMonitorIndex(int index)
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

    private static double GetUiScale()
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
    private async Task QuickSaveAsync()
    {
        if (_session is null || _quickSaveLoadInFlight)
            return;

        _quickSaveLoadInFlight = true;
        try
        {
            await _session.SetSpeedAsync(SimulationSpeed.Speed0);
            await _session.SaveAsync();
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
    /// F9 — quickload. No modal/UI window is ever shown. Builds the replacement
    /// session/screen fully in local variables first; only on success is the old
    /// session disposed and swapped in — a broken/missing save file never destroys
    /// the currently running session. Per F.19, the new session is forced to Speed0
    /// regardless of the speed recorded in the save file. Camera/zoom/focus are never
    /// restored — GameSessionScreen always starts with its default camera, which falls
    /// out naturally from constructing a brand new screen instance.
    /// Debounced: a load already in flight makes a new F9 a no-op.
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
                return;
            }

            GameSessionHandle newSession;
            GameSessionScreen newScreen;
            try
            {
                newSession = new GameSessionHandle(_sessionFactory.CreateSessionFromSave());
                var predictor = new LinearMotionPredictor();
                newScreen = new GameSessionScreen(newSession.Buffer, predictor, newSession,
                    showTrajectoryPrediction: GetShowTrajectoryPrediction(),
                    uiScale: (float)GetUiScale());
            }
            catch (Exception ex)
            {
                InterfaceLog.Write($"QuickLoad failed: {ex.Message}");
                return;
            }

            var oldSession = _session;
            if (oldSession is not null)
                await oldSession.DisposeAsync();

            _session = newSession;
            _gameSessionScreen = newScreen;
            await newSession.SetSpeedAsync(SimulationSpeed.Speed0);
            _modalDepth = 0;
            _savedSpeed = SimulationSpeed.Speed0;
            _screens.Replace(newScreen);

            InterfaceLog.Write("QuickLoad: loaded Saves/quicksave.json");
        }
        finally
        {
            _quickSaveLoadInFlight = false;
        }
    }

    private void ReturnToMainMenu()
    {
        // End the game session explicitly — no resume needed
        _ = _session?.DisposeAsync();
        _session = null;
        _gameSessionScreen = null;

        _modalDepth = 0;
        _savedSpeed = SimulationSpeed.Speed1;

        var mainMenu = new MainMenuScreen();
        _screens.ReplaceAll(mainMenu);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

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

            _input?.Dispose();
            _input = null;
        }

        _ = _session?.DisposeAsync();
        _session = null;

        _screens.DeactivateAll();

        _surface?.Dispose();
        _renderTarget?.Dispose();
        _grContext?.Dispose();
        _gl?.Dispose();
        _window.Dispose();
    }
}
