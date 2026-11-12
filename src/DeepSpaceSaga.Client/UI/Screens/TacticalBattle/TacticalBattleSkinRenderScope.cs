using System.Collections.Immutable;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal readonly record struct ShipSheet(SKImage Image, int Columns, int Frames, int Cell);
internal readonly record struct EffectStrip(SKImage Image, int Frames, int Cell, float FrameMs);

/// <summary>Style inputs for the current synchronous render on this thread.</summary>
internal static class TacticalBattleSkinRenderScope
{
    [ThreadStatic] private static TacticalBattleSkin? _skin;
    [ThreadStatic] private static string? _role;

    internal static TacticalBattleSkin? Current => _skin;
    internal static string? CurrentRole => _role;
    internal static bool Tabletop => _skin?.Metric("field.style", "tabletop", 0) == 1;

    internal static ShipSheet? ShipSheet(string model, string side)
    {
        if (!Tabletop || _skin is null) return null;
        string role = $"field.unit.sprite.{model}.{side}";
        var image = _skin.Image(role);
        float columns = _skin.Metric(role, "columns", 0), frames = _skin.Metric(role, "frames", 0);
        if (image is null || !PositiveInteger(columns) || !PositiveInteger(frames) ||
            frames % columns != 0 || image.Width % (int)columns != 0) return null;
        int cell = image.Width / (int)columns;
        return cell > 0 && image.Height == cell * ((int)frames / (int)columns)
            ? new(image, (int)columns, (int)frames, cell) : null;
    }

    internal static EffectStrip? EffectStrip(string name)
    {
        if (!Tabletop || _skin is null) return null;
        string role = "field.fx." + name;
        var image = _skin.Image(role);
        float frames = _skin.Metric(role, "frames", 0), ms = _skin.Metric(role, "frame-ms", 0);
        if (image is null || !PositiveInteger(frames) || image.Width != image.Height * (int)frames ||
            !float.IsFinite(ms) || (name == "trail" ? ms < 0 : ms <= 0)) return null;
        return new(image, (int)frames, image.Height, ms);
    }

    private static bool PositiveInteger(float value) => value is >= 1 and <= 512 && value == MathF.Truncate(value);

    internal static Restore Enter(TacticalBattleSkin skin)
    {
        var restore = new Restore(_skin, _role);
        _skin = skin;
        _role = null;
        return restore;
    }

    internal static Restore EnterRole(string role)
    {
        var restore = new Restore(_skin, _role);
        _role = role;
        return restore;
    }

    internal readonly record struct Restore(TacticalBattleSkin? Skin, string? Role) : IDisposable
    {
        public void Dispose()
        {
            _skin = Skin;
            _role = Role;
        }
    }

    internal readonly record struct Role(string Primary, string? Shared = null, string? ScopeRoot = null)
    {
        public static implicit operator Role(string role) => new(role);
    }

    internal static Role Control(string shared, string? detail = null)
    {
        string fallback = detail is null ? shared : $"{shared}.{detail}";
        if (_role is null)
            return new(fallback);
        return new(detail is null ? _role : $"{_role}.{detail}", fallback, _role);
    }

    private static T Value<T>(Role role, string property, T fallback)
    {
        if (_skin is null || ReferenceEquals(_skin, TacticalBattleSkin.Classic))
            return fallback;
        if (role.Shared is not null)
            fallback = _skin.Find(role.Shared, property, fallback);
        return _skin.Find(role.Primary, property, fallback, role.ScopeRoot);
    }

    internal static bool Has(Role role, string property) => Value<object?>(role, property, null) is not null;

    internal static float Number(Role role, string property, float fallback) => Value(role, property, fallback);

    internal static SKColor Color(Role role, string property, SKColor fallback)
    {
        var color = Value(role, property, fallback);
        float opacity = Number(role, "opacity", 1);
        return opacity == 1 ? color : color.WithAlpha((byte)MathF.Round(color.Alpha * opacity));
    }

    internal static ImmutableArray<float> Dash(Role role) => Value(role, "dash", default(ImmutableArray<float>));

