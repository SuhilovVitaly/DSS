using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Pre-computed geometry for a single object label on the tactical map.
/// Produced by <see cref="ObjectLabelLayout.Create"/>.
/// </summary>
internal readonly record struct ObjectLabelGeometry(
    SKRect PlaqueRect,
    SKPoint LeaderEndPoint,
    SKRect StatusRect,
    SKPoint TextOrigin
);
