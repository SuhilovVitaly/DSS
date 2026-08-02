using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens;
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

    private GL? _gl;
    private GRContext? _grContext;
    private GRBackendRenderTarget? _renderTarget;
    private SKSurface? _surface;

    private IInputContext? _input;
    private IMouse? _mouse;
    private RawImage? _defaultCursorImage;
    private RawImage? _interactiveCursorImage;

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
            WindowState = WindowState.Normal,
            FramesPerSecond = 80,
            VSync = false,
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3))
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
    }

    public void Run() => _window.Run();

    private void OnLoad()
    {
        var monitors = Silk.NET.Windowing.Monitor.GetMonitors(null);
        var target = monitors.FirstOrDefault();
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

        _currentScreen.OnActivated();
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
        if (_grContext is null || _gl is null)
            return;

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
        bool overInteractive = _currentScreen.OnMouseMove(position.X, position.Y);

        var targetImage = overInteractive ? _interactiveCursorImage : _defaultCursorImage;
        if (targetImage is not null)
        {
            mouse.Cursor.Type = CursorType.Custom;
            mouse.Cursor.Image = targetImage.Value;
        }
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
