using System.Collections.Immutable;
using System.Runtime.InteropServices;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>One bounded CPU raster cache. Render returns a borrowed image until the next change/Dispose.</summary>
internal sealed class AnomalyFieldLayer : IDisposable
{
    private static readonly (double T, SKColor Color)[] RedRamp =
        [(0, SKColors.Black), (.15, SKColor.Parse("#16060B")), (.45, SKColor.Parse("#44101F")), (.75, SKColor.Parse("#7A1C32")), (1, SKColor.Parse("#C23A52"))];
    private static readonly (double T, SKColor Color)[] HotRamp =
        [(0, SKColors.Black), (.12, SKColor.Parse("#0E0918")), (.40, SKColor.Parse("#2B1745")), (.70, SKColor.Parse("#4F2A74")), (1, SKColor.Parse("#8A5CC0"))];
    private readonly object _gate = new();
    private ImmutableArray<TacticalAnomalySnapshot> _source = [];
    private TacticalAnomalyRulesSnapshot? _rules;
    private TacticalBattleView _view;
    private SKSizeI _size;
    private float _scale;
    private SKImage? _image;
    internal int BuildCount { get; private set; }

    internal static ImmutableArray<TacticalAnomaly> ToMotion(ImmutableArray<TacticalAnomalySnapshot> source) =>
        source.IsDefaultOrEmpty ? [] : source.Select(a => new TacticalAnomaly(a.Id, (TacticalAnomalyKind)a.Kind,
            a.Clumps.Select(c => new TacticalClump(c.X, c.Y, c.Amplitude, c.SigmaX, c.SigmaY, c.Rotation)).ToImmutableArray())).ToImmutableArray();