    internal static bool Image(SKCanvas canvas, Role role, SKRect bounds, byte alpha = 255)
    {
        var image = _skin?.LoadedResource<SKImage>(Value<string?>(role, "image", null));
        if (image is null || bounds.IsEmpty)
            return false;

        using var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha((byte)MathF.Round(alpha * Number(role, "opacity", 1))),
            FilterQuality = SKFilterQuality.Low,
        };
        canvas.DrawImage(image, bounds, paint);
        return true;
    }

    internal static string Shape(Role role) => Value(role, "shape", "native");

    internal static SKPathEffect? Stroke(SKPaint paint, Role role, SKColor color, float width, float scale, float[]? fallbackDash = null)
    {
        paint.Color = Color(role, "stroke", color);
        paint.StrokeWidth = Has(role, "strokeWidth") ? Number(role, "strokeWidth", 1) * scale : width;
        var pattern = Dash(role);
        var lengths = pattern.IsDefault ? fallbackDash : pattern.Select(length => length * scale).ToArray();
        var effect = lengths is { Length: > 0 } ? SKPathEffect.CreateDash(lengths, 0) : null;
        paint.PathEffect = effect;
        return effect;
    }

    internal static bool HasBoxStyle(Role role) =>
        Has(role, "fill") || Has(role, "stroke") || Has(role, "strokeWidth") ||
        Has(role, "image") || Has(role, "radius") || Has(role, "dash") || Has(role, "opacity");

    internal static void Box(SKCanvas canvas, Role role, SKRect bounds, SKColor fill, SKColor? border,
        float scale, bool antialias = true)
    {
        if (bounds.IsEmpty)
            return;

        using var paint = new SKPaint { IsAntialias = antialias, Color = Color(role, "fill", fill) };
        float radius = MathF.Min(Number(role, "radius", 0) * scale, MathF.Min(bounds.Width, bounds.Height) * 0.5f);
        if (radius > 0)
            canvas.DrawRoundRect(bounds, radius, radius, paint);
        else
            canvas.DrawRect(bounds, paint);

        if (Has(role, "image"))
        {
            canvas.Save();
            if (radius > 0)
            {
                using var clip = new SKRoundRect(bounds, radius, radius);
                canvas.ClipRoundRect(clip, antialias: true);
            }
            Image(canvas, role, bounds);
            canvas.Restore();
        }

        if (border is null && !Has(role, "stroke"))
            return;
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = Color(role, "stroke", border ?? SKColors.Transparent);
        paint.StrokeWidth = MathF.Min(Number(role, "strokeWidth", 1) * scale, MathF.Min(bounds.Width, bounds.Height) * 0.5f);
        var pattern = Dash(role);
        using var effect = pattern.IsDefaultOrEmpty ? null : SKPathEffect.CreateDash(pattern.Select(length => length * scale).ToArray(), 0);
        paint.PathEffect = effect;
        if (radius > 0)
            canvas.DrawRoundRect(bounds, radius, radius, paint);
        else
            canvas.DrawRect(bounds, paint);
    }

    internal static void Text(SKPaint paint, Role role, float scale, string? text)
    {
        paint.Color = Color(role, "text", paint.Color);
        if (Has(role, "fontSize")) paint.TextSize = Number(role, "fontSize", 14) * scale;
        var typeface = _skin?.LoadedResource<SKTypeface>(Value<string?>(role, "font", null));
        if (typeface is null || string.IsNullOrEmpty(text))
            return;
        var fallback = paint.Typeface;
        paint.Typeface = typeface;
        // Layout treats CR/LF and tab as separators, so they need no glyph in the font.
        bool missing = text.AsSpan().IndexOfAny('\r', '\n', '\t') < 0
            ? MissingGlyphs(text)
            : text.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Any(MissingGlyphs);
        if (missing)
            paint.Typeface = fallback;

        bool MissingGlyphs(string line) => paint.GetGlyphs(line).Any(glyph => glyph == 0);
    }
}
