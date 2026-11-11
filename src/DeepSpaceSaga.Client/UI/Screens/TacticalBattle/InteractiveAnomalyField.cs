using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>One render-thread image and one bounded background raster for the live camera.</summary>
internal sealed class InteractiveAnomalyField : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SKPaint _paint = new() { BlendMode = SKBlendMode.Screen };
    private Task<FieldImage?>? _pending;
    private bool _disposed;
    private FieldImage? _displayed;
    private Request? _wanted;
    private long _changedAt;
    private SKRect _destination;

    private sealed record Request(ImmutableArray<TacticalAnomalySnapshot> Anomalies,
        TacticalAnomalyRulesSnapshot Rules, TacticalBattleView View, float Scale)
    {
        public bool SameField(Request other) =>
            Rules == other.Rules && AnomalyFieldLayer.Same(Anomalies, other.Anomalies);
    }

    private sealed record FieldImage(Request Request, TacticalBattleView Extent, SKImage Image, bool FullResolution);

    internal bool Prepare(SKCanvas canvas, TacticalBattleFrame frame)
    {
        if (_disposed) return false;
        if (frame.Battle.Anomalies.IsDefaultOrEmpty || frame.Battle.Rules.Anomalies is not { } rules) return false;
        var request = new Request(frame.Battle.Anomalies, rules, frame.View, Math.Abs(canvas.TotalMatrix.ScaleX));
        long now = Environment.TickCount64;
        if (_wanted is not { } previous || previous.View != request.View || previous.Scale != request.Scale || !previous.SameField(request))
        {
            _wanted = request;
            _changedAt = now;
        }
        if (_pending is { IsCompleted: true } completed)
        {
            _pending = null;
            var ready = completed.GetAwaiter().GetResult();
            if (ready is not null)
            {
                if (ready.Request.SameField(request)) Replace(ready);
                else ready.Image.Dispose();
            }
        }

        bool newField = _displayed is null || !_displayed.Request.SameField(request);
        if (newField || !CoversViewport(Project(_displayed!.Extent, request.View), request.View))
        {
            if (Build(request, preview: true, _lifetime.Token) is not { } preview) return false;
            Replace(preview);
        }

        // Refine once the camera settles. During movement the cached texture
        // stays anchored in world coordinates and the overscan covers drag edges.
        var displayed = _displayed!;
        bool exact = displayed.FullResolution && displayed.Request.View == request.View && displayed.Request.Scale == request.Scale;
        if (_pending is null && !exact && (newField || now - _changedAt >= 90))
        {
            var token = _lifetime.Token;
            // The worker returns null on normal cancellation, even if exit wins
            // the race before it starts. Do not cancel the Task itself.
            _pending = Task.Run(() => Build(request, preview: false, token));
        }

        _destination = Project(displayed.Extent, request.View);
        _paint.FilterQuality = exact ? SKFilterQuality.None : SKFilterQuality.Low;
        return true;
    }

    internal void Draw(SKCanvas canvas) => canvas.DrawImage(_displayed!.Image, _destination, _paint);

    private static SKRect Project(TacticalBattleView extent, TacticalBattleView view)
    {
        double halfWidth = extent.PixelWidth * .5 / (double)extent.PixelsPerUnit;
        double halfHeight = extent.PixelHeight * .5 / (double)extent.PixelsPerUnit;
        var leftTop = view.Project(extent.CenterX - halfWidth, extent.CenterY - halfHeight);
        var rightBottom = view.Project(extent.CenterX + halfWidth, extent.CenterY + halfHeight);
        return new(leftTop.X, leftTop.Y, rightBottom.X, rightBottom.Y);
    }

    private static bool CoversViewport(SKRect destination, TacticalBattleView view) =>
        destination.Left <= 0 && destination.Top <= 0 && destination.Right >= view.PixelWidth && destination.Bottom >= view.PixelHeight;

    private static FieldImage? Build(Request request, bool preview, CancellationToken token)
    {
        const int margin = 96;
        var view = request.View with
        {
            PixelWidth = request.View.PixelWidth + margin * 2,
            PixelHeight = request.View.PixelHeight + margin * 2
        };
        float scale = preview ? (float)PyMath.Min(request.Scale, .25) : request.Scale;
        var size = new SKSizeI((int)Math.Round(view.PixelWidth * scale), (int)Math.Round(view.PixelHeight * scale));
        var image = AnomalyFieldLayer.Build(request.Anomalies, request.Rules, view, size, scale,
            new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = preview ? 1 : 2 }, request.Scale);
        return image is null ? null : new(request, view, image, !preview);
    }

    private void Replace(FieldImage next)
    {
        _displayed?.Image.Dispose();
        _displayed = next;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        if (_pending is { } pending)
            _ = pending.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully) task.Result?.Image.Dispose();
                else _ = task.Exception;
                _lifetime.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        else _lifetime.Dispose();
        _displayed?.Image.Dispose();
        _displayed = null;
        _paint.Dispose();
    }
}
