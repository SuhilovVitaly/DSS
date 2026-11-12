using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>Painted presentation of authoritative anomaly clumps, in design coordinates.</summary>
internal sealed class PaintedCloudLayer : IDisposable
{
    private SKImage? _image;
    private TacticalBattleView _view;
    private float _scale;
    private GRContext? _context;
    private ImmutableArray<TacticalAnomalySnapshot> _source = [];
    internal int BuildCount { get; private set; }
    internal bool HasImage => _image is not null;

    public void Draw(SKCanvas canvas, TacticalBattleFrame frame, GRContext? context = null)
    {
        if (frame.Battle.Anomalies.IsDefaultOrEmpty) { Dispose(); return; }
        float scale = TacticalShipSprites.CanvasScale(canvas);
        if (!float.IsFinite(scale) || scale <= 0 || frame.View.PixelWidth <= 0 || frame.View.PixelHeight <= 0) return;
        if (_image is null || _view != frame.View || _scale != scale || !ReferenceEquals(_context, context) ||
            !AnomalyFieldLayer.Same(_source, frame.Battle.Anomalies))
        {
            var size = new SKImageInfo((int)MathF.Ceiling(frame.View.PixelWidth * scale), (int)MathF.Ceiling(frame.View.PixelHeight * scale), SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = context is null ? SKSurface.Create(size) : SKSurface.Create(context, false, size);
            surface.Canvas.Clear(SKColors.Transparent);
            surface.Canvas.Scale(scale);
            Paint(surface.Canvas, frame);
            var next = surface.Snapshot();
            _image?.Dispose(); _image = next;
            _view = frame.View; _scale = scale; _context = context; _source = frame.Battle.Anomalies;
            BuildCount++;
        }
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.None };
        canvas.DrawImage(_image, SKRect.Create(frame.View.PixelWidth, frame.View.PixelHeight), paint);
    }

    public void Dispose()
    {
        _image?.Dispose(); _image = null; _source = []; _context = null;
    }

    internal static SKColorFilter NoiseAlpha(float gain, float bias) => SKColorFilter.CreateColorMatrix([
        0, 0, 0, 0, 0,
        0, 0, 0, 0, 0,
        0, 0, 0, 0, 0,
        gain, 0, 0, 0, bias]);

    private static void Paint(SKCanvas canvas, TacticalBattleFrame frame)
    {
        if (frame.Battle.Anomalies.IsDefaultOrEmpty) return;
        foreach (var kind in new[] { TacticalAnomalyKindCode.Dust, TacticalAnomalyKindCode.Heat })
        {
            var clumps = frame.Battle.Anomalies.Where(a => a.Kind == kind).SelectMany(a => a.Clumps).ToArray();
            if (clumps.Length == 0) continue;
            var tones = kind == TacticalAnomalyKindCode.Dust
                ? new[] { SKColor.Parse("#4E3E72"), SKColor.Parse("#8A70B4"), SKColor.Parse("#B8A4DC") }
                : new[] { SKColor.Parse("#9A3A2A"), SKColor.Parse("#D0684A"), SKColor.Parse("#F0A070") };
            Layer(false); Layer(true);

            void Layer(bool core)
            {
                canvas.SaveLayer();
                try
                {
                    using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, core ? 11 : 23);
                    using var paint = new SKPaint { IsAntialias = true, MaskFilter = blur };
                    foreach (var clump in clumps)
                    {
                        var point = frame.View.Project(clump.X, clump.Y);
                        canvas.Save();
                        canvas.Translate(point);
                        canvas.RotateRadians((float)clump.Rotation);
                        Ellipse(core ? .75f : 1.25f, tones[core ? 1 : 0], core ? .75f : .55f);
                        if (core && clump.Amplitude > 110) Ellipse(.35f, tones[2], .55f);
                        canvas.Restore();
                        void Ellipse(float factor, SKColor color, float alpha)
                        {
                            paint.Color = color.WithAlpha((byte)MathF.Round(alpha * 255));
                            canvas.DrawOval(0, 0, (float)(2 * clump.SigmaX * factor * frame.View.PixelsPerUnit),
                                (float)(2 * clump.SigmaY * factor * frame.View.PixelsPerUnit), paint);
                        }
                    }
                    Noise(core ? .075f : .033f, 3, core ? 8 : 5, core ? 1.6f : 1.4f, core ? -.35f : -.25f, SKBlendMode.DstIn);
                    if (core) Noise(.75f, 1, 2, .45f, -.1f, SKBlendMode.SrcATop);
                }
                finally { canvas.Restore(); }
            }
            void Noise(float frequency, int octaves, float seed, float gain, float bias, SKBlendMode blend)
            {
                using var noise = SKShader.CreatePerlinNoiseFractalNoise(frequency, frequency, octaves, seed);
                using var filter = NoiseAlpha(gain, bias);
                using var paint = new SKPaint { Shader = noise, ColorFilter = filter, BlendMode = blend };
                canvas.DrawRect(SKRect.Create(frame.View.PixelWidth, frame.View.PixelHeight), paint);
            }
        }
    }
}
