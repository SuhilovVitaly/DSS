using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
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
    private readonly SemaphoreSlim _transitionLock = new(1, 1);
    private int _modalDepth;
    private SimulationSpeed _savedSpeed = SimulationSpeed.Speed1;
    private bool _prevEscPressed;
    private bool _disposed;
    private bool _closing;

    public SkiaWindow(IScreen initialScreen, IGameSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;

        _screens.SetRoot(initialScreen);

        var options = WindowOptions.Default with
        {
            Title = "Deep Space Saga",
            WindowBorder = WindowBorder.Hidden,
            WindowState = WindowState.Normal,
            FramesPerSecond = 80,
            VSync = false,
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3))
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Closing += OnClosing;
    }

    public void Run() => _window.Run();

    private void OnLoad()
    {
        var target = Silk.NET.Windowing.Monitor.GetMainMonitor(null);
        if (target is not null)
        {
            _window.Position = target.Bounds.Origin;
            _window.Size = target.VideoMode.Resolution ?? target.Bounds.Size;
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
            _mouse.MouseMove += OnMouseMove;

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
            _mouse.MouseMove -= OnMouseMove;
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

        if (_surface is null)
        {
            CreateRenderSurface();
            if (_surface is null)
                return;
        }

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

        canvas.Restore();
        canvas.Flush();

        PollKeyboard();
    }

    private void OnFramebufferResize(Silk.NET.Maths.Vector2D<int> newSize)
    {
        if (_surface is not null)
            CreateRenderSurface();
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
        if (button != MouseButton.Left || _closing)
            return;

        var screenEvent = _screens.Current.OnMouseDown(mouse.Position.X, mouse.Position.Y);
        await HandleScreenEvent(screenEvent);
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

    private Task? _pendingKeyboardTransition;

    private void PollKeyboard()
    {
        if (_keyboard is null || _closing)
            return;

        bool escDown = _keyboard.IsKeyPressed(Key.Escape);
        if (escDown && !_prevEscPressed)
        {
            // Ignore keypress if a transition is already in flight
            if (_pendingKeyboardTransition is null || _pendingKeyboardTransition.IsCompleted)
            {
                var screenEvent = _screens.Current.OnKeyDown(Key.Escape);
                _pendingKeyboardTransition = HandleScreenEvent(screenEvent);
            }
        }
        _prevEscPressed = escDown;
    }

    /// <summary>
    /// Serialized navigation transition. Only one screen transition
    /// (modal open/close, new game, main menu) can be in flight at a time.
    /// This prevents races where e.g. MAIN MENU is clicked while a Pause
    /// request is still awaiting confirmation.
    /// </summary>
    private async Task HandleScreenEvent(ScreenEvent evt)
    {
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
        var gameScreen = new GameSessionScreen(_session.Buffer, predictor);

        _modalDepth = 0;
        _savedSpeed = SimulationSpeed.Speed1;
        _screens.Replace(gameScreen);
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

        await PushModalAsync(new GameMenuScreen());
    }

    private async Task CloseOverlayAsync()
    {
        if (_modalDepth <= 0)
            return;

        await PopModalAsync();
    }

    private void ReturnToMainMenu()
    {
        // End the game session explicitly — no resume needed
        _ = _session?.DisposeAsync();
        _session = null;

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
                _mouse.MouseMove -= OnMouseMove;
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
