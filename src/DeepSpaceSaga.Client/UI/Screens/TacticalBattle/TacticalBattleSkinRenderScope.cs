using System.Collections.Immutable;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>Style inputs for the current synchronous render on this thread.</summary>
internal static class TacticalBattleSkinRenderScope
{
    [ThreadStatic] private static TacticalBattleSkin? _skin;
    [ThreadStatic] private static string? _role;

    internal static TacticalBattleSkin? Current => _skin;
    internal static string? CurrentRole => _role;

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

    internal readonly record struct Role(string Primary, string? Shared = null)
    {
        public static implicit operator Role(string role) => new(role);
    }

    internal static Role Control(string shared, string? detail = null)
    {
        string fallback = detail is null ? shared : $"{shared}.{detail}";
        if (_role is null)
            return new(fallback);
        return new(detail is null ? _role : $"{_role}.{detail}", fallback);
    }

    private static T Value<T>(Role role, string property, T fallback)
    {
        if (_skin is null)
            return fallback;
        if (role.Shared is not null)
            fallback = _skin.Find(role.Shared, property, fallback);
        return _skin.Find(role.Primary, property, fallback);
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

    internal static bool Image(SKCanvas canvas, Role role, SKRect bounds)
    {
        var image = _skin?.Image(role.Primary) ?? (role.Shared is not null ? _skin?.Image(role.Shared) : null);
        if (image is null || bounds.IsEmpty)
            return false;

        using var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha((byte)MathF.Round(255 * Number(role, "opacity", 1))),
            FilterQuality = SKFilterQuality.Low,
        };
        canvas.DrawImage(image, bounds, paint);
        return true;
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
        float radius = MathF.Min(Number(role, "radius", 0) * scale, MathF.Min(bounds.Width, bounds.Height) / 2);
        if (radius > 0)
            canvas.DrawRoundRect(bounds, radius, radius, paint);
        else
            canvas.DrawRect(bounds, paint);

        canvas.Save();
        if (radius > 0)
        {
            using var clip = new SKRoundRect(bounds, radius, radius);
            canvas.ClipRoundRect(clip, antialias: true);
        }
        Image(canvas, role, bounds);
        canvas.Restore();

        if (border is null && !Has(role, "stroke"))
            return;
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = Color(role, "stroke", border ?? SKColors.Transparent);
        paint.StrokeWidth = MathF.Min(Number(role, "strokeWidth", 1) * scale, MathF.Min(bounds.Width, bounds.Height) / 2);
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
        var typeface = _skin?.Typeface(role.Primary) ?? (role.Shared is not null ? _skin?.Typeface(role.Shared) : null);
        if (typeface is null || string.IsNullOrEmpty(text))
            return;
        var fallback = paint.Typeface;
        paint.Typeface = typeface;
        if (paint.GetGlyphs(text).Any(glyph => glyph == 0))
            paint.Typeface = fallback;
    }
}