    public SKImage? Render(ImmutableArray<TacticalAnomalySnapshot> anomalies, TacticalAnomalyRulesSnapshot? rules,
        TacticalBattleView view, SKSizeI size, float scale)
    {
        lock (_gate)
        {
            if (anomalies.IsDefaultOrEmpty || rules is null) return null;
            if (size.Width < 1 || size.Height < 1 || !float.IsFinite(scale) || scale <= 0 || !double.IsFinite(view.PixelsPerUnit) || view.PixelsPerUnit <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));
            if (_image is not null && _rules == rules && _view == view && _size == size && _scale == scale && Same(_source, anomalies)) return _image;
            var fields = ToMotion(anomalies);
            // Outside every Gaussian's conservative bound the sum cannot reach t>.002,
            // so the exact specified visual contribution is black; skip expensive exponentials there.
            var bounds = new[] { Bounds(TacticalAnomalyKind.Heat), Bounds(TacticalAnomalyKind.Dust) };
            var pixels = new byte[checked(size.Width * size.Height * 4)];
            Parallel.For(0, size.Height, y =>
            {
                for (int x = 0; x < size.Width; x++)
                {
                    double wx = view.CenterX + ((x + .5) / (double)scale - view.PixelWidth * .5) / (double)view.PixelsPerUnit;
                    double wy = view.CenterY + ((y + .5) / (double)scale - view.PixelHeight * .5) / (double)view.PixelsPerUnit;
                    var heat = Color(TacticalAnomalyKind.Heat, wx, wy);
                    var dust = Color(TacticalAnomalyKind.Dust, wx, wy);
                    int i = (y * size.Width + x) * 4;
                    pixels[i] = Byte(1 - (1 - heat.B) * (1 - dust.B));
                    pixels[i + 1] = Byte(1 - (1 - heat.G) * (1 - dust.G));
                    pixels[i + 2] = Byte(1 - (1 - heat.R) * (1 - dust.R)); pixels[i + 3] = 255;
                }
            });
            using var bitmap = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Opaque));
            Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
            var next = SKImage.FromBitmap(bitmap);
            _image?.Dispose(); _image = next; _source = anomalies; _rules = rules; _view = view; _size = size; _scale = scale; BuildCount++;
            return _image;

            (double Left, double Top, double Right, double Bottom)[]? Bounds(TacticalAnomalyKind kind)
            {
                double remaining = 100 * Math.Pow(2, .002) - rules.Background;
                if (remaining <= 0) return null;
                var clumps = fields.Where(a => a.Kind == kind).SelectMany(a => a.Clumps).ToArray();
                if (clumps.Length == 0) return [];
                double epsilon = remaining / (double)clumps.Length;
                return clumps.Where(c => c.Amplitude > epsilon).Select(c =>
                {
                    double radius = Math.Sqrt(2 * Math.Log(c.Amplitude / (double)epsilon));
                    var (sin, cos) = Math.SinCos(c.Rotation);
                    double dx = radius * TacticalGeometry.Hypot(c.SigmaX * cos, c.SigmaY * sin);
                    double dy = radius * TacticalGeometry.Hypot(c.SigmaX * sin, c.SigmaY * cos);
                    return (c.X - dx, c.Y - dy, c.X + dx, c.Y + dy);
                }).ToArray();
            }

            (double R, double G, double B) Color(TacticalAnomalyKind kind, double x, double y)
            {
                var area = bounds[(int)kind];
                if (area is not null)
                {
                    bool inside = false;
                    foreach (var b in area)
                        if (x >= b.Left && x <= b.Right && y >= b.Top && y <= b.Bottom) { inside = true; break; }
                    if (!inside) return (0, 0, 0);
                }
                double field = TacticalAnomalyMath.FieldWithGradient(fields, kind, x, y, rules.Background, out double gx, out double gy);
                double t = PyMath.Clamp(Math.Log(PyMath.Max(field, 100) / 100.0) / (double)Math.Log(2), 0, 1);
                if (t <= .002) return (0, 0, 0);
                bool heat = kind == TacticalAnomalyKind.Heat;
                var ramp = heat ? RedRamp : HotRamp;
                var baseColor = Ramp(ramp, t);
                double texture = heat ? .45 + .75 * (.6 * Noise(x / 4.0, y / 4.0) + .4 * Noise(x / 12.0 + 50, y / 12.0 + 50)) : 1;
                gx /= (double)(view.PixelsPerUnit * scale); gy /= (double)(view.PixelsPerUnit * scale);
                double slope = Math.Atan((heat ? .35 : .5) * TacticalGeometry.Hypot(gx, gy));
                double aspect = Math.Atan2(-gx, gy), sin45 = Math.Sqrt(.5);
                double shade = PyMath.Clamp(sin45 * Math.Cos(slope) + sin45 * Math.Sin(slope) * Math.Cos(315 * Math.PI / 180.0 - aspect), 0, 1);
                double factor = PyMath.Clamp(1 + (shade - sin45) * (heat ? 1.6 : 2.2), .45, 1.5);
                double Channel(double c) => PyMath.Min(1, PyMath.Min(1, c * texture) * factor);
                return (Channel(baseColor.R), Channel(baseColor.G), Channel(baseColor.B));
            }
        }
    }

    internal void Draw(SKCanvas canvas, TacticalBattleFrame frame)
    {
        lock (_gate)
        {
            float scale = Math.Abs(canvas.TotalMatrix.ScaleX);
            var size = new SKSizeI((int)Math.Round(frame.View.PixelWidth * scale), (int)Math.Round(frame.View.PixelHeight * scale));
            if (Render(frame.Battle.Anomalies, frame.Battle.Rules.Anomalies, frame.View, size, scale) is not { } image) return;
            using var paint = new SKPaint { BlendMode = SKBlendMode.Screen };
            canvas.Save();
            try { canvas.Scale(1f / scale); canvas.DrawImage(image, 0, 0, paint); }
            finally { canvas.Restore(); }
        }
    }

    private static bool Same(ImmutableArray<TacticalAnomalySnapshot> a, ImmutableArray<TacticalAnomalySnapshot> b) =>
        a == b || a.Length == b.Length && a.Zip(b).All(p => p.First.Id == p.Second.Id && p.First.Kind == p.Second.Kind && p.First.Clumps.SequenceEqual(p.Second.Clumps));
    private static byte Byte(double channel) => (byte)Math.Round(PyMath.Clamp(channel, 0, 1) * 255);
    private static (double R, double G, double B) Ramp((double T, SKColor Color)[] ramp, double t)
    {
        int i = 1; while (i < ramp.Length - 1 && t > ramp[i].T) i++;
        var a = ramp[i - 1]; var b = ramp[i]; double f = (t - a.T) / (double)(b.T - a.T);
        return ((a.Color.Red + (b.Color.Red - a.Color.Red) * f) / 255.0,
            (a.Color.Green + (b.Color.Green - a.Color.Green) * f) / 255.0,
            (a.Color.Blue + (b.Color.Blue - a.Color.Blue) * f) / 255.0);
    }
    private static double Noise(double x, double y)
    {
        long ix = (long)Math.Floor(x), iy = (long)Math.Floor(y);
        double fx = x - ix, fy = y - iy; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        double a = Hash(ix, iy), b = Hash(ix + 1, iy), c = Hash(ix, iy + 1), d = Hash(ix + 1, iy + 1);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }
    private static double Hash(long i, long j)
    {
        unchecked
        {
            uint h = (uint)(i * 374761393 + j * 668265263 + 1013904223);
            h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
            return (h & 0xFFFFFF) / (double)0xFFFFFF;
        }
    }
    public void Dispose() { lock (_gate) { _image?.Dispose(); _image = null; _source = []; _rules = null; } }
}
