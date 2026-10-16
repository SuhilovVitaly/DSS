using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal sealed record GameSessionRightColumnLayout(SKRect Bounds, float ObjectInfoMaximumBottom)
{
    internal const float MinimumReportsHeight = 80;
    internal static GameSessionRightColumnLayout Create(float width, float height, float top, float hudTop, float panelWidth)
    {
        float bottom = Math.Max(top, Math.Min(height - 8, hudTop - 8));
        float w = Math.Min(Math.Max(0, panelWidth), Math.Max(0, width - 16));
        var bounds = new SKRect(Math.Max(0, width - 8 - w), top, Math.Max(0, width - 8), bottom);
        return new(bounds, Math.Max(top, bottom - MinimumReportsHeight - 8));
    }

    internal SKRect ReportsAfter(SKRect objectInfo) => new(objectInfo.Left,
        Math.Clamp(objectInfo.Bottom + 8, Bounds.Top, Bounds.Bottom), objectInfo.Right, Bounds.Bottom);
}
