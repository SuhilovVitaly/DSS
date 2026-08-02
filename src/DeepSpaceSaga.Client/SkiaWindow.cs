using DeepSpaceSaga.Contracts;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace DeepSpaceSaga.Client;

public sealed class SkiaWindow : IDisposable
{
    private readonly IWindow _window;
    private readonly IGameSessionConnection _connection;
    private GL? _gl;
    private GRContext? _grContext;
    private GRBackendRenderTarget? _renderTarget;
    private SKSurface? _surface;
    private bool _disposed;

    public SkiaWindow(IGameSessionConnection connection)
    {
        _connection = connection;
        var options = WindowOptions.Default with
        {
            Title = "Deep Space Saga",
            Size = new Silk.NET.Maths.Vector2D<int>(1280, 720),
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

        CreateRenderSurface();
    }

    private void OnRender(double deltaTime)
    {
        if (_surface is null)
            return;

        var canvas = _surface.Canvas;
        canvas.Clear(new SKColor(10, 15, 30)); // dark navy blue space background

        canvas.Flush();
    }

    private void OnFramebufferResize(Silk.NET.Maths.Vector2D<int> newSize)
    {
        CreateRenderSurface();
    }

    private void CreateRenderSurface()
    {
        _surface?.Dispose();
        _renderTarget?.Dispose();

        if (_grContext is null || _gl is null)
            return;

        var size = _window.FramebufferSize;
        if (size.X <= 0 || size.Y <= 0)
            return;

        var framebufferInfo = new GRGlFramebufferInfo(0, SKColorType.Rgba8888.ToGlSizedFormat());
        _renderTarget = new GRBackendRenderTarget(size.X, size.Y, 0, 8, framebufferInfo);
        _surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _surface?.Dispose();
        _renderTarget?.Dispose();
        _grContext?.Dispose();
        _gl?.Dispose();
        _window.Dispose();
    }
}
