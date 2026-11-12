using DeepSpaceSaga.Contracts;
using SkiaSharp;
using Skin = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class TacticalShipSprites
{
    internal static int Frame(double course)
    {
        double angle = (course % 360 + 360) % 360;
        return (int)Math.Round((angle + 90) / 11.25) % 32;
    }

    internal static int Factor(float scale) => Math.Max(1, (int)MathF.Round(scale * 4 / 3));
    internal static float CanvasScale(SKCanvas canvas) => MathF.Sqrt(
        canvas.TotalMatrix.ScaleX * canvas.TotalMatrix.ScaleX + canvas.TotalMatrix.SkewY * canvas.TotalMatrix.SkewY);

    internal static SKRect Bounds(SKPoint point, int cell, float scale, int factor)
    {
        float size = cell * factor / scale;
        return SKRect.Create(MathF.Round((point.X - size / 2) * scale) / scale,
            MathF.Round((point.Y - size / 2) * scale) / scale, size, size);
    }

    internal static bool Draw(SKCanvas canvas, TacticalUnitSnapshot unit, SKPoint point, bool own,
        double course, byte alpha = 255, int? factor = null, bool shadow = true)
    {
        string model = unit.Kind == TacticalUnitKind.Carrier ? "carrier"
            : unit.FrameId is "light" or "heavy" or "thug" ? unit.FrameId : "heavy";
        string side = unit.Kind == TacticalUnitKind.Drone && unit.State is TacticalUnitState.Disabled or TacticalUnitState.Destroyed
            ? "disabled" : own ? "own" : "enemy";
        var sheet = Skin.ShipSheet(model, side);
        if (sheet is null || !double.IsFinite(course)) return false;
        int index = Frame(course) % sheet.Value.Frames;
        var source = SKRect.Create(index % sheet.Value.Columns * sheet.Value.Cell,
            index / sheet.Value.Columns * sheet.Value.Cell, sheet.Value.Cell, sheet.Value.Cell);
        return DrawCell(canvas, sheet.Value.Image, source, point, alpha, factor, shadow);
    }

    private static bool DrawCell(SKCanvas canvas, SKImage image, SKRect source, SKPoint point,
        byte alpha, int? factor, bool shadow)
    {
        float scale = CanvasScale(canvas);
        if (!(scale > 0) || !float.IsFinite(scale)) return false;
        var destination = Bounds(point, (int)source.Width, scale, factor ?? Factor(scale));
        using var paint = new SKPaint { IsAntialias = false, FilterQuality = SKFilterQuality.None,
            Color = SKColors.White.WithAlpha(alpha) };
        if (shadow)
        {
            using var filter = SKColorFilter.CreateBlendMode(new SKColor(0, 0, 0, 140), SKBlendMode.SrcIn);
            using var blur = SKImageFilter.CreateBlur(2.2f, 2.2f);
            paint.ColorFilter = filter;
            paint.ImageFilter = blur;
            var offset = destination;
            offset.Offset(4, 6);
            canvas.DrawImage(image, source, offset, paint);
            paint.ColorFilter = null;
            paint.ImageFilter = null;
        }
        canvas.DrawImage(image, source, destination, paint);
        return true;
    }

    internal static bool DrawTorpedo(SKCanvas canvas, SKPoint point, float directionDeg, bool own)
    {
        var sheet = Skin.ShipSheet("torpedo", own ? "own" : "enemy");
        if (sheet is null || !float.IsFinite(directionDeg)) return false;
        int frame = Frame(directionDeg) % sheet.Value.Frames;
        var source = SKRect.Create(frame % sheet.Value.Columns * sheet.Value.Cell,
            frame / sheet.Value.Columns * sheet.Value.Cell, sheet.Value.Cell, sheet.Value.Cell);
        return DrawCell(canvas, sheet.Value.Image, source, point, 255, null, false);
    }

    internal static int EffectFrames(string name) => Skin.EffectStrip(name)?.Frames ?? 0;

    internal static bool DrawEffect(SKCanvas canvas, string name, int frame, SKPoint point, byte alpha = 255)
    {
        var strip = Skin.EffectStrip(name);
        if (strip is null || frame < 0 || frame >= strip.Value.Frames) return false;
        var source = SKRect.Create(frame * strip.Value.Cell, 0, strip.Value.Cell, strip.Value.Cell);
        return DrawCell(canvas, strip.Value.Image, source, point, alpha, null, false);
    }
}
