using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace DeepSpaceSaga.Client;

public sealed class SkiaWindow : IDisposable
{
    private readonly IWindow _window;
    private readonly IGameSessionFactory _sessionFactory;

    private GL? _gl;
    private GRContext? _grContext;
    private GRBackendRenderTarget? _renderTarget;
    private SKSurface? _surface;

    private IInputContext? _input;
    private IMouse? _mouse;

    private IScreen _currentScreen;
    private bool _disposed;

    public SkiaWindow(IScreen initialScreen, IGameSessionFactory sessionFactory)
    {
        _currentScreen = initialScreen;
        _sessionFactory = sessionFactory;

        var options = WindowOptions.Default with
        {
            Title = "Deep Space Saga",
            WindowBorder = WindowBorder.Hidden,
            WindowState = WindowState.Fullscreen,
            FramesPerSecond = 80,
            VSync = false,
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3))
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
    }

    public void Run()
    {
        _window.Run();
    }

    private void OnLoad()
    {
        _gl = _window.CreateOpenGL();

        var glInterface = GRGlInterface.Create();
        glInterface.Validate();
        _grContext = GRContext.CreateGl(glInterface);

        // Do NOT create Skia surface yet — Silk.NET fullscreen transition
        // may not be complete. Surface is created lazily in first OnRender.

        // Initialize input
        _input = _window.CreateInput();
        _mouse = _input.Mice.FirstOrDefault();
        if (_mouse is not null)
        {
            _mouse.MouseDown += OnMouseDown;
            _mouse.MouseMove += OnMouseMove;
        }

        _currentScreen.OnActivated();
    }

    private void OnRender(double deltaTime)
    {
        if (_grContext is null || _gl is null)
            return;

        // Lazy surface creation — only after window/framebuffer is stable
        if (_surface is null)
        {
            CreateRenderSurface();
            if (_surface is null)
                return;
        }

        var canvas = _surface.Canvas;
        _currentScreen.Render(canvas, _window.FramebufferSize.X, _window.FramebufferSize.Y);
        canvas.Flush();
    }

    private void OnFramebufferResize(Silk.NET.Maths.Vector2D<int> newSize)
    {
        // Only recreate if surface already exists; otherwise first OnRender handles it
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

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button != MouseButton.Left)
            return;

        var screenEvent = _currentScreen.OnMouseDown(mouse.Position.X, mouse.Position.Y);
        HandleScreenEvent(screenEvent);
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        _currentScreen.OnMouseMove(position.X, position.Y);
    }

    private void HandleScreenEvent(ScreenEvent evt)
    {
        switch (evt)
        {
            case ScreenEvent.NewGame:
                SwitchToGameSession();
                break;
            case ScreenEvent.Exit:
                _window.Close();
                break;
        }
    }

    private void SwitchToGameSession()
    {
        if (_currentScreen is GameSessionScreen)
            return;

        _currentScreen.OnDeactivated();

        var sessionConnection = _sessionFactory.CreateSession();
        var gameScreen = new GameSessionScreen(sessionConnection);

        _currentScreen = gameScreen;
        _currentScreen.OnActivated();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_mouse is not null)
        {
            _mouse.MouseDown -= OnMouseDown;
            _mouse.MouseMove -= OnMouseMove;
        }

        _input?.Dispose();
        _currentScreen.OnDeactivated();
        _surface?.Dispose();
        _renderTarget?.Dispose();
        _grContext?.Dispose();
        _gl?.Dispose();
        _window.Dispose();
    }
}
