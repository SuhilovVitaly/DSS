using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Finance;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.Save;
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
    private readonly Key[] _keyboardPressedKeys = new Key[20]; // must cover every key KeyboardEdgeTracker.PollBoth can report in one call
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

        // Capture the current screen and (if relevant) its click payload synchronously,
        // in this same call frame, before HandleScreenEvent's first await. This is what
        // makes ScreenEvent.LoadSlotRequested's slot id race-safe: even if a rapid
        // double-click queues two of these calls back-to-back, each one's payload is
        // read here — while _screens.Current is still guaranteed to be the LoadScreen
        // that produced it — never re-derived later from (possibly stale) screen state
        // inside the locked HandleScreenEvent switch. See LoadScreen's doc comment.
        var currentScreen = _screens.Current;
        var screenEvent = currentScreen.OnMouseDown(mouse.Position.X, mouse.Position.Y, button);
        string? payload = screenEvent == ScreenEvent.LoadSlotRequested && currentScreen is LoadScreen loadScreen
            ? loadScreen.LastRequestedSlotId
            : null;

        await HandleScreenEvent(screenEvent, payload);
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

    private void OnKeyChar(IKeyboard keyboard, char c)
    {
        if (_closing)
            return;

        _screens.Current.OnTextInput(c);
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

        if (key == Key.Escape || key == Key.F5 || key == Key.F9 || key == Key.F)
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
                    await CloseOverlayAsync();
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

    /// <summary>
    /// Push the Finance overlay (Docs/FirstRelease/Screens/Finance.md). Opened via the
    /// bottom-center Finance panel button or Ctrl+F. Uses the same generic
    /// PushModalAsync pause-on-open behavior as every other modal — no Finance-specific
    /// speed logic needed.
    /// </summary>
    private async Task OpenFinanceAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is FinanceScreen)
            return;

        await PushModalAsync(new FinanceScreen());
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
        if (_session is null)
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
    /// The reserved quicksave slot is filtered out of the list here exactly like
    /// <see cref="OpenSaveWindowAsync"/> does, so a player can never load/delete it from
    /// this window and silently interfere with F9 quickload.
    /// </summary>
    private async Task OpenLoadWindowAsync()
    {
        // Guard: don't push overlay on top of another overlay
        if (_screens.Current is LoadScreen)
            return;

        var loadScreen = new LoadScreen(
            () => SaveSlots.ExcludeReserved(_sessionFactory.ListSaveSlots()),
            DeleteSaveSlotSafely);

        await PushModalAsync(loadScreen);
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
                return;
            }

            if (await LoadSlotCoreAsync(SaveSlots.Quicksave, "QuickLoad"))
                InterfaceLog.Write("QuickLoad: loaded Saves/quicksave.json");
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
        GameSessionHandle newSession;
        GameSessionScreen newScreen;
        try
        {
            newSession = new GameSessionHandle(_sessionFactory.CreateSessionFromSave(slotId));
            var predictor = new LinearMotionPredictor();
            newScreen = new GameSessionScreen(newSession.Buffer, predictor, newSession,
                showTrajectoryPrediction: GetShowTrajectoryPrediction(),
                uiScale: (float)GetUiScale());
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"{logPrefix} failed: {ex.Message}");
            return false;
        }

        var oldSession = _session;
        if (oldSession is not null)
            await oldSession.DisposeAsync();

        _session = newSession;
        _gameSessionScreen = newScreen;
        await newSession.SetSpeedAsync(SimulationSpeed.Speed0);
        _modalDepth = 0;
        _savedSpeed = SimulationSpeed.Speed0;
        _screens.ReplaceAll(newScreen);

        return true;
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

            if (_keyboard is not null)
                _keyboard.KeyChar -= OnKeyChar;

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
