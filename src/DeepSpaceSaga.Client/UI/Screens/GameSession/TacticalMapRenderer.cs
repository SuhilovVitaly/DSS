using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Read-only consumer: native replay and UI animation only. No session, predictor, layout or I/O dependency.</summary>
internal sealed class TacticalMapRenderer : IDisposable
{
    private readonly TacticalMapDepthRenderer _depth = new();
    private readonly SKPaint _status = new() { IsAntialias = true };
    private bool _disposed;

    internal void Draw(SKCanvas canvas, TacticalMapSceneGeometry scene, double uiTimeSeconds,
        Action<string>? stageObserver = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var paint = scene.PaintCommands ?? throw new InvalidOperationException("Scene paint commands were not prepared.");
        ObjectDisposedException.ThrowIf(paint.IsDisposed, paint);
        long uiTimeMs = (long)Math.Round(Math.Max(0, uiTimeSeconds) * 1000);
        int saveCount = canvas.Save();
        try
        {
            canvas.ClipRect(SKRect.Create(scene.View.Width, scene.View.Height));
            foreach (var command in paint.Commands)
            {
                if (command.Picture is { } picture) canvas.DrawPicture(picture);
                else if (command.Stage is { } stage) stageObserver?.Invoke(stage);
                else if (command.Animation is { } animation)
                {
                    switch (animation.Kind)
                    {
                        case TacticalMapAnimationKind.Selection:
                            _depth.DrawSelectionReticle(canvas, animation.X, animation.Y, animation.Radius, uiTimeMs);
                            break;
                        case TacticalMapAnimationKind.Active:
                            _depth.DrawActiveObjectReticle(canvas, animation.X, animation.Y, animation.Radius, uiTimeMs);
                            break;
                        case TacticalMapAnimationKind.EngineFlame:
                            _depth.DrawEngineFlame(canvas, animation.X, animation.Y, animation.Direction, animation.Radius, uiTimeMs);
                            break;
                        case TacticalMapAnimationKind.Status:
                            if (StatusSquareAnimator.IsStatusSquareVisible(uiTimeMs, animation.Speed))
                            {
                                _status.Color = animation.Color;
                                canvas.DrawRect(animation.Rect, _status);
                            }
                            break;
                    }
                }
            }
        }
        finally { canvas.RestoreToCount(saveCount); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _depth.Dispose();
        _status.Dispose();
    }
}
